// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Functions
{
    /// <summary>
    ///  SimpleTransformFunction converts a Func&lt;T, U&gt; into a function in XForm.
    ///  It reads the source column, allocates result and null arrays, and passes non-null
    ///  values to the function.
    ///  
    ///  If your function requires an addition buffer for transformation (like a String8Block
    ///  to hold changed copies of strings), you can declare it in a scope the Func can see
    ///  and clear it in the 'beforexarray' action. See XForm.Functions.String.ToUpper.
    /// </summary>
    /// <typeparam name="T">Type of the source column</typeparam>
    /// <typeparam name="U">Type output by the function</typeparam>
    public class SimpleTransformFunction<T, U> : IXColumn
    {
        private string _name;
        private IXColumn _column;
        private Func<T, U> _function;
        private Action _beforeBatch;
        private U[] _buffer;
        private bool[] _isNull;

        private XArray _convertedValues;

        public ColumnDetails ColumnDetails { get; private set; }

        private SimpleTransformFunction(string name, IXColumn column, Func<T, U> function, Action beforeBatch = null)
        {
            _name = name;
            _column = column;
            _function = function;
            _beforeBatch = beforeBatch;
            this.ColumnDetails = column.ColumnDetails.ChangeType(typeof(U));
        }

        public static IXColumn Build(string name, IXTable source, IXColumn column, Func<T, U> function, Action beforeBatch = null)
        {
            if (column.ColumnDetails.Type != typeof(T)) throw new ArgumentException($"Function required argument of type {typeof(T).Name}, but argument was {column.ColumnDetails.Type.Name} instead.");
            return new SimpleTransformFunction<T, U>(name, column, function, beforeBatch);
        }

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> sourceGetter = _column.CurrentGetter();
            if (ValuesGetter() != null)
            {
                // Get values mapped and replace values array with the converted array 
                return () => sourceGetter().ReplaceValues(_convertedValues);
            }
            else
            {
                // Otherwise, convert from the underlying current getter
                return () =>
                {
                    _beforeBatch?.Invoke();
                    return Convert(sourceGetter());
                };
            }
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            Func<ArraySelector, XArray> sourceGetter = _column.SeekGetter();
            if (ValuesGetter() != null)
            {
                // Get values mapped and replace values array with the converted array 
                return (selector) => sourceGetter(selector).ReplaceValues(_convertedValues);
            }
            else
            {
                // Otherwise, convert from the underlying current getter
                return (selector) =>
                {
                    _beforeBatch?.Invoke();
                    return Convert(sourceGetter(selector));
                };
            }
        }

        public Func<XArray> ValuesGetter()
        {
            if (_convertedValues.Array == null)
            {
                Func<XArray> innerGetter = _column.ValuesGetter();
                if (innerGetter == null) return null;

                _beforeBatch?.Invoke();
                _convertedValues = Convert(innerGetter());
            }

            return () => _convertedValues;
        }

        public Type IndicesType => _column.IndicesType;

        public Func<XArray> IndicesCurrentGetter()
        {
            return _column.IndicesCurrentGetter();
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return _column.IndicesSeekGetter();
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }

        private XArray Convert(XArray xarray)
        {
            // If a single value was returned, only convert it
            if (xarray.Selector.IsSingleValue)
            {
                Allocator.AllocateToSize(ref _buffer, 1);
                _buffer[0] = _function(((T[])xarray.Array)[0]);
                return XArray.Single(_buffer, xarray.Count);
            }

            // Allocate for results
            Allocator.AllocateToSize(ref _buffer, xarray.Count);

            // Convert each non-null value
            T[] array = (T[])xarray.Array;
            for (int i = 0; i < xarray.Count; ++i)
            {
                int index = xarray.Index(i);
                bool rowIsNull = (xarray.HasNulls && xarray.NullRows[index]);
                _buffer[i] = (rowIsNull ? default(U) : _function(array[index]));
            }

            return XArray.All(_buffer, xarray.Count, XArray.RemapNulls(xarray, ref _isNull));
        }

        public override string ToString()
        {
            return $"{_name}({_column})";
        }
    }
}

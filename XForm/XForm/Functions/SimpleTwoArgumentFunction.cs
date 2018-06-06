// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Functions
{
    /// <summary>
    ///  SimpleTwoArgumentFunction converts a Func&lt;T, U, V&gt; into a function in XForm.
    ///  It reads the source columns, allocates result and null arrays, and passes non-null
    ///  values to the function.
    ///  
    ///  If your function requires an addition buffer for transformation (like a String8Block
    ///  to hold changed copies of strings), you can declare it in a scope the Func can see
    ///  and clear it in the 'beforexarray' action. See XForm.Functions.String.ToUpper.
    /// </summary>
    /// <typeparam name="T">Type of the first source column</typeparam>
    /// <typeparam name="U">Type of the second source column</typeparam>
    /// <typeparam name="V">Type output by the function</typeparam>
    public class SimpleTwoArgumentFunction<T, U, V> : IXColumn
    {
        private string _name;
        private IXColumn _column1;
        private IXColumn _column2;
        private Func<T, U, V> _function;
        private Action _beforeBatch;
        private V[] _buffer;
        private bool[] _isNull;

        public ColumnDetails ColumnDetails { get; private set; }

        private SimpleTwoArgumentFunction(string name, IXColumn column1, IXColumn column2, Func<T, U, V> function, Action beforeBatch = null)
        {
            _name = name;
            _column1 = column1;
            _column2 = column2;
            _function = function;
            _beforeBatch = beforeBatch;
            this.ColumnDetails = new ColumnDetails(name, typeof(V));
        }

        public static IXColumn Build(string name, IXTable source, IXColumn column1, IXColumn column2, Func<T, U, V> function, Action beforeBatch = null)
        {
            if (column1.ColumnDetails.Type != typeof(T)) throw new ArgumentException($"Function required first argument of type {typeof(T).Name}, but argument was {column1.ColumnDetails.Type.Name} instead.");
            if (column2.ColumnDetails.Type != typeof(U)) throw new ArgumentException($"Function required second argument of type {typeof(U).Name}, but argument was {column2.ColumnDetails.Type.Name} instead.");
            return new SimpleTwoArgumentFunction<T, U, V>(name, column1, column2, function, beforeBatch);
        }

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> getter1 = _column1.CurrentGetter();
            Func<XArray> getter2 = _column2.CurrentGetter();

            return () =>
            {
                _beforeBatch?.Invoke();
                return Convert(getter1(), getter2());
            };
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            Func<ArraySelector, XArray> getter1 = _column1.SeekGetter();
            Func<ArraySelector, XArray> getter2 = _column2.SeekGetter();

            // If either side doesn't support seek, we can't seek
            if (getter1 == null || getter2 == null) return null;

            // Otherwise, convert from the underlying current getter
            return (selector) =>
            {
                _beforeBatch?.Invoke();
                return Convert(getter1(selector), getter2(selector));
            };
        }

        public Func<XArray> ValuesGetter()
        {
            return null;
        }

        public Type IndicesType => null;

        public Func<XArray> IndicesCurrentGetter()
        {
            return null;
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return null;
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }

        private XArray Convert(XArray xarray1, XArray xarray2)
        {
            int count = xarray1.Count;
            if (count != xarray2.Count) throw new InvalidOperationException("SimpleTwoArgumentFunction must get the same number of rows from each argument.");

            // Allocate for results
            Allocator.AllocateToSize(ref _buffer, count);
            Allocator.AllocateToSize(ref _isNull, count);

            // Convert each non-null value
            bool areAnyNull = false;
            T[] array1 = (T[])xarray1.Array;
            U[] array2 = (U[])xarray2.Array;
            for (int i = 0; i < count; ++i)
            {
                int index1 = xarray1.Index(i);
                int index2 = xarray2.Index(i);

                bool rowIsNull = (xarray1.HasNulls && xarray1.NullRows[index1]) || (xarray2.HasNulls && xarray2.NullRows[index2]);
                areAnyNull |= rowIsNull;

                _isNull[i] = rowIsNull;
                _buffer[i] = (rowIsNull ? default(V) : _function(array1[index1], array2[index2]));
            }

            return XArray.All(_buffer, count, (areAnyNull ? _isNull : null));
        }

        public override string ToString()
        {
            return $"{_name}({_column1}, {_column2})";
        }
    }
}

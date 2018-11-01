// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Functions
{
    /// <summary>
    ///  SimpleThreeArgumentFunction converts a Func&lt;T, U, V, W&gt; into a function in XForm.
    ///  It reads the source columns, allocates result and null arrays, and passes non-null
    ///  values to the function.
    ///  
    ///  If your function requires an addition buffer for transformation (like a String8Block
    ///  to hold changed copies of strings), you can declare it in a scope the Func can see
    ///  and clear it in the 'beforexarray' action. See XForm.Functions.String.ToUpper.
    /// </summary>
    /// <typeparam name="T">Type of the first source column</typeparam>
    /// <typeparam name="U">Type of the second source column</typeparam>
    /// <typeparam name="U">Type of the third source column</typeparam>
    /// <typeparam name="W">Type output by the function</typeparam>
    public class SimpleThreeArgumentFunction<T, U, V, W> : IXColumn
    {
        private string _name;
        private IXColumn _column1;
        private IXColumn _column2;
        private IXColumn _column3;
        private Func<T, U, V, W> _function;
        private Action _beforeBatch;
        private W[] _buffer;
        private bool[] _isNull;

        public ColumnDetails ColumnDetails { get; private set; }

        private SimpleThreeArgumentFunction(string name, IXColumn column1, IXColumn column2, IXColumn column3, Func<T, U, V, W> function, Action beforeBatch = null)
        {
            _name = name;
            _column1 = column1;
            _column2 = column2;
            _column3 = column3;
            _function = function;
            _beforeBatch = beforeBatch;
            this.ColumnDetails = new ColumnDetails(name, typeof(V));
        }

        public static IXColumn Build(string name, IXTable source, IXColumn column1, IXColumn column2, IXColumn column3, Func<T, U, V, W> function, Action beforeBatch = null)
        {
            if (column1.ColumnDetails.Type != typeof(T)) throw new ArgumentException($"Function required first argument of type {typeof(T).Name}, but argument was {column1.ColumnDetails.Type.Name} instead.");
            if (column2.ColumnDetails.Type != typeof(U)) throw new ArgumentException($"Function required second argument of type {typeof(U).Name}, but argument was {column2.ColumnDetails.Type.Name} instead.");
            if (column3.ColumnDetails.Type != typeof(V)) throw new ArgumentException($"Function required second argument of type {typeof(V).Name}, but argument was {column3.ColumnDetails.Type.Name} instead.");
            return new SimpleThreeArgumentFunction<T, U, V, W>(name, column1, column2, column3, function, beforeBatch);
        }

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> getter1 = _column1.CurrentGetter();
            Func<XArray> getter2 = _column2.CurrentGetter();
            Func<XArray> getter3 = _column3.CurrentGetter();

            return () =>
            {
                _beforeBatch?.Invoke();
                return Convert(getter1(), getter2(), getter3());
            };
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            Func<ArraySelector, XArray> getter1 = _column1.SeekGetter();
            Func<ArraySelector, XArray> getter2 = _column2.SeekGetter();
            Func<ArraySelector, XArray> getter3 = _column3.SeekGetter();

            // If either side doesn't support seek, we can't seek
            if (getter1 == null || getter2 == null || getter3 == null) return null;

            // Otherwise, convert from the underlying current getter
            return (selector) =>
            {
                _beforeBatch?.Invoke();
                return Convert(getter1(selector), getter2(selector), getter3(selector));
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

        private XArray Convert(XArray xarray1, XArray xarray2, XArray xarray3)
        {
            int count = xarray1.Count;
            if (count != xarray2.Count) throw new InvalidOperationException("SimpleTwoArgumentFunction must get the same number of rows from each argument.");
            if (count != xarray3.Count) throw new InvalidOperationException("SimpleTwoArgumentFunction must get the same number of rows from each argument.");

            // Allocate for results
            Allocator.AllocateToSize(ref _buffer, count);
            Allocator.AllocateToSize(ref _isNull, count);

            // Convert each non-null value
            bool areAnyNull = false;
            T[] array1 = (T[])xarray1.Array;
            U[] array2 = (U[])xarray2.Array;
            V[] array3 = (V[])xarray3.Array;

            for (int i = 0; i < count; ++i)
            {
                int index1 = xarray1.Index(i);
                int index2 = xarray2.Index(i);
                int index3 = xarray3.Index(i);

                bool rowIsNull = (xarray1.HasNulls && xarray1.NullRows[index1]) || (xarray2.HasNulls && xarray2.NullRows[index2]) || (xarray3.HasNulls && xarray3.NullRows[index3]);
                areAnyNull |= rowIsNull;

                _isNull[i] = rowIsNull;
                _buffer[i] = (rowIsNull ? default(W) : _function(array1[index1], array2[index2], array3[index3]));
            }

            return XArray.All(_buffer, count, (areAnyNull ? _isNull : null));
        }

        public override string ToString()
        {
            return $"{_name}({_column1}, {_column2}, {_column3})";
        }
    }
}

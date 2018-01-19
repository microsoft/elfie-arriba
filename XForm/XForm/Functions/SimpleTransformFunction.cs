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
    ///  and clear it in the 'beforeBatch' action. See XForm.Functions.String.ToUpper.
    /// </summary>
    /// <typeparam name="T">Type of the source column</typeparam>
    /// <typeparam name="U">Type output by the function</typeparam>
    public class SimpleTransformFunction<T, U> : IDataBatchColumn
    {
        private string _name;
        private IDataBatchColumn _column;
        private Func<T, U> _function;
        private Action _beforeBatch;
        private U[] _buffer;
        private bool[] _isNull;

        public ColumnDetails ColumnDetails { get; private set; }

        private SimpleTransformFunction(string name, IDataBatchColumn column, Func<T, U> function, Action beforeBatch = null)
        {
            this._name = name;
            this._column = column;
            this._function = function;
            this._beforeBatch = beforeBatch;
            this.ColumnDetails = column.ColumnDetails.ChangeType(typeof(U));
        }

        public static IDataBatchColumn Build(string name, IDataBatchEnumerator source, IDataBatchColumn column, Func<T, U> function, Action beforeBatch = null)
        {
            if (column.ColumnDetails.Type != typeof(T)) throw new ArgumentException($"Function required argument of type {typeof(T).Name}, but argument was {column.ColumnDetails.Type.Name} instead.");

            if (column is Constant)
            {
                // If the input is a constant, only convert once
                if (beforeBatch != null) beforeBatch();
                return new Constant(source, function((T)((Constant)column).Value), typeof(U));
            }
            else if(column is EnumColumn)
            {
                // If the input is an enum, convert each value once
                EnumColumn enumColumn = (EnumColumn)column;
                if (beforeBatch != null) beforeBatch();
                return new EnumColumn(enumColumn, new SimpleTransformFunction<T, U>(name, column, function, beforeBatch).Convert(enumColumn.Values()), typeof(U));
            }
            else
            {
                return new SimpleTransformFunction<T, U>(name, column, function, beforeBatch);
            }
        }

        public Func<DataBatch> Getter()
        {
            Func<DataBatch> sourceGetter = _column.Getter();

            return () =>
            {
                if (_beforeBatch != null) _beforeBatch();
                DataBatch batch = sourceGetter();
                return Convert(batch);
            };
        }

        private DataBatch Convert(DataBatch batch)
        {
            // If a single value was returned, only convert it
            if (batch.Selector.IsSingleValue)
            {
                Allocator.AllocateToSize(ref _buffer, 1);
                _buffer[0] = _function(((T[])batch.Array)[0]);
                return DataBatch.Single(_buffer, batch.Count);
            }

            // Allocate for results
            Allocator.AllocateToSize(ref _buffer, batch.Count);

            // Convert each non-null value
            T[] array = (T[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int index = batch.Index(i);
                bool rowIsNull = (batch.IsNull != null && batch.IsNull[index]);
                _buffer[i] = (rowIsNull ? default(U) : _function(array[index]));
            }

            return DataBatch.All(_buffer, batch.Count, DataBatch.RemapNulls(batch, ref _isNull));
        }

        public override string ToString()
        {
            return $"{_name}({_column})";
        }
    }
}

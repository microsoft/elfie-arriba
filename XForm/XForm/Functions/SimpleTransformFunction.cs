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
        private string Name { get; set; }
        private IDataBatchColumn Column { get; set; }
        private Func<T, U> Function { get; set; }
        private Action BeforeBatch { get; set; }
        public ColumnDetails ColumnDetails { get; private set; }

        private SimpleTransformFunction(string name, IDataBatchColumn column, Func<T, U> function, Action beforeBatch = null)
        {
            this.Name = name;
            this.Column = column;
            this.Function = function;
            this.BeforeBatch = beforeBatch;
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
            else
            {
                return new SimpleTransformFunction<T, U>(name, column, function, beforeBatch);
            }
        }

        public Func<DataBatch> Getter()
        {
            Func<DataBatch> sourceGetter = Column.Getter();
            U[] buffer = null;
            bool[] isNull = null;

            return () =>
            {
                if (BeforeBatch != null) BeforeBatch();
                DataBatch batch = sourceGetter();

                // If a single value was returned, only convert it
                if (batch.Selector.IsSingleValue)
                {
                    Allocator.AllocateToSize(ref buffer, 1);
                    buffer[0] = Function(((T[])batch.Array)[0]);
                    return DataBatch.Single(buffer, batch.Count);
                }

                // Allocate for results
                Allocator.AllocateToSize(ref buffer, batch.Count);

                // Allocate null array if needed
                if (batch.IsNull != null)
                {
                    Allocator.AllocateToSize(ref isNull, batch.Count);
                    Array.Clear(isNull, 0, batch.Count);
                }

                // Convert each non-null value
                T[] array = (T[])batch.Array;
                for (int i = 0; i < batch.Count; ++i)
                {
                    int index = batch.Index(i);
                    bool rowIsNull = batch.IsNull != null && batch.IsNull[index];

                    if (rowIsNull)
                    {
                        buffer[i] = default(U);
                        isNull[i] = true;
                    }
                    else
                    {
                        buffer[i] = Function(array[index]);
                    }
                }

                return DataBatch.All(buffer, batch.Count, isNull);
            };
        }

        public override string ToString()
        {
            return $"{Name}({Column})";
        }
    }
}

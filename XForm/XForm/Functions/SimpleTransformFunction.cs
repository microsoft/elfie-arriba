using System;
using XForm.Data;

namespace XForm.Functions
{
    /// <summary>
    ///  SimpleTransformFunction converts a Func&lt;T, U&gt; into a function in XForm.
    ///  It reads the source column, allocates result and null arrays, and passes non-null
    ///  values to the function.
    /// </summary>
    /// <typeparam name="T">Type of the source column</typeparam>
    /// <typeparam name="U">Type output by the function</typeparam>
    public class SimpleTransformFunction<T, U> : IDataBatchColumn
    {
        private IDataBatchColumn Column { get; set; }
        private Func<T, U> Function { get; set; }
        public ColumnDetails ColumnDetails { get; private set; }

        public SimpleTransformFunction(IDataBatchColumn column, Func<T, U> function)
        {
            this.Column = column;
            this.Function = function;
            this.ColumnDetails = column.ColumnDetails.ChangeType(typeof(U));
        }

        public Func<DataBatch> Getter()
        {
            Func<DataBatch> sourceGetter = Column.Getter();
            U[] buffer = null;
            bool[] isNull = null;

            return () =>
            {
                DataBatch batch = sourceGetter();

                Allocator.AllocateToSize(ref buffer, batch.Count);

                if (batch.IsNull != null)
                {
                    Allocator.AllocateToSize(ref isNull, batch.Count);
                    Array.Clear(isNull, 0, batch.Count);
                }

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
    }
}

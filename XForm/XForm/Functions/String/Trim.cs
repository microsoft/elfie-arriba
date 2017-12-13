using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using XForm.Data;

namespace XForm.Functions.String
{
    internal class TrimBuilder : IFunctionBuilder
    {
        public string Name => "Trim";

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new Trim(context.Parser.NextColumn(source, context));
        }
    }

    public class Trim : IDataBatchColumn
    {
        private IDataBatchColumn _column;

        public Trim(IDataBatchColumn column)
        {
            _column = column;
            if (_column.ColumnDetails.Type != typeof(String8)) throw new ArgumentException($"Trim() requires a String8 argument.");
        }

        public ColumnDetails ColumnDetails => _column.ColumnDetails;

        public Func<DataBatch> Getter()
        {
            Func<DataBatch> sourceGetter = _column.Getter();

            String8[] buffer = null;
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

                String8[] array = (String8[])batch.Array;
                for (int i = 0; i < batch.Count; ++i)
                {
                    int index = batch.Index(i);
                    bool rowIsNull = batch.IsNull != null && batch.IsNull[index];

                    if (rowIsNull)
                    {
                        buffer[i] = String8.Empty;
                        isNull[i] = true;
                    }
                    else
                    {
                        buffer[i] = array[index].Trim();
                    }
                }

                return DataBatch.All(buffer, batch.Count, isNull);
            };
        }
    }
}

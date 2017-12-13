using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using XForm.Data;
using XForm.Extensions;

namespace XForm.Functions
{
    internal class ToUpperBuilder : IFunctionBuilder
    {
        public string Name => "ToUpper";

        public IDataBatchFunction Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new ToUpper(source, context.Parser.NextColumnName(source));
        }
    }

    public class ToUpper : IDataBatchFunction
    {
        private Func<DataBatch> _source;
        public ColumnDetails ReturnType { get; private set; }

        public ToUpper(IDataBatchEnumerator source, string sourceColumnName)
        {
            int sourceColumnIndex = source.Columns.IndexOfColumn(sourceColumnName);
            ReturnType = source.Columns[sourceColumnIndex];

            if (ReturnType.Type != typeof(String8)) throw new ArgumentException($"ToUpper() requires a String8 argument.");
            _source = source.ColumnGetter(source.Columns.IndexOfColumn(sourceColumnName));
        }

        public Func<int, DataBatch> Getter()
        {
            String8Block block = new String8Block();
            String8[] buffer = null;
            bool[] isNull = null;

            return (rowCount) =>
            {
                block.Clear();

                DataBatch batch = _source();

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
                        buffer[i] = block.GetCopy(array[index]);
                        buffer[i].ToUpperInvariant();
                    }
                }

                return DataBatch.All(buffer, batch.Count, isNull);
            };
        }
    }
}

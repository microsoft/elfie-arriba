using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using XForm.Data;

namespace XForm.Functions.String
{
    internal class ToUpperBuilder : IFunctionBuilder
    {
        public string Name => "ToUpper";

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new ToUpper(context.Parser.NextColumn(source, context));
        }
    }

    public class ToUpper : IDataBatchColumn
    {
        private IDataBatchColumn _column;

        public ToUpper(IDataBatchColumn column)
        {
            _column = column;
            if (_column.ColumnDetails.Type != typeof(String8)) throw new ArgumentException($"ToUpper() requires a String8 argument.");
        }

        public ColumnDetails ColumnDetails => _column.ColumnDetails;

        public Func<DataBatch> Getter()
        {
            Func<DataBatch> sourceGetter = _column.Getter();

            String8Block block = new String8Block();
            String8[] buffer = null;
            bool[] isNull = null;

            return () =>
            {
                block.Clear();

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
                        buffer[i] = block.GetCopy(array[index]);
                        buffer[i].ToUpperInvariant();
                    }
                }

                return DataBatch.All(buffer, batch.Count, isNull);
            };
        }
    }
}

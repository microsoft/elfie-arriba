using System;
using XForm.Data;

namespace XForm.Functions
{
    internal class AsOfDateBuilder : IFunctionBuilder
    {
        public string Name => "AsOfDate";

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new AsOfDate(source, context);
        }
    }

    public class AsOfDate : IDataBatchColumn
    {
        private IDataBatchEnumerator Source { get; set; }
        private DateTime[] Value { get; set; }
        public ColumnDetails ColumnDetails { get; private set; }

        public AsOfDate(IDataBatchEnumerator source, WorkflowContext context)
        {
            Source = source;

            Value = new DateTime[1];
            Value[0] = context.RequestedAsOfDateTime;

            ColumnDetails = new ColumnDetails("AsOfDate", typeof(DateTime), false);
        }

        public Func<DataBatch> Getter()
        {
            return () => DataBatch.Single(Value, Source.CurrentBatchRowCount);
        }
    }
}

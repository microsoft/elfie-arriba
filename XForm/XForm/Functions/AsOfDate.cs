using System;
using XForm.Data;

namespace XForm.Functions
{
    internal class AsOfDateBuilder : IFunctionBuilder
    {
        public string Name => "AsOfDate";

        public IDataBatchFunction Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new AsOfDate(context);
        }
    }

    public class AsOfDate : IDataBatchFunction
    {
        private DateTime[] _value;

        public ColumnDetails ReturnType { get; private set; }

        public AsOfDate(WorkflowContext context)
        {
            _value = new DateTime[1];
            _value[0] = context.RequestedAsOfDateTime;

            ReturnType = new ColumnDetails("AsOfDate", typeof(DateTime), false);
        }

        public Func<int, DataBatch> Getter()
        {
            return (rowCount) => DataBatch.Single(_value, rowCount);
        }
    }
}

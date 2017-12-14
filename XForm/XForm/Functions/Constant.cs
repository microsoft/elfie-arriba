using System;
using XForm.Data;

namespace XForm.Functions
{
    public class Constant : IDataBatchColumn
    {
        private IDataBatchEnumerator Source { get; set; }
        private Array ValueArray { get; set; }
        public ColumnDetails ColumnDetails { get; private set; }

        public Constant(IDataBatchEnumerator source, object value, Type type)
        {
            Source = source;
            ValueArray = Allocator.AllocateArray(type, 1);
            ValueArray.SetValue(value, 0);
            ColumnDetails = new ColumnDetails(string.Empty, type, false);
        }

        public object Value => ValueArray.GetValue(0);

        public Func<DataBatch> Getter()
        {
            return () => DataBatch.Single(ValueArray, Source.CurrentBatchRowCount);
        }
    }
}

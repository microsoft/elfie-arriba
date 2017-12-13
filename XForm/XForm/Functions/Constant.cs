using System;
using XForm.Data;

namespace XForm.Functions
{
    public class Constant : IDataBatchColumn
    {
        private IDataBatchEnumerator Source { get; set; }
        private Array Value { get; set; }
        public ColumnDetails ColumnDetails { get; private set; }

        public Constant(IDataBatchEnumerator source, object value, Type type)
        {
            Source = source;
            Allocator.AllocateArray(type, 1);
            Value.SetValue(value, 0);
        }

        public Func<DataBatch> Getter()
        {
            return () => DataBatch.Single(Value, Source.CurrentBatchRowCount);
        }
    }
}

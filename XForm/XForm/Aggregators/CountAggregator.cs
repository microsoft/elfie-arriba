using System;
using System.Collections.Generic;
using XForm.Data;

namespace XForm.Aggregators
{
    public class CountAggregator : IDataBatchSource
    {
        private List<ColumnDetails> _column;
        private IDataBatchSource _source;
        private int _count;

        public CountAggregator(IDataBatchSource source)
        {
            _source = source;

            _count = -1;

            _column = new List<ColumnDetails>();
            _column.Add(new ColumnDetails("Count", typeof(int), false));
        }

        public IReadOnlyList<ColumnDetails> Columns => _column;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            if (columnIndex != 0) throw new ArgumentOutOfRangeException("columnIndex");

            int[] result = new int[1];

            return () =>
            {
                result[0] = _count;
                return DataBatch.All(result, 1);
            };
        }

        public bool Next(int desiredCount)
        {
            if (_count != -1) return false;

            _count = 0;

            Func<DataBatch> getter = _source.ColumnGetter(0);
            while(_source.Next(desiredCount))
            {
                DataBatch batch = getter();
                _count += batch.Count;
            }

            return true;
        }

        public void Dispose()
        {
            if(_source != null)
            {
                _source.Dispose();
                _source = null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using XForm.Data;

namespace XForm.Transforms
{
    public class RowLimiter : IDataBatchSource
    {
        private IDataBatchSource _source;
        private int _countLimit;
        private int _countSoFar;

        public RowLimiter(IDataBatchSource source, int countLimit)
        {
            _source = source;
            _countLimit = countLimit;
        }

        public IReadOnlyList<ColumnDetails> Columns => _source.Columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return _source.ColumnGetter(columnIndex);
        }

        public int Next(int desiredCount)
        {
            if (_countSoFar + desiredCount > _countLimit) desiredCount = _countLimit - _countSoFar;

            int sourceCount = _source.Next(desiredCount);
            _countSoFar += sourceCount;

            return sourceCount;
        }

        public void Dispose()
        {
            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }
        }
    }
}

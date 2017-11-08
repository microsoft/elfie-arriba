using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XForm.Data
{
    public class DataBatchEnumeratorWrapper : IDataBatchEnumerator
    {
        protected IDataBatchEnumerator _source;

        public DataBatchEnumeratorWrapper(IDataBatchEnumerator source)
        {
            _source = source;
        }

        public virtual IReadOnlyList<ColumnDetails> Columns => _source.Columns;

        public virtual Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return _source.ColumnGetter(columnIndex);
        }

        public virtual int Next(int desiredCount)
        {
            return _source.Next(desiredCount);
        }

        public virtual void Reset()
        {
            _source.Reset();
        }

        public virtual void Dispose()
        {
            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }
        }
    }
}

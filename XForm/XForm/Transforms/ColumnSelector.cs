using System;
using System.Collections.Generic;
using XForm.Data;
using XForm.Extensions;

namespace XForm.Transforms
{
    public class ColumnSelector : IDataBatchSource
    {
        private IDataBatchSource _source;
        private List<ColumnDetails> _mappedColumns;
        private List<int> _columnInnerIndices;

        public ColumnSelector(IDataBatchSource source, IEnumerable<string> columnNames)
        {
            _source = source;

            _mappedColumns = new List<ColumnDetails>();
            _columnInnerIndices = new List<int>();

            var sourceColumns = _source.Columns;
            foreach(string columnName in columnNames)
            {
                int index = sourceColumns.IndexOfColumn(columnName);
                _columnInnerIndices.Add(index);
                _mappedColumns.Add(sourceColumns[index]);
            }
        }

        public IReadOnlyList<ColumnDetails> Columns => _mappedColumns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return _source.ColumnGetter(_columnInnerIndices[columnIndex]);
        }

        public void Reset()
        {
            _source.Reset();
        }

        public int Next(int desiredCount)
        {
            return _source.Next(desiredCount);
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

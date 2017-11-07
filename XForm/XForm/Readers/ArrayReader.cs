using System;
using System.Collections.Generic;
using XForm.Data;

namespace XForm.Readers
{
    public class ArrayReader : IDataBatchSource
    {
        private List<ColumnDetails> _columns;
        private List<DataBatch> _columnArrays;
        private int _rowCount;

        private int _nextIndex;
        private int _nextLength;
    

        public ArrayReader()
        {
            _columns = new List<ColumnDetails>();
            _columnArrays = new List<DataBatch>();
        }

        public void AddColumn(ColumnDetails details, DataBatch fullColumn)
        {
            if (_columns.Count == 0) _rowCount = fullColumn.Count;
            if (fullColumn.Count != _rowCount) throw new ArgumentException($"All columns passed to ArrayReader must have the same row count. The Reader row count is {_rowCount:n0}; this column has {fullColumn.Count:n0} rows.");

            for(int i = 0; i < _columns.Count; ++i)
            {
                if (_columns[i].Name.Equals(details.Name, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"Can't add duplicate column. ArrayReader already has a column {details.Name}.");
            }

            _columns.Add(details);
            _columnArrays.Add(fullColumn);
        }

        public IReadOnlyList<ColumnDetails> Columns => this._columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return () => _columnArrays[columnIndex].Slice(_nextIndex, _nextIndex + _nextLength);
        }

        public int Next(int desiredCount)
        {
            // Move to after the previous page returned
            _nextIndex += _nextLength;
            _nextLength = desiredCount;

            // If there aren't enough items left, return only those left
            if (_nextIndex + _nextLength > _rowCount) _nextLength = _rowCount - _nextIndex;

            // Return true if there are any more rows
            return _nextLength;
        }

        public void Dispose()
        { }
    }
}

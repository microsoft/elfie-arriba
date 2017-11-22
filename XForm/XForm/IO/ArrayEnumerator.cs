// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;

namespace XForm.IO
{
    public class ArrayEnumerator : IDataBatchList
    {
        private List<ColumnDetails> _columns;
        private List<DataBatch> _columnArrays;
        private int _rowCount;

        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public ArrayEnumerator(int rowCount)
        {
            _columns = new List<ColumnDetails>();
            _columnArrays = new List<DataBatch>();
            _rowCount = rowCount;
            Reset();
        }

        public void AddColumn(ColumnDetails details, DataBatch fullColumn)
        {
            if (fullColumn.Count != _rowCount) throw new ArgumentException($"All columns passed to ArrayReader must have the configured row count. The configured row count is {_rowCount:n0}; this column has {fullColumn.Count:n0} rows.");

            for (int i = 0; i < _columns.Count; ++i)
            {
                if (_columns[i].Name.Equals(details.Name, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"Can't add duplicate column. ArrayReader already has a column {details.Name}.");
            }

            _columns.Add(details);
            _columnArrays.Add(fullColumn);
        }

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public int Count => _rowCount;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return () =>
            {
                if (columnIndex < 0 || columnIndex >= _columnArrays.Count) throw new IndexOutOfRangeException("columnIndex");
                DataBatch raw = _columnArrays[columnIndex];
                if (raw.Selector.Indices != null) throw new NotImplementedException();
                return DataBatch.Select(raw.Array, raw.Count, _currentSelector);
            };
        }

        public void Reset()
        {
            _currentEnumerateSelector = ArraySelector.All(_rowCount).Slice(0, 0);
        }

        public int Next(int desiredCount)
        {
            _currentEnumerateSelector = _currentEnumerateSelector.NextPage(_rowCount, desiredCount);
            _currentSelector = _currentEnumerateSelector;
            return _currentEnumerateSelector.Count;
        }

        public void Get(ArraySelector selector)
        {
            _currentSelector = selector;
        }

        public void Dispose()
        { }
    }
}

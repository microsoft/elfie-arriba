// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Types;

namespace XForm.IO
{
    public class ArrayTable : ISeekableXTable
    {
        private List<ColumnDetails> _columns;
        private List<XArray> _columnArrays;
        private int _rowCount;

        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public ArrayTable(int rowCount)
        {
            _columns = new List<ColumnDetails>();
            _columnArrays = new List<XArray>();
            _rowCount = rowCount;
            Reset();
        }

        public ArrayTable WithColumn(ColumnDetails details, XArray fullColumn)
        {
            if (fullColumn.Count != _rowCount) throw new ArgumentException($"All columns passed to ArrayReader must have the configured row count. The configured row count is {_rowCount:n0}; this column has {fullColumn.Count:n0} rows.");

            for (int i = 0; i < _columns.Count; ++i)
            {
                if (_columns[i].Name.Equals(details.Name, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"Can't add duplicate column. ArrayReader already has a column {details.Name}.");
            }

            _columns.Add(details);
            _columnArrays.Add(fullColumn);
            return this;
        }

        public ArrayTable WithColumn(string columnName, Array array)
        {
            return WithColumn(new ColumnDetails(columnName, array.GetType().GetElementType()), XArray.All(array, _rowCount));
        }

        public IReadOnlyList<ColumnDetails> Columns => _columns;
        public int CurrentRowCount { get; private set; }
        public int Count => _rowCount;
        public ArraySelector EnumerateSelector => _currentEnumerateSelector;

        public Func<XArray> ColumnGetter(int columnIndex)
        {
            // Declare a remap array in case indices must be remapped
            int[] remapArray = null;

            return () =>
            {
                if (columnIndex < 0 || columnIndex >= _columnArrays.Count) throw new IndexOutOfRangeException("columnIndex");
                XArray raw = _columnArrays[columnIndex];
                return raw.Select(_currentSelector, ref remapArray);
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
            CurrentRowCount = _currentEnumerateSelector.Count;
            return CurrentRowCount;
        }

        public IColumnReader ColumnReader(int columnIndex)
        {
            return CachedColumnReader(columnIndex);
        }

        public IColumnReader CachedColumnReader(int columnIndex)
        {
            return new CachedColumnReader(_columnArrays[columnIndex]);
        }

        public void Dispose()
        { }
    }
}

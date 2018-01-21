// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;

namespace XForm.IO
{
    public class ArrayColumn : IXColumn
    {
        private IXTable _table { get; set; }
        private XArray _allValues { get; set; }
        public ColumnDetails ColumnDetails { get; private set; }

        private int[] _remapArray;

        public ArrayColumn(IXTable table, XArray allValues, ColumnDetails columnDetails)
        {
            _table = table;
            _allValues = allValues;
            ColumnDetails = columnDetails;
        }

        public Func<XArray> CurrentGetter()
        {
            return () => _allValues.Select(_table.CurrentSelector, ref _remapArray);
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            return (selector) => _allValues.Reselect(selector);
        }

        public Func<XArray> ValuesGetter()
        {
            return null;
        }

        public Type IndicesType => null;

        public Func<XArray> IndicesCurrentGetter()
        {
            return null;
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return null;
        }
    }

    public class ArrayTable : ISeekableXTable
    {
        private List<ArrayColumn> _columns;
        private int _rowCount;

        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public ArrayTable(int rowCount)
        {
            _columns = new List<ArrayColumn>();
            _rowCount = rowCount;
            Reset();
        }

        public ArrayTable WithColumn(ColumnDetails details, XArray fullColumn)
        {
            if (fullColumn.Count != _rowCount) throw new ArgumentException($"All columns passed to ArrayReader must have the configured row count. The configured row count is {_rowCount:n0}; this column has {fullColumn.Count:n0} rows.");

            for (int i = 0; i < _columns.Count; ++i)
            {
                if (_columns[i].ColumnDetails.Name.Equals(details.Name, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"Can't add duplicate column. ArrayReader already has a column {details.Name}.");
            }

            _columns.Add(new ArrayColumn(this, fullColumn, details));
            return this;
        }

        public ArrayTable WithColumn(string columnName, Array array)
        {
            return WithColumn(new ColumnDetails(columnName, array.GetType().GetElementType()), XArray.All(array, _rowCount));
        }

        public int CurrentRowCount { get; private set; }
        public int Count => _rowCount;
        public ArraySelector CurrentSelector => _currentEnumerateSelector;
        public IReadOnlyList<IXColumn> Columns => _columns;

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

        public void Dispose()
        { }
    }
}

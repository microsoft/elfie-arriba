// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Types;

namespace XForm.IO
{
    public class ArrayColumn : IXColumn
    {
        private IXTable Table { get; set; }
        private XArray AllValues { get; set; }
        public ColumnDetails ColumnDetails { get; private set; }
        private int[] _remapArray;

        public ArrayColumn(IXTable table, XArray allValues, ColumnDetails columnDetails)
        {
            Table = table;
            AllValues = allValues;
            ColumnDetails = columnDetails;
        }

        public Func<XArray> Getter()
        {
            return () => AllValues.Select(Table.CurrentSelector, ref _remapArray);
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

        public IXColumn ColumnGetter(int columnIndex)
        {
            return _columns[columnIndex];
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

        public void Dispose()
        { }
    }
}

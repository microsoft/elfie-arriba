// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using XForm.Data;
using XForm.Types;

namespace XForm.IO
{
    public class BinaryTableReader : IDataBatchEnumerator
    {
        private string _tableRootPath;
        private List<ColumnDetails> _columns;
        private IColumnReader[] _readers;

        private int _totalCount;
        private int _currentRowIndex;
        private int _currentBatchCount;

        public BinaryTableReader(string tableRootPath)
        {
            _tableRootPath = tableRootPath;
            _columns = SchemaSerializer.Read(_tableRootPath);
            _readers = new IColumnReader[_columns.Count];
            Reset();
        }

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            if (_readers[columnIndex] == null)
            {
                ColumnDetails column = Columns[columnIndex];
                _readers[columnIndex] = TypeProviderFactory.Get(column.Type).BinaryReader(Path.Combine(_tableRootPath, column.Name));
            }

            return () => _readers[columnIndex].Read(ArraySelector.All(_totalCount).Slice(_currentRowIndex, _currentRowIndex + _currentBatchCount));
        }

        public int Next(int desiredCount)
        {
            _currentRowIndex += _currentBatchCount;
            _currentBatchCount = desiredCount;
            if (_currentRowIndex + _currentBatchCount > _totalCount) _currentBatchCount = _totalCount - _currentRowIndex;

            return _currentBatchCount;
        }

        public void Reset()
        {
            _currentRowIndex = 0;

            // Get the first reader in order to get the row count
            Func<DataBatch> unused = ColumnGetter(0);
            _totalCount = _readers[0].Count;
        }

        public void Dispose()
        {
            if (_readers != null)
            {
                foreach (IColumnReader reader in _readers)
                {
                    if (reader != null) reader.Dispose();
                }

                _readers = null;
            }
        }
    }
}

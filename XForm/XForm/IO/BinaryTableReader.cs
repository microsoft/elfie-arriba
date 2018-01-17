// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Types;

namespace XForm.IO
{
    public class BinaryTableReader : IDataBatchList
    {
        private IStreamProvider _streamProvider;
        private TableMetadata _metadata;

        private IColumnReader[] _readers;

        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public BinaryTableReader(IStreamProvider streamProvider, string tableRootPath)
        {
            _streamProvider = streamProvider;
            TablePath = tableRootPath;

            _metadata = TableMetadataSerializer.Read(streamProvider, TablePath);
            _readers = new IColumnReader[_metadata.Schema.Count];
            Reset();
        }
        
        public string TablePath { get; private set; }
        public string Query => _metadata.Query;
        public int Count => _metadata.RowCount;
        public IReadOnlyList<ColumnDetails> Columns => _metadata.Schema;

        public int CurrentBatchRowCount { get; private set; }

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Get and cache the reader
            ColumnReader(columnIndex);

            return () => _readers[columnIndex].Read(_currentSelector);
        }

        public IColumnReader ColumnReader(int columnIndex)
        {
            if (_readers[columnIndex] == null)
            {
                ColumnDetails column = Columns[columnIndex];
                string columnPath = Path.Combine(TablePath, column.Name);

                // Read the column using the correct typed reader
                _readers[columnIndex] = TypeProviderFactory.TryGetColumn(column.Type, _streamProvider, columnPath);
            }

            return _readers[columnIndex];
        }

        public IColumnReader CachedColumnReader(int columnIndex)
        {
            if (_readers[columnIndex] == null || !(_readers[columnIndex] is CachedColumnReader))
            {
                _readers[columnIndex] = TypeProviderFactory.TryGetColumn(Columns[columnIndex].Type, _streamProvider, Path.Combine(TablePath, Columns[columnIndex].Name), true);
            }

            return _readers[columnIndex];
        }

        public int Next(int desiredCount)
        {
            _currentEnumerateSelector = _currentEnumerateSelector.NextPage(Count, desiredCount);
            _currentSelector = _currentEnumerateSelector;
            CurrentBatchRowCount = _currentEnumerateSelector.Count;
            return CurrentBatchRowCount;
        }

        public void Get(ArraySelector selector)
        {
            _currentSelector = selector;
        }

        public void Reset()
        {
            // Mark our current position (nothing read yet)
            _currentEnumerateSelector = ArraySelector.All(Count).Slice(0, 0);
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

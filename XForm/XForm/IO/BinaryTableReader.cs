// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using XForm.Data;
using XForm.Extensions;
using XForm.IO.StreamProvider;
using XForm.Query;
using XForm.Types;

namespace XForm.IO
{
    public class BinaryTableReader : IDataBatchList
    {
        internal const string ConfigQueryPath = "Config.xql";

        private IStreamProvider _streamProvider;

        private List<ColumnDetails> _columns;
        private IColumnReader[] _readers;

        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public BinaryTableReader(IStreamProvider streamProvider, string tableRootPath)
        {
            _streamProvider = streamProvider;

            TablePath = tableRootPath;
            Query = streamProvider.ReadAllText(Path.Combine(tableRootPath, ConfigQueryPath));

            _columns = SchemaSerializer.Read(streamProvider, TablePath);
            _readers = new IColumnReader[_columns.Count];
            Reset();
        }

        public string Query { get; private set; }
        public string TablePath { get; private set; }
        public int Count { get; private set; }

        public IReadOnlyList<ColumnDetails> Columns => _columns;
        public int CurrentBatchRowCount { get; private set; }

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            if (_readers[columnIndex] == null)
            {
                ColumnDetails column = Columns[columnIndex];
                string columnPath = Path.Combine(TablePath, column.Name);

                // Build the reader for the column type
                IColumnReader reader = TypeProviderFactory.Get(column.Type).BinaryReader(_streamProvider, columnPath);

                // Wrap in a NullableReader to handle null recognition
                _readers[columnIndex] = new NullableReader(_streamProvider, columnPath, reader);
            }

            return () => _readers[columnIndex].Read(_currentSelector);
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
            // Get the first reader in order to get the row count
            Func<DataBatch> unused = ColumnGetter(0);
            Count = _readers[0].Count;

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

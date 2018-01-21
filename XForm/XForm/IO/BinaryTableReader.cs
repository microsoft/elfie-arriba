// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Query;
using XForm.Types;

namespace XForm.IO
{
    public class BinaryReaderColumn : IXColumn, IDisposable
    {
        private BinaryTableReader _table;
        private IStreamProvider _streamProvider;
        private IColumnReader _columnReader;
        public ColumnDetails ColumnDetails { get; private set; }

        public BinaryReaderColumn(BinaryTableReader table, ColumnDetails details, IStreamProvider streamProvider)
        {
            _table = table;
            _streamProvider = streamProvider;
            ColumnDetails = details;
        }

        public Func<XArray> CurrentGetter()
        {
            GetReader();
            return () => _columnReader.Read(_table.CurrentSelector);
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            GetReader(true);
            return (selector) => _columnReader.Read(selector);
        }

        public Func<XArray> ValuesGetter()
        {
            GetReader();
            if(_columnReader is EnumReader)
            {
                return () => ((EnumReader)_columnReader).Values();
            }

            return null;
        }

        public Type IndicesType
        {
            get
            {
                GetReader();
                if (_columnReader is EnumReader) return ((EnumReader)_columnReader).IndicesType;
                return null;
            }
        }

        public Func<XArray> IndicesCurrentGetter()
        {
            GetReader();
            if (_columnReader is EnumReader) return () => ((EnumReader)_columnReader).Indices(_table.CurrentSelector);
            return null;
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            GetReader(true);
            if (_columnReader is EnumReader) return (selector) => ((EnumReader)_columnReader).Indices(selector);
            return null;
        }

        private void GetReader(bool requireCached = false)
        {
            if (_columnReader != null) return;
            _columnReader = TypeProviderFactory.TryGetColumnReader(_streamProvider, ColumnDetails.Type, Path.Combine(_table.TablePath, ColumnDetails.Name), requireCached);
        }

        public override string ToString()
        {
            return XqlScanner.Escape(this.ColumnDetails.Name, TokenType.ColumnName);
        }

        public void Dispose()
        {
            if(_columnReader != null)
            {
                _columnReader.Dispose();
                _columnReader = null;
            }
        }
    }

    public class BinaryTableReader : ISeekableXTable
    {
        private TableMetadata _metadata;
        private BinaryReaderColumn[] _columns;

        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public BinaryTableReader(IStreamProvider streamProvider, string tableRootPath)
        {
            TablePath = tableRootPath;

            // Read metadata
            _metadata = TableMetadataSerializer.Read(streamProvider, TablePath);

            // Construct columns (files aren't opened until columns are subscribed to)
            _columns = new BinaryReaderColumn[_metadata.Schema.Count];
            for(int i = 0; i < _columns.Length; ++i)
            {
                _columns[i] = new BinaryReaderColumn(this, _metadata.Schema[i], streamProvider);
            }

            Reset();
        }

        public ArraySelector CurrentSelector => _currentEnumerateSelector;
        public IReadOnlyList<IXColumn> Columns => _columns;

        public string TablePath { get; private set; }
        public string Query => _metadata.Query;
        public int Count => _metadata.RowCount;
        public int CurrentRowCount { get; private set; }

        public int Next(int desiredCount)
        {
            _currentEnumerateSelector = _currentEnumerateSelector.NextPage(Count, desiredCount);
            _currentSelector = _currentEnumerateSelector;
            CurrentRowCount = _currentEnumerateSelector.Count;
            return CurrentRowCount;
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
            if (_columns != null)
            {
                foreach (BinaryReaderColumn column in _columns)
                {
                    if (column != null) column.Dispose();
                }

                _columns = null;
            }
        }
    }
}

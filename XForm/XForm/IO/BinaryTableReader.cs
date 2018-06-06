// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Columns;
using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Query;
using XForm.Types;

namespace XForm.IO
{
    public class BinaryReaderColumn : IXColumn, IDisposable
    {
        private bool _isCached;

        private BinaryTableReader _table;
        private IStreamProvider _streamProvider;
        private IColumnReader _columnReader;
        private EnumReader _enumReader;

        private Type _indicesType;
        private bool _loadedIndicesType;

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
            GetReader(CachingOption.Always);
            return (selector) => _columnReader.Read(selector);
        }

        public Func<XArray> ValuesGetter()
        {
            if (IndicesType == null) return null;
            GetReader();
            return _enumReader.Values;
        }

        public Func<object> ComponentGetter(string componentName)
        {
            if (componentName.Equals(ColumnComponent.String8Raw))
            {
                if (ColumnDetails.Type != typeof(String8)) return null;

                GetReader();
                String8ColumnReader reader = _columnReader as String8ColumnReader;
                if (reader == null) return null;

                return () => reader.ReadRaw(_table.CurrentSelector);
            }

            return null;
        }

        public Type IndicesType
        {
            get
            {
                // Important: Don't load the column reader itself to get the Indices type - this shouldn't trigger column caching
                if (!_loadedIndicesType)
                {
                    _indicesType = EnumReader.CheckIndicesType(_streamProvider, ColumnDetails.Type, Path.Combine(_table.TablePath, ColumnDetails.Name));
                    _loadedIndicesType = true;
                }

                return _indicesType;
            }
        }

        public Func<XArray> IndicesCurrentGetter()
        {
            if (IndicesType == null) return null;
            GetReader();
            return () => _enumReader.Indices(_table.CurrentSelector);
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            if (IndicesType == null) return null;
            GetReader(CachingOption.Always);
            return (selector) => _enumReader.Indices(selector);
        }

        private void GetReader(CachingOption option = CachingOption.AsConfigured)
        {
            // If we already have a reader with appropriate caching, keep using it
            if (_columnReader != null && (option != CachingOption.Always || _isCached == true)) return;

            // If we had a reader but need a cached one, Dispose the previous one
            if (_columnReader != null) _columnReader.Dispose();

            // Build the new reader and store a typed EnumReader copy.
            _columnReader = TypeProviderFactory.TryGetColumnReader(_streamProvider, ColumnDetails.Type, Path.Combine(_table.TablePath, ColumnDetails.Name), option);
            _enumReader = _columnReader as EnumReader;
            _isCached = (option == CachingOption.Always || (option == CachingOption.AsConfigured && ColumnCache.IsEnabled));
        }

        public override string ToString()
        {
            return XqlScanner.Escape(this.ColumnDetails.Name, TokenType.ColumnName);
        }

        public void Dispose()
        {
            if (_columnReader != null)
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

        private BinaryTableReader(IStreamProvider streamProvider, string tableRootPath)
        {
            TablePath = tableRootPath;
            _metadata = TableMetadataSerializer.Read(streamProvider, tableRootPath);

            // Construct columns (files aren't opened until columns are subscribed to)
            _columns = new BinaryReaderColumn[_metadata.Schema.Count];
            for (int i = 0; i < _columns.Length; ++i)
            {
                _columns[i] = new BinaryReaderColumn(this, _metadata.Schema[i], streamProvider);
            }

            Reset();
        }

        public static IXTable Build(IStreamProvider streamProvider, string tableRootPath)
        {
            TableMetadata metadata = TableMetadataSerializer.Read(streamProvider, tableRootPath);

            if (metadata.Partitions.Count > 0)
            {
                // If this table has partitions, load the parts (*allowing* recursive partitioning)
                // This allows partitioning by a column where one column value still will hit the size limit.
                return ConcatenatedTable.Build(metadata.Partitions.Select((partition) => BinaryTableReader.Build(streamProvider, Path.Combine(tableRootPath, partition))));
            }
            else
            {
                return new BinaryTableReader(streamProvider, tableRootPath);
            }
        }

        public ArraySelector CurrentSelector => _currentEnumerateSelector;
        public IReadOnlyList<IXColumn> Columns => _columns;

        public string TablePath { get; private set; }
        public string Query => _metadata.Query;
        public int Count => (int)_metadata.RowCount;
        public int CurrentRowCount { get; private set; }

        public int Next(int desiredCount, CancellationToken cancellationToken)
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

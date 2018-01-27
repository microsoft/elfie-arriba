// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using XForm.Data;
using XForm.Extensions;

namespace XForm.IO
{
    public class ConcatenatingColumn : IXColumn
    {
        private IXTable _table;

        private bool _isSubscribed;
        private List<IXColumn> _sources;
        private Func<XArray> _currentGetter;

        private Array _nullArray = null;

        public ColumnDetails ColumnDetails { get; private set; }

        public ConcatenatingColumn(IXTable table, ColumnDetails columnDetails)
        {
            _table = table;
            _sources = new List<IXColumn>();

            ColumnDetails = columnDetails;
            Allocator.AllocateToSize(ref _nullArray, 1, columnDetails.Type);
        }

        public void AddSource(IXColumn sourceColumn)
        {
            _sources.Add(sourceColumn);
        }

        public void SetCurrentSourceIndex(int index)
        {
            IXColumn source = _sources[index];
            if (_isSubscribed && source != null) _currentGetter = source.CurrentGetter();
        }

        public Func<XArray> CurrentGetter()
        {
            _isSubscribed = true;

            return () =>
            {
                if (_currentGetter != null) return _currentGetter();
                return XArray.Null(_nullArray, _table.CurrentRowCount);
            };
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            return null;
        }

        public Type IndicesType => null;

        public Func<XArray> ValuesGetter()
        {
            return null;
        }

        public Func<XArray> IndicesCurrentGetter()
        {
            return null;
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return null;
        }
    }

    /// <summary>
    ///  ConcatenatingReader groups together multiple IXTable sources and
    ///  returns the rows from them one by one, showing the union of all available columns.
    /// </summary>
    public class ConcatenatingReader : IXTable
    {
        private IXTable[] _sources;
        private List<ConcatenatingColumn> _columns;
        private int _currentSourceIndex;

        public ConcatenatingReader(IEnumerable<IXTable> sources)
        {
            _sources = sources.ToArray();
            _columns = new List<ConcatenatingColumn>();
            _currentSourceIndex = -1;

            IdentifyColumns();
        }

        private void IdentifyColumns()
        {
            // Find the union of all columns. Ensure the names match
            Dictionary<string, ColumnDetails> columnCollection = new Dictionary<string, ColumnDetails>(StringComparer.OrdinalIgnoreCase);
            foreach (IXTable source in _sources)
            {
                foreach (IXColumn sourceColumn in source.Columns)
                {
                    ColumnDetails existingColumnDetails;
                    if (columnCollection.TryGetValue(sourceColumn.ColumnDetails.Name, out existingColumnDetails))
                    {
                        if (!sourceColumn.ColumnDetails.Equals(existingColumnDetails))
                        {
                            throw new ArgumentException($"ConcatenatingReader couldn't combine sources because column {sourceColumn.ColumnDetails.Name} had different types from different sources.");
                        }
                    }
                    else
                    {
                        columnCollection.Add(sourceColumn.ColumnDetails.Name, sourceColumn.ColumnDetails);
                    }
                }
            }

            // Create a column for each, adding each source to it (or null for sources which didn't have it)
            foreach (string columnName in columnCollection.Keys)
            {
                ConcatenatingColumn column = new ConcatenatingColumn(this, columnCollection[columnName]);

                foreach (IXTable source in _sources)
                {
                    IXColumn sourceColumn;
                    source.Columns.TryFind(columnName, out sourceColumn);
                    column.AddSource(sourceColumn);
                }

                _columns.Add(column);
            }
        }

        public IReadOnlyList<IXColumn> Columns => _columns;
        public int CurrentRowCount { get; private set; }

        public int Next(int desiredCount)
        {
            if (_currentSourceIndex == -1) NextSource();

            while (_currentSourceIndex < _sources.Length)
            {
                // Try to get rows from the next source
                CurrentRowCount = _sources[_currentSourceIndex].Next(desiredCount);

                // If it had rows left, return them
                if (CurrentRowCount > 0) return CurrentRowCount;

                // If not, try the next source
                NextSource();
            }

            // If out of sources, return no rows
            return 0;
        }

        private void NextSource()
        {
            _currentSourceIndex++;

            if (_currentSourceIndex < _sources.Length)
            {
                // Update the source for each column
                for (int i = 0; i < _columns.Count; ++i)
                {
                    _columns[i].SetCurrentSourceIndex(_currentSourceIndex);
                }
            }
        }

        public void Reset()
        {
            _currentSourceIndex = -1;

            foreach (IXTable source in _sources)
            {
                source.Reset();
            }
        }

        public void Dispose()
        {
            if (_sources != null)
            {
                foreach (IXTable source in _sources)
                {
                    if (source != null) source.Dispose();
                }

                _sources = null;
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using XForm.Data;
using XForm.Extensions;

namespace XForm.IO
{
    /// <summary>
    ///  ConcatenatedTable groups together multiple IXTables and exposes them.
    ///  Unaware consumers can call Next to enumerate each source in turn.
    ///  Aware consumers can enumerate them in parallel or append to each of them
    ///  to allow subsequent stages to choose behavior.
    /// </summary>
    public class ConcatenatedTable : IXTable
    {
        private List<IXTable> _sources;
        private List<ConcatenatedColumn> _columns;
        private int _currentSourceIndex;

        private ConcatenatedTable(List<IXTable> sources)
        {
            _sources = sources;
            _columns = new List<ConcatenatedColumn>();
            _currentSourceIndex = -1;

            IdentifyColumns();
        }

        public static IXTable Build(IEnumerable<IXTable> sources)
        {
            // Unwrap any nested ConcatenatedTables
            List<IXTable> directSources = DirectSources(sources).ToList();

            // If only one source remains, return it
            if (directSources.Count == 1) return directSources[0];

            // Otherwise, wrap it in a ConcatenatedTable
            return new ConcatenatedTable(directSources);
        }

        /// <summary>
        ///  Recursively extract each non-ConcatenatedTable source from a list of sources.
        /// </summary>
        /// <param name="source">IXTable to add</param>
        private static IEnumerable<IXTable> DirectSources(IEnumerable<IXTable> sources)
        {
            foreach(IXTable source in sources)
            {
                ConcatenatedTable cSource = source as ConcatenatedTable;
                if (cSource == null)
                {
                    yield return source;
                }
                else
                {
                    foreach (IXTable inner in DirectSources(cSource.Sources))
                    {
                        yield return inner;
                    }
                }
            }
        }

        /// <summary>
        ///  Sources for this concatenated table. Callers may use Sources directly instead of reading this table.
        /// </summary>
        public IEnumerable<IXTable> Sources => _sources;

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
                ConcatenatedColumn column = new ConcatenatedColumn(this, columnCollection[columnName]);

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

        public int Next(int desiredCount, CancellationToken cancellationToken)
        {
            if (_currentSourceIndex == -1) NextSource();

            while (_currentSourceIndex < _sources.Count)
            {
                // Try to get rows from the next source
                CurrentRowCount = _sources[_currentSourceIndex].Next(desiredCount, cancellationToken);

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

            if (_currentSourceIndex < _sources.Count)
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

    /// <summary>
    ///  ConcatenatedColumn merges together columns from multiple sources,
    ///  exposing only 'Current' enumeration.
    /// </summary>
    public class ConcatenatedColumn : IXColumn
    {
        private IXTable _table;

        private bool _isSubscribed;
        private List<IXColumn> _sources;
        private Func<XArray> _currentGetter;

        private Array _nullArray = null;

        public ColumnDetails ColumnDetails { get; private set; }

        public ConcatenatedColumn(IXTable table, ColumnDetails columnDetails)
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
            if(index < _sources.Count)
            {
                IXColumn source = _sources[index];
                if (_isSubscribed) { _currentGetter = source?.CurrentGetter() ?? null; }
            }
            else
            {
                _currentGetter = null;
            }
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

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }
    }
}

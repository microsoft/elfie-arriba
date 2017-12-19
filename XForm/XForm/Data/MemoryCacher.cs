// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.IO;
using XForm.Query;

namespace XForm.Data
{
    public enum CacheLevel
    {
        Used,
        All
    }

    internal class MemoryCacheBuilder : IPipelineStageBuilder
    {
        public string Verb => "cache";
        public string Usage => "'cache'";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            IDataBatchList sourceList = source as IDataBatchList;
            if (sourceList == null) throw new ArgumentException("'cache' can only be used on IDataBatchList sources.");

            CacheLevel level = CacheLevel.Used;
            if (context.Parser.HasAnotherPart) level = context.Parser.NextEnum<CacheLevel>();
            return new MemoryCacher(sourceList, level);
        }
    }

    /// <summary>
    ///  MemoryCacher builds a full in-memory cache of the requested columns for an IDataBatchSource.
    ///  It's used to make operations which will need the full dataset, like joins, fast.
    /// </summary>
    public class MemoryCacher : IDataBatchList
    {
        private IDataBatchList _source;
        private IReadOnlyList<ColumnDetails> _columns;

        private bool _cacheBuilt;
        private ArrayTable _cache;
        private List<int> _requestedColumnSourceIndices;

        public MemoryCacher(IDataBatchList source, CacheLevel level)
        {
            _source = source;
            _columns = source.Columns;
            _cache = new ArrayTable(_source.Count);
            _requestedColumnSourceIndices = new List<int>();

            // If requested to cache everything, self-request all columns
            if (level == CacheLevel.All)
            {
                for (int i = 0; i < _columns.Count; ++i)
                {
                    ColumnGetter(i);
                }
            }
        }

        public int Count => (_cache != null ? _cache.Count : _source.Count);

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public int CurrentBatchRowCount { get; private set; }

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Track column indices requested from the source (only those will be cached)
            // and re-return the same getter if requested multiple times.
            int columnIndexInCache = _requestedColumnSourceIndices.IndexOf(columnIndex);
            if (columnIndexInCache == -1)
            {
                columnIndexInCache = _requestedColumnSourceIndices.Count;
                _requestedColumnSourceIndices.Add(columnIndex);
            }

            // Return the getter for the cache for the mapped column index
            return _cache.ColumnGetter(columnIndexInCache);
        }

        public void Get(ArraySelector selector)
        {
            _cache.Get(selector);
        }

        public int Next(int desiredCount)
        {
            // Build the cache only on the first 'Next' call
            if (!_cacheBuilt)
            {
                BuildCache();
                _cacheBuilt = true;
            }

            CurrentBatchRowCount = _cache.Next(desiredCount);
            return CurrentBatchRowCount;
        }

        private void BuildCache()
        {
            // Request getters for all columns to cache
            Func<DataBatch>[] cachedColumnGetters = new Func<DataBatch>[_requestedColumnSourceIndices.Count];
            for (int i = 0; i < _requestedColumnSourceIndices.Count; ++i)
            {
                cachedColumnGetters[i] = _source.ColumnGetter(_requestedColumnSourceIndices[i]);
            }

            // Request all rows
            int rowsGotten = _source.Next(_source.Count);
            if (rowsGotten != _source.Count) throw new NotImplementedException($"MemoryCacher unable to combine rows if they aren't all returned together. Requested {_source.Count}, got {rowsGotten}.");

            // Get each Array and associate with the ArrayEnumerator
            for (int i = 0; i < _requestedColumnSourceIndices.Count; ++i)
            {
                _cache.WithColumn(_source.Columns[_requestedColumnSourceIndices[i]], cachedColumnGetters[i]());
            }

            // Dispose the source as soon as the cache is built
            _source.Dispose();
            _source = null;
        }

        public void Reset()
        {
            // We'll just re-loop over the cache each time
            _cache.Reset();
        }

        public void Dispose()
        {
            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.IO;
using XForm.Query;

namespace XForm.Data
{
    internal class MemoryCacheBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "cache" };
        public string Usage => "'cache'";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            IDataBatchList sourceList = source as IDataBatchList;
            if (sourceList == null) throw new ArgumentException("'cache' can only be used on IDataBatchList sources.");

            return new MemoryCacher(sourceList);
        }
    }

    public class MemoryCacher : IDataBatchList
    {
        private IDataBatchList _source;
        private IReadOnlyList<ColumnDetails> _columns;

        private bool _cacheBuilt;
        private ArrayEnumerator _cache;
        private List<int> _requestedColumnSourceIndices;

        public MemoryCacher(IDataBatchList source)
        {
            _source = source;
            _columns = source.Columns;
            _cache = new ArrayEnumerator();
            _requestedColumnSourceIndices = new List<int>();
        }

        public int Count => (_cache != null ? _cache.Count : _source.Count);

        public IReadOnlyList<ColumnDetails> Columns => _columns;

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

            return _cache.Next(desiredCount);
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
            _source.Next(_source.Count);

            // Get each Array and associate with the ArrayEnumerator
            for (int i = 0; i < _requestedColumnSourceIndices.Count; ++i)
            {
                _cache.AddColumn(_source.Columns[_requestedColumnSourceIndices[i]], cachedColumnGetters[i]());
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

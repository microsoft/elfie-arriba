// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Types;

namespace XForm.IO
{
    public enum CachingOption
    {
        Always,         // Cache this item unconditionally (joins and seeks)
        AsConfigured,   // Cache this item if the column cache is enabled
        Never           // Don't cache this item ever (a container of it will be cached)
    }

    /// <summary>
    ///  ColumnCache provides optional in-memory caching for columns in typed arrays.
    ///  If ColumnCache.IsEnabled = true, columns using the ColumnCache are fully read in advance and served from CachedColumnReaders.
    /// </summary>
    public class ColumnCache
    {
        public static bool IsEnabled = false;
        public static ColumnCache Instance = new ColumnCache();
        private Cache<IColumnReader> _cache;

        private ColumnCache()
        {
            _cache = new Cache<IColumnReader>();
        }

        public IColumnReader GetOrBuild(string key, CachingOption option, Func<IColumnReader> build)
        {
            if (option == CachingOption.Always || (ColumnCache.IsEnabled && option == CachingOption.AsConfigured))
            {
                return _cache.GetOrBuild(key, null, () =>
                {
                    IColumnReader inner = build();
                    if (inner == null) return null;
                    if (inner is CachedColumnReader) return inner;
                    return new CachedColumnReader(inner);
                });
            }
            else
            {
                IColumnReader result;
                if (_cache.TryGet(key, out result)) return result;

                return build();
            }
        }
    }

    /// <summary>
    ///  CachedColumnReader implements IColumnReader for a column already retrieved into a single complete array.
    /// </summary>
    public class CachedColumnReader : IColumnReader
    {
        private XArray _column;
        private int[] _remapArray;

        public CachedColumnReader(XArray column)
        {
            _column = column;
        }

        public CachedColumnReader(IColumnReader inner)
        {
            using (inner)
            {
                _column = inner.Read(ArraySelector.All(inner.Count));
            }
        }

        public int Count => _column.Count;

        public XArray Read(ArraySelector selector)
        {
            return _column.Select(selector, ref _remapArray);
        }

        public void Dispose()
        { }
    }
}

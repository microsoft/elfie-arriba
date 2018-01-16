using System;
using XForm.Data;
using XForm.Types;

namespace XForm.IO
{
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
            _cache = new Cache<IColumnReader>(TimeSpan.FromMinutes(10));
        }

        public IColumnReader GetOrBuild(string key, Func<IColumnReader> build)
        {
            if (!IsEnabled) return build();
            return _cache.GetOrBuild(key, null, () =>
            {
                IColumnReader inner = build();
                if (inner == null) return null;
                if (inner is CachedColumnReader) return inner;
                return new CachedColumnReader(inner);
            });
        }

        public IColumnReader RequireCached(string key, Func<IColumnReader> build)
        {
            return _cache.GetOrBuild(key, null, () =>
            {
                IColumnReader inner = build();
                if (inner == null) return null;
                if (inner is CachedColumnReader) return inner;
                return new CachedColumnReader(inner);
            });
        }
    }

    /// <summary>
    ///  CachedColumnReader implements IColumnReader for a column already retrieved into a single complete array.
    /// </summary>
    public class CachedColumnReader : IColumnReader
    {
        private DataBatch _column;
        private int[] _remapArray;

        public CachedColumnReader(DataBatch column)
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

        public DataBatch Read(ArraySelector selector)
        {
            return _column.Select(selector, ref _remapArray);
        }

        public void Dispose()
        { }
    }
}

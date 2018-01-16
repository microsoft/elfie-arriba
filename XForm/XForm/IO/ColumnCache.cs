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
            return _cache.GetOrBuild(key, null, () => new CachedColumnReader(build()));
        }
    }

    /// <summary>
    ///  CachedColumnReader implements IColumnReader for a column already retrieved into a single complete array.
    /// </summary>
    public class CachedColumnReader : IColumnReader
    {
        private Array _array;
        private int _length;

        public CachedColumnReader(Array array, int length)
        {
            _array = array;
            _length = length;
        }

        public CachedColumnReader(IColumnReader inner)
        {
            using (inner)
            {
                DataBatch all = inner.Read(ArraySelector.All(inner.Count));
                _array = all.Array;
                _length = all.Count;
            }
        }

        public int Count => _length;

        public DataBatch Read(ArraySelector selector)
        {
            return DataBatch.All(_array, _length).Reselect(selector);
        }

        public void Dispose()
        { }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace XForm
{
    /// <summary>
    ///  CacheEntries are used to track items being cached within a timeout.
    /// </summary>
    /// <typeparam name="T">Type of items being cached</typeparam>
    public class CacheEntry<T>
    {
        public T Value { get; set; }
        public DateTime WhenModifiedUtc { get; set; }
        public DateTime WhenCachedUtc { get; set; }

        public CacheEntry(T value, DateTime whenModifiedUtc, DateTime whenCachedUtc)
        {
            this.Value = value;
            this.WhenModifiedUtc = whenModifiedUtc;
            this.WhenCachedUtc = whenCachedUtc;
        }
    }

    /// <summary>
    ///  Cache Provides convenient caching with configurable expiration and a separate
    ///  'whenModified' check to see if the expired value is still up-to-date.
    /// </summary>
    /// <typeparam name="T">Type of item to cache</typeparam>
    public class Cache<T>
    {
        public static TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(1);
        private Dictionary<string, CacheEntry<T>> _cache;
        private TimeSpan _expireAfter;

        public Cache() : this(DefaultCacheDuration)
        { }

        public Cache(TimeSpan expireAfter)
        {
            _cache = new Dictionary<string, CacheEntry<T>>(StringComparer.OrdinalIgnoreCase);
            _expireAfter = expireAfter;
        }

        public bool TryGet(string key, out T value)
        {
            CacheEntry<T> entry;
            if (_cache.TryGetValue(key, out entry) && (DateTime.UtcNow - entry.WhenCachedUtc) < _expireAfter)
            {
                value = entry.Value;
                return true;
            }

            value = default(T);
            return false;
        }

        public T GetOrBuild(string key, Func<DateTime> getWhenModifiedUtc, Func<T> build)
        {
            DateTime now = DateTime.UtcNow;
            DateTime liveWhenModifiedUtc = now;

            CacheEntry<T> entry;
            if (_cache.TryGetValue(key, out entry))
            {
                // If cache value isn't expired, return it
                if ((now - entry.WhenCachedUtc) < _expireAfter) return entry.Value;

                // Otherwise, check if the item has changed
                if (getWhenModifiedUtc != null)
                {
                    liveWhenModifiedUtc = getWhenModifiedUtc();

                    // If not, mark the entry fresh and return it again
                    if (liveWhenModifiedUtc <= entry.WhenModifiedUtc)
                    {
                        entry.WhenCachedUtc = now;
                        return entry.Value;
                    }
                }
            }
            else
            {
                if (getWhenModifiedUtc != null)
                {
                    liveWhenModifiedUtc = getWhenModifiedUtc();
                }
            }

            // Otherwise, rebuild and return
            entry = new CacheEntry<T>(build(), liveWhenModifiedUtc, now);
            _cache[key] = entry;

            return entry.Value;
        }

        public void Add(string key, T value)
        {
            DateTime now = DateTime.UtcNow;
            _cache[key] = new CacheEntry<T>(value, now, now);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }
    }
}

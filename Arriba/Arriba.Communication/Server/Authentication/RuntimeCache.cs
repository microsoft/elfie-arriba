// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Caching;

namespace Arriba.Server.Authentication
{
    /// <summary>
    /// Simple runtime object cache. 
    /// </summary>
    internal class RuntimeCache : IDisposable
    {
        private MemoryCache _cache;
        private TimeSpan _maximumTimeToLive = TimeSpan.FromDays(180);

        public RuntimeCache(string name)
        {
            _cache = new MemoryCache(name);
        }

        /// <summary>
        /// Gets an existing cache item for the specified key, if it does not exist a cache item is added by running the specified production. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Cache item key.</param>
        /// <param name="production">Function to call in the case of cache miss.</param>
        /// <param name="timeToLive">Time for the cache item to live.</param>
        /// <returns>Cache item.</returns>
        public T GetOrAdd<T>(string key, Func<T> production, TimeSpan? timeToLive = null)
        {
            object value = _cache.Get(key);

            if (value == null)
            {
                value = production();
                _cache.Add(key, value, this.CreatePolicyForValue(value, timeToLive));
            }

            return (T)value;
        }

        /// <summary>
        /// Removes and returns the specified cache item. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Cache item key.</param>
        /// <returns>Cache item.</returns>
        public T Remove<T>(string key)
        {
            return (T)_cache.Remove(key);
        }

        /// <summary>
        /// Creates a sliding expiration policy for the specified cache item value. 
        /// </summary>
        /// <param name="value">Value to cache.</param>
        /// <param name="timeToLive">Time to live for the value.</param>
        /// <returns>Cache item policy.</returns>
        private CacheItemPolicy CreatePolicyForValue(object value, TimeSpan? timeToLive)
        {
            var policy = new CacheItemPolicy();
            policy.SlidingExpiration = timeToLive ?? _maximumTimeToLive;

            // If the value is IDisposable, dispose of the item when it removed from the cache. 
            if (value is IDisposable)
            {
                policy.RemovedCallback = (args) =>
                    {
                        ((IDisposable)args.CacheItem.Value).Dispose();
                    };
            }

            return policy;
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}

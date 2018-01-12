// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace XForm
{
    /// <summary>
    ///  Dictionary5 is a Dictionary using Robin Hood hashing to provide good insert and search performance
    ///  with much lower memory use than .NET HashSet.
    ///  
    ///  Dictionary5 adds one byte of overhead and stays >= 75% full for large sizes;
    ///  Dictionary has 8 bytes overhead [cached hashcode and next node] and resizes more each time.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Dictionary5O<T, U> : IEnumerable<T>
    {
        public int Count { get; private set; }
        public int MaxProbeLength { get; private set; }

        private IEqualityComparer<T> _comparer;

        // The key values themselves
        private T[] _keys;
        private U[] _values;

        // Metadata stores the probe length in the upper four bits and the probe increment in the lower four bits
        private byte[] _metadata;

        // Items can be a maximum of 14 buckets from the initial bucket they hash to, so the probe length fits in four bits with a sentinel zero
        private const int ProbeLengthLimit = 14;

        // The HashSet is a minimum of 28 items, which is size 32 with overhead.
        private const int MinimumCapacity = 28;

        // The HashSet is sized to (Capacity + Capacity >> CapacityOverheadShift), so 1 1/8 of base size for shift 3.
        private const int CapacityOverheadShift = 3;

        public Dictionary5O(IEqualityComparer<T> comparer, int capacity = -1)
        {
            _comparer = comparer;
            if (capacity < MinimumCapacity) capacity = MinimumCapacity;
            Reset(capacity + (capacity >> CapacityOverheadShift) + 1);
        }

        private void Reset(int size)
        {
            _keys = new T[size];
            _values = new U[size];
            _metadata = new byte[size];

            this.Count = 0;
            this.MaxProbeLength = 0;
        }

        public void Clear()
        {
            Array.Clear(_keys, 0, _keys.Length);
            Array.Clear(_values, 0, _values.Length);
            Array.Clear(_metadata, 0, _metadata.Length);

            this.Count = 0;
            this.MaxProbeLength = 0;
        }

        // Find the average distance items are from their target buckets. Debuggability.
        public double DistanceMean()
        {
            ulong distance = 0;
            for (int i = 0; i < _metadata.Length; ++i)
            {
                if (_metadata[i] > 0) distance += (ulong)(_metadata[i] >> 4);
            }

            return ((double)distance / (double)this.Count);
        }

        private uint Hash(T value)
        {
            return unchecked((uint)_comparer.GetHashCode(value));
        }

        private uint Bucket(uint hash)
        {
            // Use Lemire method to convert hash [0, 2^32) to [0, N) without modulus.
            // If hash is [0, 2^32), then N*hash is [0, N*2^32], and (N*hash)/2^32 is [0, N).
            // This uses the high bits of the hash, so the high bits need to vary and all be set. (Incrementing integers and non-negative integers are both bad).
            return (uint)(((ulong)hash * (ulong)_metadata.Length) >> 32);
        }

        private uint Increment(uint hashOrMetadata)
        {
            // Linear Probing with the low four bits of the hash.
            // This causes only 1/16 of initially colliding values to re-collide, reducing the variance of the probe length.
            return (hashOrMetadata & 15) + 1;
        }

        /// <summary>
        ///  Return whether this Dictionary contains the given key.
        /// </summary>
        /// <param name="key">Value to find</param>
        /// <returns>True if in set, False otherwise</returns>
        public bool Contains(T key)
        {
            return IndexOf(key) != -1;
        }

        private int IndexOf(T key)
        {
            uint hash = Hash(key);
            uint bucket = Bucket(hash);
            uint increment = Increment(hash);

            // To find a key, just compare every key starting with the expected bucket
            // up to the farthest any key had to be moved from the desired bucket.
            for (int probeLength = 1; probeLength <= this.MaxProbeLength; ++probeLength)
            {
                if (_keys[bucket].Equals(key)) return (int)bucket;

                bucket += increment;
                if (bucket >= _metadata.Length) bucket -= (uint)_metadata.Length;
            }

            return -1;
        }

        /// <summary>
        ///  Remove the given key from the Dictionary.
        /// </summary>
        /// <param name="key">Value to remove</param>
        /// <returns>True if removed, False if not found</returns>
        public bool Remove(T key)
        {
            int index = IndexOf(key);
            if (index == -1) return false;

            // To remove a key, just clear the key and wealth.
            // Searches don't stop on empty buckets, so this is safe.
            _metadata[index] = 0;
            _keys[index] = default(T);
            _values[index] = default(U);
            this.Count--;

            return true;
        }

        /// <summary>
        ///  Add the given value to the set.
        /// </summary>
        /// <param name="key">Key to add</param>
        /// <param name="key">Value to add</param>
        public void Add(T key, U value)
        {
            // If the table is too close to full, expand it. Very full tables cause slower inserts as many items are shifted.
            if (this.Count >= (_metadata.Length - (_metadata.Length >> CapacityOverheadShift))) Expand();

            uint hash = Hash(key);
            uint bucket = Bucket(hash);
            uint increment = Increment(hash);

            for (int probeLength = 1; probeLength <= ProbeLengthLimit; ++probeLength)
            {
                int metadataFound = _metadata[bucket];
                int probeLengthFound = (metadataFound >> 4);

                if (probeLengthFound == 0)
                {
                    // If we found an empty cell (probe zero), add the item and return
                    _metadata[bucket] = (byte)((probeLength << 4) + increment - 1);
                    _keys[bucket] = key;
                    _values[bucket] = value;
                    if (probeLength > this.MaxProbeLength) this.MaxProbeLength = probeLength;
                    this.Count++;

                    return;
                }
                else if (probeLengthFound < probeLength)
                {
                    // If we found an item with a higher wealth, put the new item here and move the existing one
                    T keyMoved = _keys[bucket];
                    U valueMoved = _values[bucket];

                    _metadata[bucket] = (byte)((probeLength << 4) + increment - 1);
                    _keys[bucket] = key;
                    _values[bucket] = value;
                    if (probeLength > this.MaxProbeLength) this.MaxProbeLength = probeLength;

                    key = keyMoved;
                    value = valueMoved;
                    probeLength = probeLengthFound;
                    increment = Increment((uint)metadataFound);
                }
                else if (probeLengthFound == probeLength)
                {
                    // If this is a duplicate of the new item, stop
                    if (_keys[bucket].Equals(key))
                    {
                        _values[bucket] = value;
                        return;
                    }
                }

                bucket += increment;
                if (bucket >= _metadata.Length) bucket -= (uint)_metadata.Length;
            }

            // If we had to move an item more than the maximum distance from the desired bucket, we need to resize
            Expand();

            // If we resized, re-add the new value (recalculating the bucket for the new size)
            Add(key, value);
        }

        private void Expand()
        {
            // Expand the array to 1.5x the current size up to 1M items, 1.125x the current size thereafter
            int newSize = _metadata.Length + (_metadata.Length >= 1048576 ? _metadata.Length >> 3 : _metadata.Length >> 1);

            // Save the current contents
            T[] oldKeys = _keys;
            U[] oldValues = _values;
            byte[] oldWealth = _metadata;

            // Allocate the larger table
            Reset(newSize);

            // Add items to the enlarged table
            for (int i = 0; i < oldWealth.Length; ++i)
            {
                if (oldWealth[i] > 0) Add(oldKeys[i], oldValues[i]);
            }
        }

        public struct DictionaryEnumerator<V, W> : IEnumerator<V>
        {
            private Dictionary5O<V, W> _set;
            private int _nextBucket;

            public V Current => _set._keys[_nextBucket];
            object IEnumerator.Current => _set._keys[_nextBucket];

            public DictionaryEnumerator(Dictionary5O<V, W> set)
            {
                _set = set;
                _nextBucket = -1;
            }

            public void Dispose()
            { }

            public bool MoveNext()
            {
                while (++_nextBucket < _set._metadata.Length)
                {
                    if (_set._metadata[_nextBucket] >= 0x10) return true;
                }

                return false;
            }

            public void Reset()
            {
                _nextBucket = -1;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new DictionaryEnumerator<T, U>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new DictionaryEnumerator<T, U>(this);
        }
    }
}

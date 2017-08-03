// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace V5
{
    // https://en.wikipedia.org/wiki/MurmurHash, hardcoded for only 32-bit values
    public static class MurmurHasher
    {
        public static uint Murmur3(uint value, uint seed)
        {
            uint h = seed;

            uint k = value;
            k *= 0xcc9e2d51;
            k = (k << 15) | (k >> 17);
            k *= 0x1b873593;
            h ^= k;
            h = (h << 13) | (h >> 19);
            h = (h * 5) + 0xe6546b64;

            h ^= 4;
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;

            return h;
        }

        private const uint M = 0x5bd1e995;
        private const int R = 24;

        public static uint Murmur2(uint value, uint seed)
        {
            uint h = seed ^ 4;

            uint k = value;
            k *= M;
            k ^= k >> R;
            k *= M;

            h *= M;
            h ^= k;

            h ^= h >> 13;
            h *= M;
            h ^= h >> 15;

            return h;
        }
    }

    /// <summary>
    ///  HashSet5 is a HashSet using Robin Hood hashing to provide good insert and search performance
    ///  with much lower memory use than .NET HashSet.
    ///  
    ///  HashSet5 adds one byte of overhead and stays >= 75% full for large sizes;
    ///  HashSet has 8 bytes overhead [cached hashcode and next node] and resizes more each time.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HashSet5<T> : IEnumerable<T> where T : IEquatable<T>
    {
        public int Count { get; private set; }
        public int LowestWealth { get; private set; }

        private T[] Values;
        private byte[] Wealth;

        public HashSet5(int capacity = -1)
        {
            if (capacity < 28) capacity = 28;
            Reset(capacity + (capacity >> 3) + 1);
        }

        private void Reset(int size)
        {
            this.Values = new T[size];
            this.Wealth = new byte[size];

            this.Count = 0;
            this.LowestWealth = 255;
        }

        public void Clear()
        {
            Array.Clear(this.Wealth, 0, this.Wealth.Length);
            Array.Clear(this.Values, 0, this.Values.Length);

            this.Count = 0;
            this.LowestWealth = 255;
        }

        // Find the average distance items are from their target buckets. Debuggability.
        public double DistanceMean()
        {
            ulong distance = 0;
            for(int i = 0; i < this.Wealth.Length; ++i)
            {
                if (this.Wealth[i] > 0) distance += (ulong)(255 - this.Wealth[i]);
            }

            return ((double)distance / (double)this.Count);
        }

        // Return the counts of distance from desired bucket for each item. Debuggability.
        public int[] DistanceDistribution()
        {
            int[] result = new int[256];
            for (int i = 0; i < this.Wealth.Length; ++i)
            {
                if (this.Wealth[i] > 0)
                {
                    result[255 - this.Wealth[i]]++;
                }
            }

            return result;
        }

        private uint Hash(T value)
        {
            //return (uint)value.GetHashCode();
            //return (uint)(Arriba.Hashing.MurmurHash3(((ulong)value.GetHashCode()), 0) & uint.MaxValue);
            //return MurmurHasher.Murmur2((uint)value.GetHashCode(), 0);
            return MurmurHasher.Murmur3((uint)value.GetHashCode(), 0);
        }

        private uint Bucket(uint hash)
        {
            // Use Lemire method to convert hash [0, 2^32) to [0, N) without modulus.
            // If hash is [0, 2^32), then N*hash is [0, N*2^32], and (N*hash)/2^32 is [0, N).
            // NOTE: This method uses the top bits of the hash only, so small integers with GetHashCode(i) == i will perform terribly.
            return (uint)(((ulong)hash * (ulong)this.Wealth.Length) >> 32);

            // Simple modulus. Good for small incrementing integers but 33% slower insert for integers.
            //return (uint)(hash % this.Wealth.Length);
        }

        private uint NextBucket(uint bucket, uint hash, int wealth)
        {
            // Murmur hashing with changing seed
            //hash = (uint)(Arriba.Hashing.MurmurHash3((ulong)hash, (uint)(256 - wealth)) & uint.MaxValue);
            //hash = MurmurHasher.Hash(hash, (uint)(256 - wealth));
            //return (uint)(((ulong)hash * (ulong)this.Wealth.Length) >> 32);

            // Sequential Probing - bad variance but fast due to cache coherency
            //if (++bucket >= this.Wealth.Length) bucket = 0;

            // Linear Probing with next hash bits
            int increment = (int)(hash & 15) + 1;
            bucket += (uint)increment;
            if (bucket >= this.Wealth.Length) bucket -= (uint)this.Wealth.Length;

            return bucket;
        }

        /// <summary>
        ///  Return whether this set contains the given value.
        /// </summary>
        /// <param name="value">Value to find</param>
        /// <returns>True if in set, False otherwise</returns>
        public bool Contains(T value)
        {
            return IndexOf(value) != -1;
        }

        private int IndexOf(T value)
        {
            uint hash = Hash(value);
            uint bucket = Bucket(hash);

            // To find a value, just compare every value starting with the expected bucket
            // up to the farthest any value had to be moved from the desired bucket.
            for (int wealth = 255; wealth >= this.LowestWealth; --wealth)
            {
                if (this.Values[bucket].Equals(value)) return (int)bucket;
                bucket = NextBucket(bucket, hash, wealth);
            }

            return -1;
        }

        /// <summary>
        ///  Remove the given value from the set.
        /// </summary>
        /// <param name="value">Value to remove</param>
        /// <returns>True if removed, False if not found</returns>
        public bool Remove(T value)
        {
            int index = IndexOf(value);
            if (index == -1) return false;

            // To remove a value, just clear the value and wealth.
            // Searches don't stop on empty buckets, so this is safe.
            this.Wealth[index] = 0;
            this.Values[index] = default(T);
            this.Count--;

            return true;
        }

        /// <summary>
        ///  Add the given value to the set.
        /// </summary>
        /// <param name="value">Value to add</param>
        /// <returns>True if added, False if value was already in set</returns>
        public bool Add(T value)
        {
            // If the table is more than 7/8 full, expand it. 
            // Very full tables cause slower inserts as many items are shifted.
            if (this.Count >= (this.Wealth.Length - (this.Wealth.Length >> 3))) Expand();

            uint hash = Hash(value);
            uint bucket = Bucket(hash);

            for(int wealth = 255; wealth > 0; --wealth)            
            {
                byte wealthFound = this.Wealth[bucket];

                if (wealthFound == 0)
                {
                    // If we found an empty cell (wealth zero), add the item and return
                    this.Wealth[bucket] = (byte)wealth;
                    this.Values[bucket] = value;
                    if (wealth < this.LowestWealth) this.LowestWealth = wealth;
                    this.Count++;

                    return true;
                }
                else if (wealthFound > wealth)
                {
                    // If we found an item with a higher wealth, put the new item here and move the existing one
                    T valueMoved = this.Values[bucket];

                    this.Wealth[bucket] = (byte)wealth;
                    this.Values[bucket] = value;
                    if (wealth < this.LowestWealth) this.LowestWealth = wealth;

                    value = valueMoved;
                    wealth = wealthFound;
                    hash = Hash(value);
                }
                else if(wealthFound == wealth)
                {
                    // If this is a duplicate of the new item, stop
                    if (this.Values[bucket].Equals(value)) return false;
                }

                bucket = NextBucket(bucket, hash, wealth);
            }
        
            // If we had to move an item more than 255 from the desired bucket, we need to resize
            Expand();

            // If we resized, re-add the new value (recalculating the bucket for the new size)
            return Add(value);
        }

        private void Expand()
        {
            // Expand the array to 1.5x the current size up to 1M items, 1.125x the current size thereafter
            int sizeShiftAmount = (this.Wealth.Length >= 1048576 ? 3 : 1);
            int newSize = this.Wealth.Length + (this.Wealth.Length >> sizeShiftAmount);

            // Save the current contents
            T[] oldValues = this.Values;
            byte[] oldWealth = this.Wealth;

            // Allocate the larger table
            Reset(newSize);

            // Add items to the enlarged table
            for (int i = 0; i < oldWealth.Length; ++i)
            {
                if (oldWealth[i] > 0) Add(oldValues[i]);
            }
        }

        public struct HashSet5Enumerator<U> : IEnumerator<U> where U : IEquatable<U>
        {
            private HashSet5<U> Set;
            private int NextBucket;

            public U Current => this.Set.Values[this.NextBucket];
            object IEnumerator.Current => this.Set.Values[this.NextBucket];

            public HashSet5Enumerator(HashSet5<U> set)
            {
                this.Set = set;
                this.NextBucket = -1;
            }

            public void Dispose()
            { }

            public bool MoveNext()
            {
                while(++this.NextBucket < this.Set.Wealth.Length)
                {
                    if (this.Set.Wealth[this.NextBucket] > 0) return true;
                }

                return false;
            }

            public void Reset()
            {
                this.NextBucket = 0;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new HashSet5Enumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new HashSet5Enumerator<T>(this);
        }
    }
}

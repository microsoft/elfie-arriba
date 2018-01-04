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
    public class Dictionary<T> : IEnumerable<T> where T : IEquatable<T>
    {
        public int Count { get; private set; }
        public int MaxProbeLength { get; private set; }

        // The key values themselves
        private T[] Keys;

        // Metadata stores the probe length in the upper four bits and the probe increment in the lower four bits
        private byte[] Metadata;

        // Items can be a maximum of 14 buckets from the initial bucket they hash to, so the probe length fits in four bits with a sentinel zero
        private const int ProbeLengthLimit = 14;

        // The HashSet is a minimum of 28 items, which is size 32 with overhead.
        private const int MinimumCapacity = 28;

        // The HashSet is sized to (Capacity + Capacity >> CapacityOverheadShift), so 1 1/8 of base size for shift 3.
        private const int CapacityOverheadShift = 3;

        public Dictionary(int capacity = -1)
        {
            if (capacity < MinimumCapacity) capacity = MinimumCapacity;
            Reset(capacity + (capacity >> CapacityOverheadShift) + 1);
        }

        private void Reset(int size)
        {
            this.Keys = new T[size];
            this.Metadata = new byte[size];

            this.Count = 0;
            this.MaxProbeLength = 0;
        }

        public void Clear()
        {
            Array.Clear(this.Metadata, 0, this.Metadata.Length);
            Array.Clear(this.Keys, 0, this.Keys.Length);

            this.Count = 0;
            this.MaxProbeLength = 0;
        }

        // Find the average distance items are from their target buckets. Debuggability.
        public double DistanceMean()
        {
            ulong distance = 0;
            for(int i = 0; i < this.Metadata.Length; ++i)
            {
                if (this.Metadata[i] > 0) distance += (ulong)(this.Metadata[i] >> 4);
            }

            return ((double)distance / (double)this.Count);
        }

        private unsafe uint Hash(T value)
        {
            return unchecked((uint)Hashing.Hash((ulong)value.GetHashCode(), 0));
        }

        private uint Bucket(uint hash)
        {
            // Use Lemire method to convert hash [0, 2^32) to [0, N) without modulus.
            // If hash is [0, 2^32), then N*hash is [0, N*2^32], and (N*hash)/2^32 is [0, N).
            // This uses the high bits of the hash, so the high bits need to vary and all be set. (Incrementing integers and non-negative integers are both bad).
            return (uint)(((ulong)hash * (ulong)this.Metadata.Length) >> 32);
        }

        private uint Increment(uint hashOrMetadata)
        {
            // Linear Probing with the low four bits of the hash.
            // This causes only 1/16 of initially colliding values to re-collide, reducing the variance of the probe length.
            return (hashOrMetadata & 15) + 1;
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
            uint increment = Increment(hash);

            // To find a value, just compare every value starting with the expected bucket
            // up to the farthest any value had to be moved from the desired bucket.
            for (int probeLength = 1; probeLength <= this.MaxProbeLength; ++probeLength)
            {
                if (this.Keys[bucket].Equals(value)) return (int)bucket;

                bucket += increment;
                if (bucket >= this.Metadata.Length) bucket -= (uint)this.Metadata.Length;
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
            this.Metadata[index] = 0;
            this.Keys[index] = default(T);
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
            // If the table is too close to full, expand it. Very full tables cause slower inserts as many items are shifted.
            if (this.Count >= (this.Metadata.Length - (this.Metadata.Length >> CapacityOverheadShift))) Expand();

            uint hash = Hash(value);
            uint bucket = Bucket(hash);
            uint increment = Increment(hash);

            for (int probeLength = 1; probeLength <= ProbeLengthLimit; ++probeLength)
            {
                int metadataFound = this.Metadata[bucket];
                int probeLengthFound = (metadataFound >> 4);

                if (probeLengthFound == 0)
                {
                    // If we found an empty cell (probe zero), add the item and return
                    this.Metadata[bucket] = (byte)((probeLength << 4) + increment - 1);
                    this.Keys[bucket] = value;
                    if (probeLength > this.MaxProbeLength) this.MaxProbeLength = probeLength;
                    this.Count++;

                    return true;
                }
                else if (probeLengthFound < probeLength)
                {
                    // If we found an item with a higher wealth, put the new item here and move the existing one
                    T valueMoved = this.Keys[bucket];

                    this.Metadata[bucket] = (byte)((probeLength << 4) + increment - 1);
                    this.Keys[bucket] = value;
                    if (probeLength > this.MaxProbeLength) this.MaxProbeLength = probeLength;

                    value = valueMoved;
                    probeLength = probeLengthFound;
                    increment = Increment((uint)metadataFound);
                }
                else if (probeLengthFound == probeLength)
                {
                    // If this is a duplicate of the new item, stop
                    if (this.Keys[bucket].Equals(value)) return false;
                }

                bucket += increment;
                if (bucket >= this.Metadata.Length) bucket -= (uint)this.Metadata.Length;
            }

            // If we had to move an item more than the maximum distance from the desired bucket, we need to resize
            Expand();

            // If we resized, re-add the new value (recalculating the bucket for the new size)
            return Add(value);
        }

        private void Expand()
        {
            // Expand the array to 1.5x the current size up to 1M items, 1.125x the current size thereafter
            int newSize = this.Metadata.Length + (this.Metadata.Length >= 1048576 ? this.Metadata.Length >> 3 : this.Metadata.Length >> 1);

            // Save the current contents
            T[] oldValues = this.Keys;
            byte[] oldWealth = this.Metadata;

            // Allocate the larger table
            Reset(newSize);

            // Add items to the enlarged table
            for (int i = 0; i < oldWealth.Length; ++i)
            {
                if (oldWealth[i] > 0) Add(oldValues[i]);
            }
        }

        public struct DictionaryEnumerator<U> : IEnumerator<U> where U : IEquatable<U>
        {
            private Dictionary<U> Set;
            private int NextBucket;

            public U Current => this.Set.Keys[this.NextBucket];
            object IEnumerator.Current => this.Set.Keys[this.NextBucket];

            public DictionaryEnumerator(Dictionary<U> set)
            {
                this.Set = set;
                this.NextBucket = -1;
            }

            public void Dispose()
            { }

            public bool MoveNext()
            {
                while(++this.NextBucket < this.Set.Metadata.Length)
                {
                    if (this.Set.Metadata[this.NextBucket] >= 0x10) return true;
                }

                return false;
            }

            public void Reset()
            {
                this.NextBucket = -1;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new DictionaryEnumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new DictionaryEnumerator<T>(this);
        }
    }
}

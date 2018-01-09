// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace XForm
{
    public class Dictionary52<T, U>
    {
        private IEqualityComparer<T> Comparer;
        private HashCore Core { get; set; }

        private T[] Keys { get; set; }
        private U[] Values { get; set; }

        private T CurrentKey;
        private U CurrentValue;

        public Dictionary52(IEqualityComparer<T> comparer, int initialCapacity = -1)
        {
            this.Comparer = comparer;
            this.Core = new HashCore();
            Reset(HashCore.SizeForCapacity(initialCapacity));
        }

        private void Reset(int size)
        {
            this.Core.Reset(size);
            this.Keys = new T[size];
            this.Values = new U[size];
        }

        private void Expand()
        {
            // Save the current Keys/Values/Metadata
            T[] oldKeys = this.Keys;
            U[] oldValues = this.Values;
            byte[] oldMetaData = this.Core.Metadata;

            // Expand the table
            Reset(HashCore.ResizeToSize(this.Keys.Length));

            // Add items to the enlarged table
            for (int i = 0; i < oldMetaData.Length; ++i)
            {
                if (oldMetaData[i] > 0) Add(oldKeys[i], oldValues[i]);
            }
        }

        #region Methods HashCore needs
        private uint HashCurrent()
        {
            return unchecked((uint)Comparer.GetHashCode(CurrentKey));
        }

        private bool EqualsCurrent(uint index)
        {
            return Comparer.Equals(this.Keys[index], CurrentKey);
        }

        private void SwapWithCurrent(uint index)
        {
            T swapKey = this.Keys[index];
            U swapValue = this.Values[index];

            this.Keys[index] = CurrentKey;
            this.Values[index] = CurrentValue;

            CurrentKey = swapKey;
            CurrentValue = swapValue;
        }
        #endregion

        #region Public Members
        /// <summary>
        ///  Return the number of items currently in the Dictionary
        /// </summary>
        public int Count => this.Core.Count;

        /// <summary>
        ///  Remove all items from the Dictionary, retaining the allocated size.
        /// </summary>
        public void Clear()
        {
            this.Core.Clear();
            Array.Clear(this.Keys, 0, this.Keys.Length);
            Array.Clear(this.Values, 0, this.Values.Length);
        }

        /// <summary>
        ///  Return whether this Dictionary contains the given key.
        /// </summary>
        /// <param name="key">Value to find</param>
        /// <returns>True if in set, False otherwise</returns>
        public bool ContainsKey(T key)
        {
            CurrentKey = key;
            return this.Core.IndexOf(HashCurrent(), EqualsCurrent) != -1;
        }

        /// <summary>
        ///  Add the given value to the set.
        /// </summary>
        /// <param name="key">Key to add</param>
        /// <param name="key">Value to add</param>
        public void Add(T key, U value)
        {
            CurrentKey = key;
            CurrentValue = value;

            if (!this.Core.Add(HashCurrent(), EqualsCurrent, SwapWithCurrent))
            {
                Expand();

                CurrentKey = key;
                CurrentValue = value;
                this.Core.Add(HashCurrent(), EqualsCurrent, SwapWithCurrent);
            }
        }

        /// <summary>
        ///  Remove the given key from the Dictionary.
        /// </summary>
        /// <param name="key">Value to remove</param>
        /// <returns>True if removed, False if not found</returns>
        public bool Remove(T key)
        {
            CurrentKey = key;
            int index = this.Core.IndexOf(HashCurrent(), EqualsCurrent);
            if (index == -1) return false;

            this.Core.Remove(index);
            this.Keys[index] = default(T);
            this.Values[index] = default(U);
            return true;
        }

        public IEnumerable<T> AllKeys
        {
            get
            {
                for(int index = 0; index < this.Keys.Length; ++index)
                {
                    if(this.Core.Metadata[index] > 0)
                    {
                        yield return this.Keys[index];
                    }
                }
            }
        }
        #endregion
    }

    public class HashCore
    {
        public int Count { get; private set; }
        public int MaxProbeLength { get; private set; }

        // Metadata stores the probe length in the upper four bits and the probe increment in the lower four bits
        public byte[] Metadata { get; private set; }

        // Items can be a maximum of 14 buckets from the initial bucket they hash to, so the probe length fits in four bits with a sentinel zero
        private const int ProbeLengthLimit = 14;

        public HashCore()
        { }

        public static int SizeForCapacity(int capacity)
        {
            // Minimum capacity is 28 items, which is a 32-element array
            if (capacity < 28) return 32;
            
            // Size to 1/8 over capacity so the table is just under 90% filled at the configured capacity
            return capacity + (capacity >> 3) + 1;
        }

        public static int ResizeToSize(int currentSize)
        {
            // Grow by 1/2 under 1M items and 1/8 when over
            return currentSize + (currentSize >= 1048576 ? currentSize >> 3 : currentSize >> 1);
        }

        public void Reset(int size)
        {
            this.Metadata = new byte[size];
            this.Count = 0;
            this.MaxProbeLength = 0;
        }

        public void Clear()
        {
            Array.Clear(this.Metadata, 0, this.Metadata.Length);
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

        public int IndexOf(uint hash, Func<uint, bool> equals)
        {
            uint bucket = Bucket(hash);
            uint increment = Increment(hash);

            // To find a key, just compare every key starting with the expected bucket
            // up to the farthest any key had to be moved from the desired bucket.
            for (int probeLength = 1; probeLength <= this.MaxProbeLength; ++probeLength)
            {
                if (equals(bucket)) return (int)bucket;

                bucket += increment;
                if (bucket >= this.Metadata.Length) bucket -= (uint)this.Metadata.Length;
            }

            return -1;
        }

        public void Remove(int bucket)
        {
            // To remove a key, just clear the key and wealth.
            // Searches don't stop on empty buckets, so this is safe.
            this.Metadata[bucket] = 0;
            this.Count--;
        }

        public bool Add(uint hash, Func<uint, bool> equalsCurrent, Action<uint> swapWithCurrent)
        {
            // If the table is too close to full, expand it. Very full tables cause slower inserts as many items are shifted.
            if (this.Metadata.Length < SizeForCapacity(this.Count)) return false;

            // Find the bucket and probe increment for the new item
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
                    swapWithCurrent(bucket);

                    // Track the max probe length
                    if (probeLength > this.MaxProbeLength) this.MaxProbeLength = probeLength;

                    // Increment the count for the new item
                    this.Count++;

                    return true;
                }
                else if (probeLengthFound < probeLength)
                {
                    // If we found an item with a higher wealth, put the new item here
                    swapWithCurrent(bucket);
                    this.Metadata[bucket] = (byte)((probeLength << 4) + increment - 1);

                    // Track the max probe length
                    if (probeLength > this.MaxProbeLength) this.MaxProbeLength = probeLength;

                    // Get the swapped out item probe length and increment and loop to insert it
                    probeLength = probeLengthFound;
                    increment = Increment((uint)metadataFound);
                }
                else if (probeLengthFound == probeLength)
                {
                    // If this is a duplicate of the new item, reset the value and stop
                    if (equalsCurrent(bucket))
                    {
                        swapWithCurrent(bucket);
                        return true;
                    }
                }

                // Find the the next valid bucket for the current item to place
                bucket += increment;
                if (bucket >= this.Metadata.Length) bucket -= (uint)this.Metadata.Length;
            }

            // If we couldn't find a place for this item, a resize is required
            return false;
        }
    }
}

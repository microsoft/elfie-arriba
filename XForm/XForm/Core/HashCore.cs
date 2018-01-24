// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Types;

namespace XForm
{
    /// <summary>
    ///  EqualityComparerAdapter turns an IXArrayComparer&lt;T&gt; into an IEqualityComparer&lt;T&gt;
    ///  for use with types which require one.
    /// </summary>
    /// <typeparam name="T">Type of values being compared</typeparam>
    public class EqualityComparerAdapter<T> : IEqualityComparer<T>
    {
        private IXArrayComparer<T> _inner;

        public EqualityComparerAdapter(IXArrayComparer inner)
        {
            _inner = (IXArrayComparer<T>)inner;
        }

        public bool Equals(T left, T right)
        {
            return _inner.WhereEqual(left, right);
        }

        public int GetHashCode(T value)
        {
            return _inner.GetHashCode(value);
        }
    }

    /// <summary>
    ///  When Swap is called, this indicates the type of swap.
    ///   Insert  - the target cell is empty.
    ///   Move    - the target cell has an unrelated value.
    ///   Match   - the target cell has the same key(s).
    /// </summary>
    public enum SwapType
    {
        Insert,
        Move,
        Match
    }

    /// <summary>
    ///  HashCore provides a base Robin Hood hash implementation for specific classes to build on.
    ///  It provides the algorithm for choosing a bucket, probing, swapping on insert, and resizing.
    ///  
    ///  Implementing classes can store any number of other arrays, so that HashSet, Dictionary, and
    ///  multiple-key, multiple-value classes can be implemented on top.
    /// </summary>
    public abstract class HashCore
    {
        /// <summary>
        ///  Return the number of items currently in the Dictionary
        /// </summary>
        public int Count { get; private set; }

        public int MaxProbeLength { get; private set; }

        // Metadata stores the probe length in the upper four bits and the probe increment in the lower four bits
        public byte[] Metadata { get; private set; }

        // Items can be a maximum of 14 buckets from the initial bucket they hash to, so the probe length fits in four bits with a sentinel zero
        private const int ProbeLengthLimit = 14;

        public HashCore()
        {
            // Descendant needs to call Reset to ensure arrays allocated,
            // but should do so after initializing the value arrays.
        }

        // Required methods - compare the value at an index to the current value to insert, swap the value at the index with the one to insert
        protected abstract bool EqualsCurrent(uint index);
        protected abstract void SwapWithCurrent(uint index, SwapType swapType);
        protected abstract void Expand();

        protected virtual void Reset(int size)
        {
            this.Metadata = new byte[size];
            this.Count = 0;
            this.MaxProbeLength = 0;
        }

        public virtual void Clear()
        {
            Array.Clear(this.Metadata, 0, this.Metadata.Length);
            this.Count = 0;
            this.MaxProbeLength = 0;
        }

        protected static int SizeForCapacity(int capacity)
        {
            // Minimum capacity is 28 items, which is a 32-element array
            if (capacity < 28) return 32;

            // Size to 1/8 over capacity so the table is just under 90% filled at the configured capacity
            return capacity + (capacity >> 3) + 1;
        }

        protected static int ResizeToSize(int currentSize)
        {
            // Double under 1M items and 1/8 when over
            return currentSize + (currentSize >= 1048576 ? currentSize >> 3 : currentSize);
        }

        // Find the average distance items are from their target buckets. Debuggability.
        public double DistanceMean()
        {
            ulong distance = 0;
            for (int i = 0; i < this.Metadata.Length; ++i)
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

        /// <summary>
        ///  Find the bucket containing an item with the given hash.
        ///  Uses EqualsCurrent to compare keys.
        /// </summary>
        /// <param name="hash">Hash of key to find</param>
        /// <returns>Index of bucket with item or -1 if not present</returns>
        protected int IndexOf(uint hash)
        {
            uint bucket = Bucket(hash);
            uint increment = Increment(hash);

            // To find a key, just compare every key starting with the expected bucket
            // up to the farthest any key had to be moved from the desired bucket.
            int limit = (byte)(((this.MaxProbeLength + 1) << 4) + 15);
            for (int matchingMetadata = (byte)((1 << 4) + (increment - 1)); matchingMetadata <= limit; matchingMetadata += 16)
            {
                if (this.Metadata[bucket] == matchingMetadata && EqualsCurrent(bucket)) return (int)bucket;

                bucket += increment;
                if (bucket >= this.Metadata.Length) bucket -= (uint)this.Metadata.Length;
            }

            return -1;
        }

        /// <summary>
        ///  Remove the item in a given bucket. Any additional arrays must also be cleared.
        /// </summary>
        /// <param name="bucket">Index of Bucket of item to clear</param>
        protected void Remove(int bucket)
        {
            // To remove a key, just clear the key and wealth.
            // Searches don't stop on empty buckets, so this is safe.
            this.Metadata[bucket] = 0;
            this.Count--;
        }

        /// <summary>
        ///  Find a bucket for an item with a given hash, swapping existing items to maintain the
        ///  Robin Hood properties. Add calls EqualsCurrent to compare keys and SwapWithCurrent to
        ///  insert the item.
        /// </summary>
        /// <param name="hash">Hash of item to find a bucket for</param>
        /// <returns>True if the item was inserted, False if the table must be expanded</returns>
        protected bool Add(uint hash)
        {
            // If the table is too close to full, expand it. Very full tables cause slower inserts as many items are shifted.
            if (this.Metadata.Length < SizeForCapacity(this.Count)) return false;

            // Find the bucket and probe increment for the new item
            uint bucket = Bucket(hash);
            uint increment = Increment(hash);

            int limit = (byte)((ProbeLengthLimit << 4) + 15);
            for (int matchingMetadata = (byte)((1 << 4) + (increment - 1)); matchingMetadata <= limit; matchingMetadata += 16)
            {
                int metadataFound = this.Metadata[bucket];

                if (metadataFound == 0)
                {
                    // If we found an empty cell (probe zero), add the item and return
                    this.Metadata[bucket] = (byte)matchingMetadata;
                    this.SwapWithCurrent(bucket, SwapType.Insert);

                    // Track the max probe length
                    if ((matchingMetadata >> 4) > this.MaxProbeLength) this.MaxProbeLength++;

                    // Increment the count for the new item
                    this.Count++;

                    return true;
                }
                else if (metadataFound < matchingMetadata)
                {
                    // If we found an item with a higher wealth, put the new item here
                    this.SwapWithCurrent(bucket, SwapType.Move);
                    this.Metadata[bucket] = (byte)matchingMetadata;

                    // Track the max probe length
                    if ((matchingMetadata >> 4) > this.MaxProbeLength) this.MaxProbeLength++;

                    // Get the swapped out item probe length and increment and loop to insert it
                    matchingMetadata = metadataFound;
                    increment = Increment((uint)metadataFound);
                }
                else if (metadataFound == matchingMetadata)
                {
                    // If this is a duplicate of the new item, reset the value and stop
                    if (this.EqualsCurrent(bucket))
                    {
                        this.SwapWithCurrent(bucket, SwapType.Match);
                        return true;
                    }
                }

                // Find the the next valid bucket for the current item to place
                bucket += increment;
                if (bucket >= this.Metadata.Length) bucket -= (uint)this.Metadata.Length;
            }

            // If we couldn't find a place for this item, a resize is required
            if (((double)this.Count / (double)this.Metadata.Length) < 0.9) throw new InvalidOperationException($"HashCore was unable to place an item when under 90% full. Is the hash function working correctly? {this.Count:n0} items, {this.Metadata.Length:n0} capacity.");
            return false;
        }
    }
}

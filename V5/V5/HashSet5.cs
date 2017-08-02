// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba;
using System;
using System.Collections;
using System.Collections.Generic;

namespace V5
{
    // TODO:
    //  - Can I take LowestWealth tracking out of insert loop?
    //  - Remove 'WealthVariance'

    public class HashSet5<T> : IEnumerable<T> where T : IEquatable<T>
    {
        public int Count { get; private set; }
        private T[] Values;
        private byte[] Wealth;
        private int LowestWealth;

        public HashSet5(int capacity = 16)
        {
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

        public double DistanceMean()
        {
            ulong distance = 0;
            for(int i = 0; i < this.Wealth.Length; ++i)
            {
                if (this.Wealth[i] > 0) distance += (ulong)(255 - this.Wealth[i]);
            }

            return ((double)distance / (double)this.Count);
        }

        public int[] WealthVariance()
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

        private uint Bucket(uint hash, int wealth)
        {
            // Goal: Choose a location nearby (w/in ~16 bytes?) with minimal chance to keep intersecting an initial collision

            // Linear Probing using extra hash bits
            //uint bucket = (uint)(((ulong)hash * (ulong)this.Wealth.Length) >> 32);
            //bucket += (uint)((hash & 0x3) * (255 - wealth));
            //if (bucket >= this.Wealth.Length) bucket -= (uint)this.Wealth.Length;
            //return bucket;

            // Use next hash bits
            //hash = (hash << (255 - wealth)) & uint.MaxValue;
            //return (uint)((hash * (ulong)this.Wealth.Length) >> 32);

            // Kinda quadratic probing
            //hash += (uint)1 << (64 - (255 - wealth));
            //return (uint)(((ulong)hash * (ulong)this.Wealth.Length) >> 32);

            // Sequential Probing - high variance but much faster
            uint bucket = (uint)(((ulong)hash * (ulong)this.Wealth.Length) >> 32);
            bucket += (uint)(255 - wealth);
            if (bucket >= this.Wealth.Length) bucket -= (uint)this.Wealth.Length;
            return bucket;

            // Murmur Probing; completely new bucket each time
            //ulong newHash = Hashing.MurmurHash3((ulong)hash, (uint)wealth) & uint.MaxValue;
            //return (uint)((newHash * (ulong)this.Wealth.Length) >> 32);
        }

        public bool Contains(T value)
        {
            return IndexOf(value) != -1;
        }

        private int IndexOf(T value)
        {
            uint hash = (uint)value.GetHashCode();
            for (int wealth = 255; wealth >= this.LowestWealth; --wealth)
            {
                uint bucket = Bucket(hash, wealth);
                if (this.Values[bucket].Equals(value)) return (int)bucket;
            }

            return -1;
        }

        public bool Remove(T value)
        {
            int index = IndexOf(value);
            if (index == -1) return false;

            this.Wealth[index] = 0;
            this.Values[index] = default(T);
            this.Count--;

            return true;
        }

        public bool Add(T value)
        {
            if (this.Count >= (this.Wealth.Length - (this.Wealth.Length >> 3)))
            {
                Expand();
            }

            uint hash = (uint)value.GetHashCode();

            for(byte wealth = 255; wealth > 0; --wealth)            
            {
                uint bucket = Bucket(hash, wealth);
                byte wealthFound = this.Wealth[bucket];

                if (wealthFound == 0)
                {
                    this.Wealth[bucket] = wealth;
                    this.Values[bucket] = value;
                    if (wealth < this.LowestWealth) this.LowestWealth = wealth;
                    this.Count++;

                    return true;
                }
                else if (wealthFound >= wealth)
                {
                    T valueMoved = this.Values[bucket];
                    if (valueMoved.Equals(value)) return false;

                    this.Wealth[bucket] = wealth;
                    this.Values[bucket] = value;
                    if (wealth < this.LowestWealth) this.LowestWealth = wealth;

                    value = valueMoved;
                    wealth = wealthFound;
                    hash = (uint)value.GetHashCode();
                }
            }

            // If we get here, we couldn't place the item - we must expand
            Expand();

            // Add the value, re-calculating where it goes
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

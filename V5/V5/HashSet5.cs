using System;
using System.Collections;
using System.Collections.Generic;

namespace V5
{
    public class HashSet5<T> : IEnumerable<T> where T : IEquatable<T>
    {
        public int Count { get; private set; }
        private T[] Values;
        private byte[] Wealth;
        private int LowestWealth;

        public HashSet5(int capacity = 16)
        {
            capacity = (capacity + (capacity >> 5) + 1);

            this.Count = 0;
            this.Values = new T[capacity];
            this.Wealth = new byte[capacity];

            this.LowestWealth = 255;
        }

        public void Clear()
        {
            Array.Clear(this.Wealth, 0, this.Wealth.Length);
            Array.Clear(this.Values, 0, this.Values.Length);

            this.Count = 0;
            this.LowestWealth = 255;
        }

        public int[] WealthVariance()
        {
            int[] result = new int[256];
            for (int i = 0; i < this.Wealth.Length; ++i)
            {
                if (this.Wealth[i] >= 0x10)
                {
                    result[255 - this.Wealth[i]]++;
                }
            }

            return result;
        }

        private uint Bucket(T value)
        {
            uint hash = (uint)(value.GetHashCode());
            return (uint)(((ulong)hash * (ulong)this.Wealth.Length) >> 32);
        }

        public bool Contains(T value)
        {
            uint bucket = Bucket(value);
            for (int wealth = 255; wealth >= this.LowestWealth; --wealth)
            {
                if (this.Values[bucket].Equals(value)) return true;
                if (++bucket == this.Wealth.Length) bucket = 0;
            }

            return false;
        }

        public bool Add(T value)
        {
            if (this.Count >= (this.Wealth.Length - (this.Wealth.Length >> 5)))
            {
                Expand();
            }

            uint bucket = Bucket(value);

            for(byte wealth = 255; wealth > 0; --wealth)            
            {
                byte wealthFound = this.Wealth[bucket];

                if (wealthFound == 0)
                {
                    this.Wealth[bucket] = wealth;
                    this.Values[bucket] = value;
                    this.Count++;

                    if (wealth < this.LowestWealth) this.LowestWealth = wealth;

                    return true;
                }
                else if (wealthFound >= wealth)
                {
                    T valueMoved = this.Values[bucket];
                    if (valueMoved.Equals(value)) return false;

                    this.Wealth[bucket] = wealth;
                    this.Values[bucket] = value;

                    if (wealth < this.LowestWealth)
                    {
                        this.LowestWealth = wealth;
                    }

                    value = valueMoved;
                    wealth = wealthFound;
                }

                if (++bucket == this.Wealth.Length) bucket = 0;
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

            // Round the new size up to an even 16 item length
            if ((newSize & 15) != 0) newSize = (newSize + 16) & ~15;

            // Save the current contents
            T[] oldValues = this.Values;
            byte[] oldWealth = this.Wealth;

            // Allocate the larger table
            this.Values = new T[newSize];
            this.Wealth = new byte[newSize];
            this.Count = 0;
            this.LowestWealth = 255;

            // Add items to the enlarged table
            for (int i = 0; i < oldWealth.Length; ++i)
            {
                if (oldWealth[i] > 0)
                {
                    Add(oldValues[i]);
                }
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

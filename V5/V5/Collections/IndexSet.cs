using System;
using V5.Query;

namespace V5.Collections
{
    public class IndexSet
    {
        private int offset;
        private int length;

        private ulong[] bitVector;

        public IndexSet(int offset, int length)
        {
            this.offset = offset;
            this.length = length;
            this.bitVector = new ulong[(length + 63) >> 6];
        }

        public int Count
        {
            get => ArraySearch.Count(this.bitVector);
        }

        public bool this[int index]
        {
            get => (this.bitVector[index >> 6] & (0x1U << (index & 63))) != 0;
            set
            {
                if (value)
                {
                    this.bitVector[index >> 6] |= (0x1U << (index & 63));
                }
                else
                {
                    this.bitVector[index >> 6] &= ~(0x1U << (index & 63));
                }
            }
        }

        public bool Equals(IndexSet other)
        {
            if (this.offset != other.offset) return false;
            if (this.length != other.length) return false;

            for (int i = 0; i < this.bitVector.Length; ++i)
            {
                if (this.bitVector[i] != other.bitVector[i]) return false;
            }

            return true;
        }

        public IndexSet None()
        {
            Array.Clear(this.bitVector, 0, this.bitVector.Length);
            return this;
        }

        public IndexSet All()
        {
            // Turn on all bits
            for (int i = 0; i < this.bitVector.Length; ++i)
            {
                this.bitVector[i] = ulong.MaxValue;
            }

            // Turn off bits over 'length'
            if ((length & 63) > 0)
            {
                this.bitVector[this.bitVector.Length - 1] &= (ulong.MaxValue >> (64 - (length & 63)));
            }

            return this;
        }

        public IndexSet And(Array values, Operator op, object value)
        {
            ArraySearch.AndWhereGreaterThan((byte[])values, (byte)value, this.bitVector);
            return this;
        }

        public IndexSet And(IndexSet other)
        {
            if (this.offset != other.offset) throw new InvalidOperationException();
            if (this.length != other.length) throw new InvalidOperationException();

            for (int i = 0; i < this.bitVector.Length; ++i)
            {
                this.bitVector[i] &= other.bitVector[i];
            }

            return this;
        }

        public IndexSet Or(IndexSet other)
        {
            if (this.offset != other.offset) throw new InvalidOperationException();
            if (this.length != other.length) throw new InvalidOperationException();

            for (int i = 0; i < this.bitVector.Length; ++i)
            {
                this.bitVector[i] |= other.bitVector[i];
            }

            return this;
        }

        public IndexSet AndNot(IndexSet other)
        {
            if (this.offset != other.offset) throw new InvalidOperationException();
            if (this.length != other.length) throw new InvalidOperationException();

            for (int i = 0; i < this.bitVector.Length; ++i)
            {
                this.bitVector[i] = this.bitVector[i] & ~other.bitVector[i];
            }

            return this;
        }
    }
}

using System;
using V5.Query;

namespace V5.Collections
{
    public class IndexSet
    {
        private int offset;
        private int length;

        private uint[] bitVector;

        public IndexSet(int offset, int length)
        {
            this.offset = offset;
            this.length = length;
            this.bitVector = new uint[(length + 31) >> 5];
        }

        public int Count
        {
            get => ArraySearch.Count(this.bitVector);
        }

        public bool this[int index]
        {
            get => (this.bitVector[index >> 5] & (0x1U << (index & 31))) != 0;
            set => this.bitVector[index >> 5] |= (0x1U << (index & 31));
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
                this.bitVector[i] = uint.MaxValue;
            }

            // Turn off bits over 'length'
            this.bitVector[this.bitVector.Length - 1] &= (uint.MaxValue << (length & 31));

            return this;
        }

        public IndexSet And(Array values, Operator op, object value)
        {
            ArraySearch.AndWhereGreaterThan((byte[])values, (byte)value, this.bitVector);
            return this;
        }
    }
}

using System;

namespace V5.Collections
{
    /// <summary>
    ///  IndexSet contains a bit vector stored in an array of ulongs.
    ///  It is used to track rows matching query expressions.    
    ///  
    ///  It provides C++ vector instruction accelerated Where, Page, and Count,
    ///  which are the key operations in V5 queries.
    /// </summary>
    /// <remarks>
    ///  Bit Manipulation Tricks:
    ///     (index >> 6) is (index / 64).
    ///     (index & 63) is (index % 64).
    ///     (0x1UL &lt;&lt; (index & 63)) is a ulong with only the (index % 64) bit set.   
    ///     (vector[index >> 6] & (0xUL &lt&lt; (index & 63))) extracts the index'th bit only.
    ///     
    ///     ((length + 63) >> 6) is the length over 64 rounded up.
    /// </remarks>
    public class IndexSet
    {
        private ulong[] _bitVector;

        public IndexSet(int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length");
            this._bitVector = new ulong[((length + 63) >> 6)];
        }

        public bool this[int index]
        {
            get { return (this._bitVector[index >> 6] & (0x1UL << (index & 63))) != 0; }
            set
            {
                if (value)
                {
                    this._bitVector[index >> 6] |= (0x1UL << (index & 63));
                }
                else
                {
                    this._bitVector[index >> 6] &= ~(0x1UL << (index & 63));
                }
            }
        }

        public int Capacity
        {
            get { return this._bitVector.Length << 6; }
        }

        public int Count
        {
            get { return V5.Native.Collections.IndexSetN.Count(this._bitVector); }
        }

        public void Page(ref Span<int> page, ref int fromIndex)
        {
            page.Length = V5.Native.Collections.IndexSetN.Page(this._bitVector, page.Array, ref fromIndex);
        }

        public IndexSet Where<T>(BooleanOperator bOp, T[] values, CompareOperator cOp, T value)
        {
            return Where(bOp, values, cOp, value, 0, values.Length);
        }

        public IndexSet Where<T>(BooleanOperator bOp, T[] values, CompareOperator cOp, T value, int offset, int length)
        {
            V5.Native.Collections.IndexSetN.Where(this._bitVector, bOp, values, cOp, value, offset, length);
            return this;
        }

        public IndexSet ClearAbove(int length)
        {
            if (length < 0 || length > this.Capacity) throw new ArgumentOutOfRangeException("length");

            // Clear bits over 'length' in the partially filled ulong, if any
            int lastIndex = length >> 6;

            if((length & 63) != 0)
            {
                this._bitVector[lastIndex] &= ulong.MaxValue >> (64 - (length & 63));
                lastIndex++;
            }

            // Clear all fully empty vector parts
            if (lastIndex < this._bitVector.Length)
            {
                Array.Clear(this._bitVector, lastIndex, this._bitVector.Length - lastIndex);
            }
            
            return this;
        }

        public IndexSet None()
        {
            return ClearAbove(0);
        }

        public IndexSet All(int length)
        {
            if (length < 0 || length > this.Capacity) throw new ArgumentOutOfRangeException("length");

            // Set every bit in every fully filled ulong (and the partially filled one, if any)
            int end = (length + 63) >> 6;
            for (int i = 0; i < end; ++i)
            {
                this._bitVector[i] = ulong.MaxValue;
            }

            // Clear the bits over the target length
            ClearAbove(length);

            return this;
        }

        public IndexSet Not(int length)
        {
            if (length < 0 || length > this.Capacity) throw new ArgumentOutOfRangeException("length");

            // Negate every block to the last ulong
            int end = (length + 63) >> 6;
            for (int i = 0; i < end; ++i)
            {
                this._bitVector[i] = ~this._bitVector[i];
            }

            // Clear bits over the length
            ClearAbove(length);

            return this;
        }

        public IndexSet Set(IndexSet other)
        {
            if (this._bitVector.Length != other._bitVector.Length) throw new InvalidOperationException();

            for (int i = 0; i < this._bitVector.Length; ++i)
            {
                this._bitVector[i] = other._bitVector[i];
            }

            return this;
        }

        public IndexSet And(IndexSet other)
        {
            if (this._bitVector.Length != other._bitVector.Length) throw new InvalidOperationException();

            for(int i = 0; i < this._bitVector.Length; ++i)
            {
                this._bitVector[i] &= other._bitVector[i];
            }

            return this;
        }

        public IndexSet Or(IndexSet other)
        {
            if (this._bitVector.Length != other._bitVector.Length) throw new InvalidOperationException();

            for (int i = 0; i < this._bitVector.Length; ++i)
            {
                this._bitVector[i] |= other._bitVector[i];
            }

            return this;
        }

        public IndexSet AndNot(IndexSet other)
        {
            if (this._bitVector.Length != other._bitVector.Length) throw new InvalidOperationException();

            for (int i = 0; i < this._bitVector.Length; ++i)
            {
                this._bitVector[i] &= ~other._bitVector[i];
            }

            return this;
        }

        public override bool Equals(object obj)
        {
            IndexSet other = obj as IndexSet;
            if (other == null) return false;

            if (this._bitVector.Length != other._bitVector.Length) return false;
            for (int i = 0; i < this._bitVector.Length; ++i)
            {
                if (this._bitVector[i] != other._bitVector[i]) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return this._bitVector.GetHashCode();
        }

        public override string ToString()
        {
            return $"{this.Count:n0}";
        }
    }
}

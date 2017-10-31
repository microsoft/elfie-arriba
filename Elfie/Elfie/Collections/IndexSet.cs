using ElfieNative.Collections;
using Microsoft.CodeAnalysis.Elfie.Query;
using System;

namespace Microsoft.CodeAnalysis.Elfie.Collections
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
        public static bool NativeAccelerated = false;

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

        #region Unary Operations
        public IndexSet ClearAbove(int length)
        {
            if (length < 0 || length > this.Capacity) throw new ArgumentOutOfRangeException("length");

            // Clear bits over 'length' in the partially filled ulong, if any
            int lastIndex = length >> 6;

            if ((length & 63) != 0)
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
        #endregion

        #region Set Operations
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

            for (int i = 0; i < this._bitVector.Length; ++i)
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
        #endregion

        #region Object overrides
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
        #endregion

        #region Native Accelerated Members
        public int Count
        {
            get
            {
                if (NativeAccelerated)
                {
                    return IndexSetN.Count(this._bitVector);
                }
                else
                {
                    return CountManaged();
                }
            }
        }

        private int CountManaged()
        {
            // Count using the hamming weight algorithm [http://en.wikipedia.org/wiki/Hamming_weight]
            const ulong m1 = 0x5555555555555555UL;
            const ulong m2 = 0x3333333333333333UL;
            const ulong m4 = 0x0f0f0f0f0f0f0f0fUL;
            const ulong h1 = 0x0101010101010101UL;

            ushort count = 0;

            int length = _bitVector.Length;
            for (int i = 0; i < length; ++i)
            {
                ulong x = _bitVector[i];

                x -= (x >> 1) & m1;
                x = (x & m2) + ((x >> 2) & m2);
                x = (x + (x >> 4)) & m4;

                count += (ushort)((x * h1) >> 56);
            }

            return count;
        }

        public void Page(ref Span<int> page, ref int fromIndex)
        {
            if (NativeAccelerated)
            {
                page.Length = IndexSetN.Page(this._bitVector, page.Array, ref fromIndex);
            }
            else
            {
                PageManaged(ref page, ref fromIndex);
            }
        }

        private void PageManaged(ref Span<int> page, ref int fromIndex)
        {
            int count = 0;

            // Starting at fromIndex, get the indices of all matches in the bit vector
            for (; fromIndex < this.Capacity; ++fromIndex)
            {
                if (this[fromIndex])
                {
                    page[count++] = fromIndex;
                    if (count == page.Capacity) break;
                }
            }

            // Mark the next index to start from, or -1 if we checked every bit
            if (fromIndex >= this.Capacity)
            {
                fromIndex = -1;
            }
            else
            {
                fromIndex++;
            }

            // Set the number of indices written
            page.Length = count;
        }

        public IndexSet Where<T>(BooleanOperator bOp, T[] values, CompareOperator cOp, T value) where T : IComparable<T>
        {
            return Where(bOp, values, cOp, value, 0, values.Length);
        }

        public IndexSet Where<T>(BooleanOperator bOp, T[] values, CompareOperator cOp, T value, int offset, int length) where T : IComparable<T>
        {
            if (NativeAccelerated)
            {
                IndexSetN.Where(this._bitVector, (ElfieNative.Query.BooleanOperator)bOp, values, (ElfieNative.Query.CompareOperator)cOp, value, offset, length);
            }
            else
            {
                WhereManaged(bOp, values, cOp, value, offset, length);
            }

            return this;
        }

        private void WhereManaged<T>(BooleanOperator bOp, T[] values, CompareOperator cOp, T value, int offset, int length) where T : IComparable<T>
        {
            // Get the IComparable for value (so we only box one value)
            IComparable<T> valueToCompare = (IComparable<T>)value;

            int end = offset + length;
            for(int index = offset; index < end; ++index)
            {
                // Compare (backwards)
                int compareInverse = value.CompareTo(values[index]);

                // Determine whether this row matches
                bool isMatch = IsMatch(compareInverse, cOp);

                // Set the bit vector bit accordingly
                switch(bOp)
                {
                    case BooleanOperator.Set:
                        this[index] = isMatch;
                        break;
                    case BooleanOperator.And:
                        this[index] &= isMatch;
                        break;
                    case BooleanOperator.Or:
                        this[index] |= isMatch;
                        break;
                    default:
                        throw new NotImplementedException(bOp.ToString());
                }
            }
        }

        private bool IsMatch(int compareInverse, CompareOperator cOp)
        {
            switch(cOp)
            {
                case CompareOperator.Equals:
                    return compareInverse == 0;
                case CompareOperator.NotEquals:
                    return compareInverse != 0;
                case CompareOperator.GreaterThan:
                    return compareInverse < 0;
                case CompareOperator.GreaterThanOrEqual:
                    return compareInverse <= 0;
                case CompareOperator.LessThan:
                    return compareInverse > 0;
                case CompareOperator.LessThanOrEqual:
                    return compareInverse >= 0;
                default:
                    throw new NotImplementedException(cOp.ToString());
            }
        }

        #endregion
    }
}

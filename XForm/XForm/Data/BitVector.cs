using System;
using System.Runtime.CompilerServices;

namespace XForm.Data
{
    /// <summary>
    ///  BitVectory contains a bit vector stored in an array of ulongs.
    ///  It is used to track rows matching query expressions.    
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
    public class BitVector
    {
        private ulong[] _bitVector;
        internal static Func<ulong[], int> s_nativeCount;
        internal static PageSignature s_nativePage;
        internal delegate int PageSignature(ulong[] vector, int[] indicesFound, ref int nextIndex);

        public BitVector(int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length");
            this._bitVector = new ulong[((length + 63) >> 6)];
        }

        internal ulong[] Array => _bitVector;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index)
        {
            this._bitVector[index >> 6] |= (0x1UL << (index & 63));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(int index)
        {
            this._bitVector[index >> 6] &= ~(0x1UL << (index & 63));
        }

        public int Capacity
        {
            get { return this._bitVector.Length << 6; }
        }

        public int Count
        {
            get
            {
                if (s_nativeCount != null) return s_nativeCount(this._bitVector);

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
        }

        public int Page(int[] indicesFound, ref int fromIndex)
        {
            if (s_nativePage != null) return s_nativePage(this._bitVector, indicesFound, ref fromIndex);

            int countFound = 0;
            int i;
            for (i = fromIndex; i < this.Capacity; ++i)
            {
                if (this[i])
                {
                    indicesFound[countFound] = i;
                    countFound++;

                    if (countFound == indicesFound.Length) break;
                }
            }

            fromIndex = (i == this.Capacity ? -1 : i + 1);
            return countFound;
        }

        public BitVector ClearAbove(int length)
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
                System.Array.Clear(this._bitVector, lastIndex, this._bitVector.Length - lastIndex);
            }
            
            return this;
        }

        public BitVector None()
        {
            return ClearAbove(0);
        }

        public BitVector All(int length)
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

        public BitVector Not(int length)
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

        public BitVector Set(BitVector other)
        {
            if (this._bitVector.Length != other._bitVector.Length) throw new InvalidOperationException();

            for (int i = 0; i < this._bitVector.Length; ++i)
            {
                this._bitVector[i] = other._bitVector[i];
            }

            return this;
        }

        public BitVector And(BitVector other)
        {
            if (this._bitVector.Length != other._bitVector.Length) throw new InvalidOperationException();

            for(int i = 0; i < this._bitVector.Length; ++i)
            {
                this._bitVector[i] &= other._bitVector[i];
            }

            return this;
        }

        public BitVector Or(BitVector other)
        {
            if (this._bitVector.Length != other._bitVector.Length) throw new InvalidOperationException();

            for (int i = 0; i < this._bitVector.Length; ++i)
            {
                this._bitVector[i] |= other._bitVector[i];
            }

            return this;
        }

        public BitVector AndNot(BitVector other)
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
            BitVector other = obj as BitVector;
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

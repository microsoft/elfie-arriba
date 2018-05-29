// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace XForm
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
        private int _length;
        internal static Func<ulong[], int> s_nativeCount;
        internal static PageSignature s_nativePage;
        internal delegate int PageSignature(ulong[] vector, int[] indicesFound, ref int nextIndex, int countLimit);

        public BitVector(int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length");
            _bitVector = new ulong[((length + 63) >> 6)];
            _length = length;
        }

        public BitVector(ulong[] vector)
        {
            _bitVector = vector;
        }

        internal ulong[] Array => _bitVector;

        public int Capacity
        {
            get { return _length; }
            set
            {
                _length = value;
                int vectorLength = ((_length + 63) >> 6);
                if (_bitVector.Length < vectorLength) _bitVector = new ulong[vectorLength];
            }
        }

        public bool this[int index]
        {
            get { return (_bitVector[index >> 6] & (0x1UL << (index & 63))) != 0; }
            set
            {
                if (value)
                {
                    _bitVector[index >> 6] |= (0x1UL << (index & 63));
                }
                else
                {
                    _bitVector[index >> 6] &= ~(0x1UL << (index & 63));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index)
        {
            _bitVector[index >> 6] |= (0x1UL << (index & 63));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(int index)
        {
            _bitVector[index >> 6] &= ~(0x1UL << (index & 63));
        }

        public int Count
        {
            get
            {
                if (s_nativeCount != null) return s_nativeCount(_bitVector);

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

        public int Page(int[] indicesFound, ref int fromIndex, int countLimit = -1)
        {
            if (countLimit > indicesFound.Length) throw new ArgumentOutOfRangeException("countLimit");
            if (countLimit == -1) countLimit = indicesFound.Length;
            if (s_nativePage != null) return s_nativePage(_bitVector, indicesFound, ref fromIndex, countLimit);

            int countFound = 0;
            int i;
            for (i = fromIndex; i < this.Capacity; ++i)
            {
                if (this[i])
                {
                    indicesFound[countFound] = i;
                    countFound++;

                    if (countFound == countLimit) break;
                }
            }

            fromIndex = (i == this.Capacity ? -1 : i + 1);
            return countFound;
        }

        public void ToArray(ref bool[] array, int startIndexInclusive = 0, int endIndexExclusive = -1)
        {
            if (endIndexExclusive == -1) endIndexExclusive = this.Capacity;
            Allocator.AllocateToSize(ref array, (endIndexExclusive - startIndexInclusive));

            for (int i = startIndexInclusive; i < endIndexExclusive; ++i)
            {
                array[i - startIndexInclusive] = this[i];
            }
        }

        public BitVector ClearAbove(int length)
        {
            if (length < 0 || length > this.Capacity) throw new ArgumentOutOfRangeException("length");

            // Clear bits over 'length' in the partially filled ulong, if any
            int lastIndex = length >> 6;

            if ((length & 63) != 0)
            {
                _bitVector[lastIndex] &= ulong.MaxValue >> (64 - (length & 63));
                lastIndex++;
            }

            // Clear all fully empty vector parts
            if (lastIndex < _bitVector.Length)
            {
                System.Array.Clear(_bitVector, lastIndex, _bitVector.Length - lastIndex);
            }

            return this;
        }

        /// <summary>
        ///  Return the single matching index from this BitVector, or -1 if there
        ///  wasn't exactly one match.
        /// </summary>
        /// <returns>Index of the only match or -1 if not exactly one match.</returns>
        public int GetSingle()
        {
            int[] result = new int[2];
            int fromIndex = 0;
            int countFound = this.Page(result, ref fromIndex);

            return (countFound == 1 ? result[0] : -1);
        }

        public BitVector None()
        {
            System.Array.Clear(_bitVector, 0, _bitVector.Length);
            return this;
        }

        public BitVector All(int length = -1)
        {
            if (length != -1) length = this.Capacity;
            if (length < 0 || length > this.Capacity) throw new ArgumentOutOfRangeException("length");

            // Set every bit in every fully filled ulong (and the partially filled one, if any)
            int end = (length + 63) >> 6;
            for (int i = 0; i < end; ++i)
            {
                _bitVector[i] = ulong.MaxValue;
            }

            // Clear the bits over the target length
            ClearAbove(length);

            return this;
        }

        public BitVector Not()
        {
            // Negate every block to the last ulong
            int end = (this.Capacity + 63) >> 6;
            for (int i = 0; i < end; ++i)
            {
                _bitVector[i] = ~_bitVector[i];
            }

            // Clear bits over the length
            ClearAbove(this.Capacity);

            return this;
        }

        public BitVector Set(BitVector other)
        {
            if (this.Capacity != other.Capacity) throw new InvalidOperationException();

            for (int i = 0; i < _bitVector.Length; ++i)
            {
                _bitVector[i] = other._bitVector[i];
            }

            return this;
        }

        public BitVector And(BitVector other)
        {
            if (this.Capacity != other.Capacity) throw new InvalidOperationException();

            for (int i = 0; i < _bitVector.Length; ++i)
            {
                _bitVector[i] &= other._bitVector[i];
            }

            return this;
        }

        public BitVector Or(BitVector other)
        {
            if (this.Capacity != other.Capacity) throw new InvalidOperationException();

            for (int i = 0; i < _bitVector.Length; ++i)
            {
                _bitVector[i] |= other._bitVector[i];
            }

            return this;
        }

        public BitVector AndNot(BitVector other)
        {
            if (this.Capacity != other.Capacity) throw new InvalidOperationException();

            for (int i = 0; i < _bitVector.Length; ++i)
            {
                _bitVector[i] &= ~other._bitVector[i];
            }

            return this;
        }

        public BitVector Set(bool[] other)
        {
            if (this.Capacity < other.Length) throw new InvalidOperationException();
            this.None();

            for (int i = 0; i < other.Length; ++i)
            {
                this[i] = other[i];
            }

            return this;
        }

        public BitVector And(bool[] other)
        {
            if (this.Capacity < other.Length) throw new InvalidOperationException();

            for (int i = 0; i < other.Length; ++i)
            {
                this[i] &= other[i];
            }

            return this;
        }

        public BitVector Or(bool[] other)
        {
            if (this.Capacity < other.Length) throw new InvalidOperationException();

            for (int i = 0; i < other.Length; ++i)
            {
                this[i] |= other[i];
            }

            return this;
        }

        public override bool Equals(object obj)
        {
            BitVector other = obj as BitVector;
            if (other == null) return false;

            if (_bitVector.Length != other._bitVector.Length) return false;
            for (int i = 0; i < _bitVector.Length; ++i)
            {
                if (_bitVector[i] != other._bitVector[i]) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return _bitVector.GetHashCode();
        }

        public override string ToString()
        {
            return $"{this.Count:n0}";
        }
    }
}

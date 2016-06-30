// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

using Arriba.Extensions;
using Arriba.Serialization;

namespace Arriba.Structures
{
    /// <summary>
    ///  ShortSet is a set of ushorts up to a given capacity. ShortSets are
    ///  used to identify the items in an Arriba partition matching a query
    ///  clause or in a given set. Set operations are in the inner loop of
    ///  searches, so performance of this class is critical.
    /// </summary>
    public class ShortSet : IBinarySerializable
    {
        internal const ulong FirstBit = 0x1UL << 63;
        public static bool UseNativeSupport;
        private static byte[] s_setBitsTable;

        private ushort _capacity;
        private ulong[] _bitVector;
        private ulong _clearAboveCapacityMask;
        private ShortSet _scratchSet;

        static ShortSet()
        {
            BuildSetBitsLookupTable();
            UseNativeSupport = false;
        }

        public ShortSet(ushort capacity)
        {
            Initialize(capacity);
        }

        private void Initialize(ushort capacity)
        {
            _capacity = capacity;

            // Allocate enough bits to tell whether 0-(capacity-1) are in the set
            int vectorSize = (capacity / 64);
            int overage = (capacity % 64);
            if (overage != 0) vectorSize++;
            _bitVector = new ulong[vectorSize];

            // Create a mask to clear bits in the last ulong which are above the capacity
            _clearAboveCapacityMask = ~0UL;
            if (overage != 0) _clearAboveCapacityMask = ~(_clearAboveCapacityMask >> overage);
        }

        #region Add/Remove, Contains, Enumerate, Count
        /// <summary>
        ///  Return whether the given item is in the set.
        /// </summary>
        /// <param name="index">Item to check</param>
        /// <returns>True if item in set, False otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(ushort index)
        {
            // Check the corresponding bit in the vector
            return (_bitVector[index >> 6] & (FirstBit >> (index & 63))) != 0UL;
        }

        /// <summary>
        ///  Add the given item to the set.
        /// </summary>
        /// <param name="index">Item to add</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ushort index)
        {
            // Set the corresponding bit in the vector
            ulong bitSection = (FirstBit >> (index & 63));
            _bitVector[index >> 6] |= bitSection;
        }

        /// <summary>
        ///  Remove the given item from the set.
        /// </summary>
        /// <param name="index">Item to remove</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(ushort index)
        {
            // Clear the corresponding bit in the vector
            ulong bitSection = (FirstBit >> (index & 63));
            _bitVector[index >> 6] &= ~bitSection;
        }

        /// <summary>
        ///  Return whether the set is empty as quickly as possible.
        /// </summary>
        /// <returns>True if no values are set, False otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEmpty()
        {
            int length = _bitVector.Length;
            for (int i = 0; i < length; ++i)
            {
                if (_bitVector[i] != 0) return false;
            }

            return true;
        }

        /// <summary>
        ///  Return the set of values, in ascending order, which are in this set.
        /// </summary>
        public unsafe ushort[] Values
        {
            get
            {
                int count = this.Count();
                int countFound = 0;
                ushort[] items = new ushort[count];
                if (count == 0) return items;

                int length = _bitVector.Length;

                fixed (ulong* pbitVector = &_bitVector[0])
                {
                    fixed (byte* pBitsTable = &s_setBitsTable[0])
                    {
                        for (int i = 0; i < length; ++i)
                        {
                            ulong bitSection = pbitVector[i];
                            ushort x = (ushort)(i * 64);

                            while (bitSection != 0UL)
                            {
                                // Get the first byte of bitSection and convert to a table lookup
                                int bitSegment = (byte)(bitSection >> 56) * 9;

                                // Add the value of each bit set in the table
                                byte value;
                                while ((value = pBitsTable[bitSegment]) != 0xFF)
                                {
                                    items[countFound] = (ushort)(x + value);
                                    ++countFound;
                                    ++bitSegment;
                                }

                                // Look at the next byte i n the section
                                x += 8;
                                bitSection = bitSection << 8;
                            }
                        }
                    }
                }

                return items;
            }
        }

        /// <summary>
        ///  Return the count of items included in the set.
        /// </summary>
        public unsafe ushort Count()
        {
            if (_bitVector.Length == 0) return 0;

            if (UseNativeSupport)
            {
                // Count directly using the POPCNT instruction (one ulong per instruction)
                fixed (ulong* array = &_bitVector[0])
                {
                    return (ushort)NativeMethods.PopulationCount(array, _bitVector.Length);
                }
            }
            else
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
        }

        /// <summary>
        ///  Capacity accessor; returns the number of items possible within the set (0-limit).
        /// </summary>
        public ushort Capacity
        {
            get { return _capacity; }
        }
        #endregion

        #region Unary Set Operations
        /// <summary>
        ///  Negate all items in the set.
        /// </summary>
        public void Not()
        {
            int length = _bitVector.Length;
            for (int i = 0; i < length; ++i)
            {
                _bitVector[i] = ~_bitVector[i];
            }

            TrimToCapacity();
        }

        /// <summary>
        ///  Clear the set (remove all items)
        /// </summary>
        public void Clear()
        {
            int length = _bitVector.Length;
            Array.Clear(_bitVector, 0, _bitVector.Length);
        }
        #endregion

        #region ShortSet Set Operations [This Op Other]
        /// <summary>
        ///  And this set with another set (this set becomes the result), up
        ///  to our capacity.
        /// </summary>
        /// <param name="other">ShortSet with which to And</param>
        public void And(ShortSet other)
        {
            if (other == null) throw new ArgumentNullException("other");

            // And parts in both. Values above other capacity will be zero, clearing them in our set.
            int length = Math.Min(_bitVector.Length, other._bitVector.Length);
            for (int i = 0; i < length; ++i)
            {
                _bitVector[i] &= other._bitVector[i];
            }

            // Clear our values above other capacity, if any
            ClearAboveLength(length);
        }

        /// <summary>
        ///  Or this set with another set (this set becomes the result), up to
        ///  our capacity.
        /// </summary>
        /// <param name="other">ShortSet with which to Or</param>
        public void Or(ShortSet other)
        {
            if (other == null) throw new ArgumentNullException("other");

            // Or parts in both. This may set values above our capacity in the last ulong.
            int length = Math.Min(_bitVector.Length, other._bitVector.Length);
            for (int i = 0; i < length; ++i)
            {
                _bitVector[i] |= other._bitVector[i];
            }

            // Clear back to our capacity.
            TrimToCapacity();
        }

        /// <summary>
        ///  OrNot this set with another set (this set becomes the result), up
        ///  to our capacity.
        /// </summary>
        /// <param name="other">ShortSet with which to AndNot</param>
        public void OrNot(ShortSet other)
        {
            if (other == null) throw new ArgumentNullException("other");

            // OrNot away values in other.
            int length = Math.Min(_bitVector.Length, other._bitVector.Length);
            for (int i = 0; i < length; ++i)
            {
                _bitVector[i] = _bitVector[i] | ~other._bitVector[i];
            }

            // Clear back to our capacity.
            TrimToCapacity();
        }

        /// <summary>
        ///  AndNot this set with another set (this set becomes the result), up
        ///  to our capacity.
        /// </summary>
        /// <param name="other">ShortSet with which to AndNot</param>
        public void AndNot(ShortSet other)
        {
            if (other == null) throw new ArgumentNullException("other");

            // AndNot away values in other. This will not set values above our capacity,
            // since they're already 0 on our side. This will not clear values above their
            // capacity, because they are already 0 on their side.
            int length = Math.Min(_bitVector.Length, other._bitVector.Length);
            for (int i = 0; i < length; ++i)
            {
                _bitVector[i] = _bitVector[i] & ~other._bitVector[i];
            }
        }
        #endregion

        #region ShortSet Set Operations [This = Left Op Right]
        /// <summary>
        ///  Copy the values in other to this set, overwriting current set values.
        /// </summary>
        /// <param name="other">ShortSet with which to And</param>
        public void From(ShortSet other)
        {
            if (other == null) throw new ArgumentNullException("other");

            // Copy from other
            int length = Math.Min(_bitVector.Length, other._bitVector.Length);
            for (int i = 0; i < length; ++i)
            {
                _bitVector[i] = other._bitVector[i];
            }

            // Clear our values above other capacity, if any
            ClearAboveLength(length);
        }

        /// <summary>
        ///  Set this set equal to (left AND right), overwriting current values.
        /// </summary>
        /// <param name="left">First ShortSet to And</param>
        /// <param name="right">Second ShortSet to And</param>
        public unsafe void FromAnd(ShortSet left, ShortSet right)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");

            int length = Math.Min(_bitVector.Length, Math.Min(left._bitVector.Length, right._bitVector.Length));

            if (UseNativeSupport)
            {
                fixed (ulong* thisA = &_bitVector[0])
                {
                    fixed (ulong* leftA = &left._bitVector[0])
                    {
                        fixed (ulong* rightA = &right._bitVector[0])
                        {
                            NativeMethods.AndSets(thisA, leftA, rightA, length);
                        }
                    }
                }
            }
            else
            {
                // Copy from (left & right)
                for (int i = 0; i < length; ++i)
                {
                    _bitVector[i] = left._bitVector[i] & right._bitVector[i];
                }
            }

            // Clear our values above other capacity, if any
            ClearAboveLength(length);
        }
        #endregion

        #region IEnumerable Set Operations
        /// <summary>
        ///  And this set with another set (this set becomes the result).
        /// </summary>
        /// <remarks>
        ///  'And' cannot be easily computed for a set and enumerable because
        ///  it's difficult to tell quickly if values on both side are set,
        ///  especially since 'other' can be in any order.
        ///  
        ///  However, thanks to De Morgan's Law [!(A && B) == (!A || !B)],
        ///  we can compute our (A && B) with !(!A || !B), avoiding creating a
        ///  second ShortSet.
        /// </remarks>
        /// <param name="other">Items with which to And</param>
        public void And(IEnumerable<ushort> other)
        {
            if (other == null) throw new ArgumentNullException("other");

            // Build a scratch set (once)
            if (_scratchSet == null)
            {
                _scratchSet = new ShortSet(_capacity);
            }
            else
            {
                _scratchSet.Clear();
            }

            // Set values in 'other'
            foreach (ushort value in other)
            {
                if (value < _capacity) _scratchSet.Add(value);
            }

            // And with this
            _scratchSet.And(this);

            // Swap bitVectors with the scratchSet
            ulong[] thisVector = _bitVector;
            _bitVector = _scratchSet._bitVector;
            _scratchSet._bitVector = _bitVector;
        }

        /// <summary>
        ///  Or this set with another set (this set becomes the result).
        /// </summary>
        /// <param name="other">Items with which to Or</param>
        public void Or(IEnumerable<ushort> other)
        {
            if (other == null) throw new ArgumentNullException("other");

            foreach (ushort value in other)
            {
                if (value < _capacity) this.Add(value);
            }
        }

        /// <summary>
        ///  AndNot this set with another set (this set becomes the result).
        /// </summary>
        /// <param name="other">Items with which to AndNot</param>
        public void AndNot(IEnumerable<ushort> other)
        {
            if (other == null) throw new ArgumentNullException("other");

            foreach (ushort value in other)
            {
                if (value < _capacity) this.Remove(value);
            }
        }
        #endregion

        #region Unsafe Set Operations
        /// <summary>
        ///  Or this set with a group of values in an array.
        /// </summary>
        /// <param name="values">Sparse array from which to add (Or)</param>
        /// <param name="length">Number of values in array</param>
        unsafe public void Or(ushort* values, ushort length)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            for (int i = 0; i < length; ++i)
            {
                ushort value = values[i];
                if (value < _capacity) this.Add(values[i]);
            }
        }

        /// <summary>
        ///  Or this set with a group of bits in an array.
        /// </summary>
        /// <param name="values">Dense array from which to add (Or)</param>
        /// <param name="length">Number of values in array</param>
        unsafe public void Or(ulong* values, ushort length)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            int commonLength = Math.Min(_bitVector.Length, length);
            for (int i = 0; i < commonLength; ++i)
            {
                _bitVector[i] |= values[i];
            }

            TrimToCapacity();
        }
        #endregion

        #region IBinarySerializable
        public void ReadBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            ushort capacity = context.Reader.ReadUInt16();
            Initialize(capacity);

            _bitVector = BinaryBlockSerializer.ReadArray<ulong>(context);
        }

        public void WriteBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            context.Writer.Write(_capacity);
            BinaryBlockSerializer.WriteArray(context, _bitVector);
        }
        #endregion

        public override string ToString()
        {
            return StringExtensions.Format("[{0}]", String.Join(", ", this.Values));
        }

        #region Private Members
        private void ClearAboveLength(int length)
        {
            if (length < _bitVector.Length)
            {
                for (int i = length; i < _bitVector.Length; ++i)
                {
                    _bitVector[i] = 0UL;
                }
            }
        }

        private void TrimToCapacity()
        {
            if (this.Capacity > 0)
            {
                _bitVector[_bitVector.Length - 1] &= _clearAboveCapacityMask;
            }
        }

        // Table Lookup without unsafe code.
        //public ICollection<ushort> Values
        //{
        //    get
        //    {
        //        int countFound = 0;
        //        ushort[] items = new ushort[this.Count];
        //        int length = this.bitVector.Length;

        //        for (int i = 0; i < length; ++i)
        //        {
        //            ulong bitSection = this.bitVector[i];
        //            ushort x = (ushort)(i * 64);

        //            while (bitSection != 0UL)
        //            {
        //                // Get the first byte of bitSection and convert to a table lookup
        //                int bitSegment = (byte)(bitSection >> 56) * 9;

        //                // Add the value of each bit set in the table
        //                byte value;
        //                while ((value = SetBitsTable[bitSegment]) != 0xFF)
        //                {
        //                    items[countFound] = (ushort)(x + value);
        //                    ++countFound;
        //                    ++bitSegment;
        //                }

        //                // Look at the next byte i n the section
        //                x += 8;
        //                bitSection = bitSection << 8;
        //            }
        //        }

        //        return items;
        //    }
        //}

        // Work per set bit, but slower. Too much repeat work re-calling LeadingZeros?
        //public ICollection<ushort> Values
        //{
        //    get
        //    {
        //        List<ushort> items = new List<ushort>();

        //        int length = this.bitVector.Length;
        //        for (int i = 0; i < length; ++i)
        //        {
        //            ulong bitSection = this.bitVector[i];
        //            ushort x = (ushort)(i * 64);

        //            while (bitSection != 0UL)
        //            {
        //                // Find the first set bit index (equal to the number of leading zeros)
        //                int firstIndexSet = LeadingZeros(bitSection);
        //                items.Add((ushort)(x + firstIndexSet));

        //                // Clear the first set bit and continue
        //                bitSection &= ~(FirstBit >> firstIndexSet);
        //            }
        //        }

        //        return items;
        //    }
        //}

        public static int LeadingZeros(ulong value)
        {
            if (value == 0UL) return 64;

            int count = 0;
            if (value >> 32 == 0) { count += 32; value = value << 32; }
            if (value >> 48 == 0) { count += 16; value = value << 16; }
            if (value >> 56 == 0) { count += 8; value = value << 8; }
            if (value >> 60 == 0) { count += 4; value = value << 4; }
            if (value >> 62 == 0) { count += 2; value = value << 2; }
            if (value >> 63 == 0) { count += 1; }

            return count;
        }

        private static void BuildSetBitsLookupTable()
        {
            // Build a table for every possible byte listing the bits set within the byte (ex: 0x84 -> 0 and 6 bits set)
            s_setBitsTable = new byte[256 * 9];

            for (int i = 0; i < 256; ++i)
            {
                int baseIndexForI = i * 9;
                int countSetForI = 0;

                // At SetBitsTable[i * 9], list each bit set for a byte with value i
                for (int j = 0; j < 8; ++j)
                {
                    if ((i & (0x80 >> j)) != 0)
                    {
                        s_setBitsTable[baseIndexForI + countSetForI] = (byte)j;
                        ++countSetForI;
                    }
                }

                // Write one 0xFF next to indicate no more values
                s_setBitsTable[baseIndexForI + countSetForI] = 0xFF;
            }
        }
        #endregion

        #region Arriba.Native Imports
        private class NativeMethods
        {
            [DllImport("Arriba.Native.dll", PreserveSig = true, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            public unsafe static extern int CallOverheadTest();

            [DllImport("Arriba.Native.dll", PreserveSig = true, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            public unsafe static extern int PopulationCount(ulong* values, int length);

            [DllImport("Arriba.Native.dll", PreserveSig = true, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            public unsafe static extern void AndSets(ulong* result, ulong* left, ulong* right, int length);
        }
        #endregion
    }
}

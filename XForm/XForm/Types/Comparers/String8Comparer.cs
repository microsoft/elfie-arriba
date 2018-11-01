// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
using XForm.IO;
using XForm.Query;

namespace XForm.Types.Comparers
{
    /// <summary>
    ///  IXArrayComparer for String8[].
    ///  NOT GENERATED; CompareBlock is customized.
    /// </summary>
    internal class String8Comparer : IXArrayComparer, IXArrayTextComparer, IXArrayComparer<String8>
    {
        internal static ComparerExtensions.WhereSingle<String8> s_WhereSingleNative = null;
        internal static ComparerExtensions.Where<String8> s_WhereNative = null;

        public delegate int IndexOfAll(byte[] text, int textIndex, int textLength, byte[] value, int valueIndex, int valueLength, bool ignoreCase, int[] resultArray);
        internal static IndexOfAll s_IndexOfAllNative = null;

        internal int[] _indicesBuffer;

        public void GetHashCodes(XArray xarray, int[] hashes)
        {
            if (hashes.Length < xarray.Count) throw new ArgumentOutOfRangeException("hashes.Length");
            String8[] array = (String8[])xarray.Array;

            for (int i = 0; i < xarray.Count; ++i)
            {
                int index = xarray.Index(i);
                if (xarray.HasNulls || xarray.NullRows[index] == false)
                {
                    hashes[i] = (hashes[i] << 5) - hashes[i] + unchecked((int)Hashing.Hash(array[xarray.Index(i)], 0));
                }
            }
        }

        public int GetHashCode(String8 value)
        {
            return unchecked((int)Hashing.Hash(value, 0));
        }

        public void WhereEqual(XArray left, XArray right, BitVector vector)
        {
            String8[] leftArray = (String8[])left.Array;
            String8[] rightArray = (String8[])right.Array;

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (WhereEqual(leftArray[left.Index(i)], rightArray[right.Index(i)])) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if (s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.Equal, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    int zeroOffset = left.Selector.StartIndexInclusive;
                    int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (WhereEqual(leftArray[i], rightArray[i + leftIndexToRightIndex])) vector.Set(i - zeroOffset);
                    }
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                String8 rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.Equal, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    String8 first = leftArray[left.Selector.StartIndexInclusive];
                    String8 last = leftArray[left.Selector.EndIndexExclusive - 1];
                    if (first.Array == last.Array && first.Index < last.Index)
                    {
                        WhereBlock(left, rightValue, false, CompareOperator.Equal, vector);
                    }
                    else
                    {
                        for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                        {
                            if (WhereEqual(leftArray[i], rightValue)) vector.Set(i - zeroOffset);
                        }
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (WhereEqual(leftArray[left.Selector.StartIndexInclusive], rightArray[right.Selector.StartIndexInclusive]))
                {
                    vector.All(left.Count);
                }
            }

            // Remove nulls from matches
            BoolComparer.AndNotNull(left, vector);
            BoolComparer.AndNotNull(right, vector);
        }

        public unsafe bool WhereEqual(String8 left, String8 right)
        {
            //return left.Equals(right);

            if (left.Length != right.Length) return false;
            if (left.Length == 0) return true;
            if (left.Array == right.Array && left.Index == right.Index) return true;

            if (left.Array.Length < left.Index + left.Length) throw new IndexOutOfRangeException("left.Index + Length");
            if (right.Array.Length < right.Index + right.Length) throw new IndexOutOfRangeException("right.Index + Length");

            fixed (byte* leftArray = &left.Array[left.Index])
            {
                fixed (byte* rightArray = &right.Array[right.Index])
                {
                    int index = 0;
                    int length = left.Length;
                    int blockLength = length - 8;

                    while (index < blockLength)
                    {
                        ulong* leftLong = (ulong*)&leftArray[index];
                        ulong* rightLong = (ulong*)&rightArray[index];
                        if (*leftLong != *rightLong) return false;
                        index += 8;
                    }

                    for (; index < length; ++index)
                    {
                        if (leftArray[index] != rightArray[index]) return false;
                    }
                }
            }

            return true;
        }

        public void WhereNotEqual(XArray left, XArray right, BitVector vector)
        {
            String8[] leftArray = (String8[])left.Array;
            String8[] rightArray = (String8[])right.Array;

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (!WhereEqual(leftArray[left.Index(i)], rightArray[right.Index(i)])) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if (s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.NotEqual, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    int zeroOffset = left.Selector.StartIndexInclusive;
                    int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (!WhereEqual(leftArray[i], rightArray[i + leftIndexToRightIndex])) vector.Set(i - zeroOffset);
                    }
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                String8 rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.NotEqual, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    String8 first = leftArray[left.Selector.StartIndexInclusive];
                    String8 last = leftArray[left.Selector.EndIndexExclusive - 1];
                    if (first.Array == last.Array && first.Index < last.Index)
                    {
                        WhereBlock(left, rightValue, false, CompareOperator.NotEqual, vector);
                    }
                    else
                    {
                        for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                        {
                            if (!WhereEqual(leftArray[i], rightValue)) vector.Set(i - zeroOffset);
                        }
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (!WhereEqual(leftArray[left.Selector.StartIndexInclusive], rightArray[right.Selector.StartIndexInclusive]))
                {
                    vector.All(left.Count);
                }
            }

            // Remove nulls from matches
            BoolComparer.AndNotNull(left, vector);
            BoolComparer.AndNotNull(right, vector);
        }

        public bool WhereNotEqual(String8 left, String8 right)
        {
            return !WhereEqual(left, right);
        }

        public void WhereLessThan(XArray left, XArray right, BitVector vector)
        {
            String8[] leftArray = (String8[])left.Array;
            String8[] rightArray = (String8[])right.Array;

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) < 0) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if (s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.LessThan, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    int zeroOffset = left.Selector.StartIndexInclusive;
                    int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i].CompareTo(rightArray[i + leftIndexToRightIndex]) < 0) vector.Set(i - zeroOffset);
                    }
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                String8 rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.LessThan, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i].CompareTo(rightValue) < 0) vector.Set(i - zeroOffset);
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].CompareTo(rightArray[right.Selector.StartIndexInclusive]) < 0)
                {
                    vector.All(left.Count);
                }
            }

            // Remove nulls from matches
            BoolComparer.AndNotNull(left, vector);
            BoolComparer.AndNotNull(right, vector);
        }

        public bool WhereLessThan(String8 left, String8 right)
        {
            return left.CompareTo(right) < 0;
        }

        public void WhereLessThanOrEqual(XArray left, XArray right, BitVector vector)
        {
            String8[] leftArray = (String8[])left.Array;
            String8[] rightArray = (String8[])right.Array;

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) <= 0) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if (s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.LessThanOrEqual, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    int zeroOffset = left.Selector.StartIndexInclusive;
                    int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i].CompareTo(rightArray[i + leftIndexToRightIndex]) <= 0) vector.Set(i - zeroOffset);
                    }
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                String8 rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.LessThanOrEqual, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i].CompareTo(rightValue) <= 0) vector.Set(i - zeroOffset);
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].CompareTo(rightArray[right.Selector.StartIndexInclusive]) <= 0)
                {
                    vector.All(left.Count);
                }
            }

            // Remove nulls from matches
            BoolComparer.AndNotNull(left, vector);
            BoolComparer.AndNotNull(right, vector);
        }

        public bool WhereLessThanOrEqual(String8 left, String8 right)
        {
            return left.CompareTo(right) <= 0;
        }

        public void WhereGreaterThan(XArray left, XArray right, BitVector vector)
        {
            String8[] leftArray = (String8[])left.Array;
            String8[] rightArray = (String8[])right.Array;

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) > 0) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if (s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.GreaterThan, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    int zeroOffset = left.Selector.StartIndexInclusive;
                    int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i].CompareTo(rightArray[i + leftIndexToRightIndex]) > 0) vector.Set(i - zeroOffset);
                    }
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                String8 rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.GreaterThan, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i].CompareTo(rightValue) > 0) vector.Set(i - zeroOffset);
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].CompareTo(rightArray[right.Selector.StartIndexInclusive]) > 0)
                {
                    vector.All(left.Count);
                }
            }

            // Remove nulls from matches
            BoolComparer.AndNotNull(left, vector);
            BoolComparer.AndNotNull(right, vector);
        }

        public bool WhereGreaterThan(String8 left, String8 right)
        {
            return left.CompareTo(right) > 0;
        }

        public void WhereGreaterThanOrEqual(XArray left, XArray right, BitVector vector)
        {
            String8[] leftArray = (String8[])left.Array;
            String8[] rightArray = (String8[])right.Array;

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) >= 0) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if (s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.GreaterThanOrEqual, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    int zeroOffset = left.Selector.StartIndexInclusive;
                    int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i].CompareTo(rightArray[i + leftIndexToRightIndex]) >= 0) vector.Set(i - zeroOffset);
                    }
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                String8 rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.GreaterThanOrEqual, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i].CompareTo(rightValue) >= 0) vector.Set(i - zeroOffset);
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].CompareTo(rightArray[right.Selector.StartIndexInclusive]) >= 0)
                {
                    vector.All(left.Count);
                }
            }

            // Remove nulls from matches
            BoolComparer.AndNotNull(left, vector);
            BoolComparer.AndNotNull(right, vector);
        }

        public bool WhereGreaterThanOrEqual(String8 left, String8 right)
        {
            return left.CompareTo(right) >= 0;
        }

        public void WhereContains(XArray left, XArray right, BitVector vector)
        {
            String8[] leftArray = (String8[])left.Array;
            String8[] rightArray = (String8[])right.Array;

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].IndexOf(rightArray[right.Index(i)]) != -1) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if (s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.Contains, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    int zeroOffset = left.Selector.StartIndexInclusive;
                    int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i].IndexOf(rightArray[i + leftIndexToRightIndex]) != -1) vector.Set(i - zeroOffset);
                    }
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                String8 rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.Contains, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    String8 first = leftArray[left.Selector.StartIndexInclusive];
                    String8 last = leftArray[left.Selector.EndIndexExclusive - 1];
                    if (first.Array == last.Array && first.Index < last.Index)
                    {
                        WhereBlock(left, rightValue, true, CompareOperator.Contains, vector);
                    }
                    else
                    {
                        for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                        {
                            if (leftArray[i].IndexOf(rightValue) != -1) vector.Set(i - zeroOffset);
                        }
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].Contains(rightArray[right.Selector.StartIndexInclusive]) != -1)
                {
                    vector.All(left.Count);
                }
            }

            // Remove nulls from matches
            BoolComparer.AndNotNull(left, vector);
            BoolComparer.AndNotNull(right, vector);
        }

        /// <summary>
        ///  WhereContains overload to compare String8 rows as a single block, if available.
        ///  This is only available when comparing to a constant and before any other row filtering operations.
        /// </summary>
        /// <param name="left">Raw String8 byte[] and int[] for current rows</param>
        /// <param name="rightValue">Constant Value to compare to</param>
        /// <param name="vector">BitVector to record matches to</param>
        public void WhereContains(String8Raw left, String8 rightValue, BitVector vector)
        {
            if (rightValue.Length == 0) return;

            String8 all = new String8((byte[])left.Bytes.Array, 0, left.Bytes.Selector.EndIndexExclusive);
            Allocator.AllocateToSize(ref _indicesBuffer, 1024);

            int startRowIndex = left.Positions.Selector.StartIndexInclusive;
            int nextRowIndex = startRowIndex;
            int endRowIndex = left.Positions.Selector.EndIndexExclusive;

            int nextByteIndex = left.Bytes.Selector.StartIndexInclusive;

            int rightLength = rightValue.Length;
            int[] positions = (int[])left.Positions.Array;

            bool includesFirstString = (left.Selector.StartIndexInclusive == 0);
            int firstStringStart = (includesFirstString ? 0 : positions[left.Positions.Index(0)]);
            int positionOffset = left.Positions.Index((includesFirstString ? 0 : 1));
            int textOffset = firstStringStart - left.Bytes.Index(0);

            while (true)
            {
                // Find a batch of matches
                int countFound;
                if (s_IndexOfAllNative != null)
                {
                    countFound = s_IndexOfAllNative(all.Array, nextByteIndex, all.Length - nextByteIndex, rightValue.Array, rightValue.Index, rightValue.Length, true, _indicesBuffer);
                }
                else
                {
                    countFound = all.IndexOfAll(rightValue, nextByteIndex, true, _indicesBuffer);
                }

                // Map the indices found to rows
                int countMatched = 0;

                // If matching the very first row, look for the full match within it
                if (countFound > 0 && nextRowIndex == startRowIndex)
                {
                    if (_indicesBuffer[0] < (positions[nextRowIndex] - textOffset))
                    {
                        if (_indicesBuffer[0] + rightLength <= (positions[nextRowIndex] - textOffset))
                        {
                            vector.Set(0);
                            countMatched++;
                        }
                    }

                    nextRowIndex++;
                }

                for (; countMatched < countFound; ++countMatched)
                {
                    int indexToFind = _indicesBuffer[countMatched] + rightLength + textOffset;

                    for (; nextRowIndex < endRowIndex; ++nextRowIndex)
                    {
                        // If the next match is before this row...
                        if (indexToFind <= positions[nextRowIndex])
                        {
                            // If it's fully within the previous row, add it
                            if (indexToFind - rightLength >= positions[nextRowIndex - 1])
                            {
                                vector.Set(nextRowIndex - startRowIndex - 1);
                            }

                            // Look for the next match (in this row again)
                            break;
                        }
                    }

                    // If we're out of rows, stop
                    if (nextRowIndex == endRowIndex) break;
                }

                // If there were matches left, they must be in the last row
                if (countMatched != countFound)
                {
                    vector.Set(nextRowIndex - startRowIndex - 1);
                }

                // Find the next index to search for matches from
                if (countFound < _indicesBuffer.Length) break;
                nextByteIndex = _indicesBuffer[countFound - 1] + 1;
            }
        }

        public bool WhereContains(String8 left, String8 right)
        {
            return left.IndexOf(right) != -1;
        }

        public void WhereContainsExact(XArray left, XArray right, BitVector vector)
        {
            String8[] leftArray = (String8[])left.Array;
            String8[] rightArray = (String8[])right.Array;

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].Contains(rightArray[right.Index(i)]) != -1) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if (s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.ContainsExact, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    int zeroOffset = left.Selector.StartIndexInclusive;
                    int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i].Contains(rightArray[i + leftIndexToRightIndex]) != -1) vector.Set(i - zeroOffset);
                    }
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                String8 rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.ContainsExact, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    String8 first = leftArray[left.Selector.StartIndexInclusive];
                    String8 last = leftArray[left.Selector.EndIndexExclusive - 1];
                    if (first.Array == last.Array && first.Index < last.Index)
                    {
                        WhereBlock(left, rightValue, false, CompareOperator.ContainsExact, vector);
                    }
                    else
                    {
                        for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                        {
                            if (leftArray[i].Contains(rightValue) != -1) vector.Set(i - zeroOffset);
                        }
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].Contains(rightArray[right.Selector.StartIndexInclusive]) != -1)
                {
                    vector.All(left.Count);
                }
            }

            // Remove nulls from matches
            BoolComparer.AndNotNull(left, vector);
            BoolComparer.AndNotNull(right, vector);
        }

        public bool WhereContainsExact(String8 left, String8 right)
        {
            return left.Contains(right) != -1;
        }

        public void WhereStartsWith(XArray left, XArray right, BitVector vector)
        {
            String8[] leftArray = (String8[])left.Array;
            String8[] rightArray = (String8[])right.Array;

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].StartsWith(rightArray[right.Index(i)], true)) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if (s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.StartsWith, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    int zeroOffset = left.Selector.StartIndexInclusive;
                    int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i].StartsWith(rightArray[i + leftIndexToRightIndex], true)) vector.Set(i - zeroOffset);
                    }
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                String8 rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.StartsWith, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    String8 first = leftArray[left.Selector.StartIndexInclusive];
                    String8 last = leftArray[left.Selector.EndIndexExclusive - 1];
                    if (first.Array == last.Array && first.Index < last.Index)
                    {
                        WhereBlock(left, rightValue, true, CompareOperator.StartsWith, vector);
                    }
                    else
                    {
                        for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                        {
                            if (leftArray[i].StartsWith(rightValue, true)) vector.Set(i - zeroOffset);
                        }
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].StartsWith(rightArray[right.Selector.StartIndexInclusive], true))
                {
                    vector.All(left.Count);
                }
            }

            // Remove nulls from matches
            BoolComparer.AndNotNull(left, vector);
            BoolComparer.AndNotNull(right, vector);
        }

        public bool WhereStartsWith(String8 left, String8 right)
        {
            return left.StartsWith(right, true);
        }

        public void WhereEndsWith(XArray left, XArray right, BitVector vector)
        {
            String8[] leftArray = (String8[])left.Array;
            String8[] rightArray = (String8[])right.Array;

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].EndsWith(rightArray[right.Index(i)], true)) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if (s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.EndsWith, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    int zeroOffset = left.Selector.StartIndexInclusive;
                    int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i].EndsWith(rightArray[i + leftIndexToRightIndex], true)) vector.Set(i - zeroOffset);
                    }
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                String8 rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.EndsWith, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    String8 first = leftArray[left.Selector.StartIndexInclusive];
                    String8 last = leftArray[left.Selector.EndIndexExclusive - 1];
                    if (first.Array == last.Array && first.Index < last.Index)
                    {
                        WhereBlock(left, rightValue, true, CompareOperator.EndsWith, vector);
                    }
                    else
                    {
                        for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                        {
                            if (leftArray[i].EndsWith(rightValue, true)) vector.Set(i - zeroOffset);
                        }
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].EndsWith(rightArray[right.Selector.StartIndexInclusive], true))
                {
                    vector.All(left.Count);
                }
            }

            // Remove nulls from matches
            BoolComparer.AndNotNull(left, vector);
            BoolComparer.AndNotNull(right, vector);
        }

        public bool WhereEndsWith(String8 left, String8 right)
        {
            return left.EndsWith(right, true);
        }

        public void WhereBlock(XArray left, String8 rightValue, bool ignoreCase, CompareOperator cOp, BitVector vector)
        {
            if (rightValue.IsEmpty())
            {
                WhereBlockEmpty(left, cOp, vector);
                return;
            }

            String8[] leftArray = (String8[])left.Array;
            String8 last = leftArray[left.Selector.EndIndexExclusive - 1];
            String8 all = new String8(last.Array, 0, last.Index + last.Length);
            Allocator.AllocateToSize(ref _indicesBuffer, 1024);

            int nextIndex = leftArray[left.Selector.StartIndexInclusive].Index;
            int nextRowIndex = left.Selector.StartIndexInclusive + 1;

            while (true)
            {
                // Find a batch of matches
                int countFound;
                if (s_IndexOfAllNative != null)
                {
                    countFound = s_IndexOfAllNative(all.Array, nextIndex, all.Length - nextIndex, rightValue.Array, rightValue.Index, rightValue.Length, ignoreCase, _indicesBuffer);
                }
                else
                {
                    countFound = all.IndexOfAll(rightValue, nextIndex, true, _indicesBuffer);
                }

                // Map the indices found to rows
                if (cOp == CompareOperator.Contains || cOp == CompareOperator.ContainsExact)
                {
                    IndicesToContainsRows(leftArray, rightValue.Length, ref nextRowIndex, left.Selector.StartIndexInclusive, left.Selector.EndIndexExclusive, countFound, vector);
                }
                else if (cOp == CompareOperator.Equal || cOp == CompareOperator.NotEqual)
                {
                    IndicesToEqualsRows(leftArray, rightValue.Length, ref nextRowIndex, left.Selector.StartIndexInclusive, left.Selector.EndIndexExclusive, countFound, vector);
                }
                else if(cOp == CompareOperator.StartsWith)
                {
                    IndicesToStartsWithRows(leftArray, rightValue.Length, ref nextRowIndex, left.Selector.StartIndexInclusive, left.Selector.EndIndexExclusive, countFound, vector);
                }
                else if (cOp == CompareOperator.EndsWith)
                {
                    IndicesToEndsWithRows(leftArray, rightValue.Length, ref nextRowIndex, left.Selector.StartIndexInclusive, left.Selector.EndIndexExclusive, countFound, vector);
                }
                else
                {
                    throw new NotImplementedException(cOp.ToString());
                }

                // Find the next index to search for matches from
                if (countFound < _indicesBuffer.Length) break;
                nextIndex = _indicesBuffer[countFound - 1] + 1;
            }

            if (cOp == CompareOperator.NotEqual) vector.Not();
        }

        public void WhereBlockEmpty(XArray left, CompareOperator cOp, BitVector vector)
        {
            // Contains and StartsWith are allowed but have no matches
            if (cOp == CompareOperator.Contains || cOp == CompareOperator.ContainsExact || cOp == CompareOperator.StartsWith || cOp == CompareOperator.EndsWith) return;

            // Operators other than equality are otherwise disallowed
            if (cOp != CompareOperator.Equal && cOp != CompareOperator.NotEqual) throw new NotImplementedException(cOp.ToString());

            bool include = (cOp == CompareOperator.Equal);

            String8[] array = (String8[])left.Array;
            for(int i = 0; i < left.Count; ++i)
            {
                String8 value = array[left.Index(i)];
                if (include == value.IsEmpty()) vector.Set(i);
            }
        }

        private void IndicesToContainsRows(String8[] leftArray, int rightLength, ref int nextRowIndex, int startRowIndex, int endRowIndex, int countFound, BitVector vector)
        {
            // Find the row containing each match
            int countMatched = 0;
            for (; countMatched < countFound; ++countMatched)
            {
                int indexToFind = _indicesBuffer[countMatched] + rightLength;

                for (; nextRowIndex < endRowIndex; ++nextRowIndex)
                {
                    // If the next match is before this row...
                    if (indexToFind <= leftArray[nextRowIndex].Index)
                    {
                        // If it's fully within the previous row, add it
                        if (_indicesBuffer[countMatched] >= leftArray[nextRowIndex - 1].Index)
                        {
                            vector.Set(nextRowIndex - 1 - startRowIndex);
                        }

                        // Look for the next match (in this row again)
                        break;
                    }
                }

                // If we're out of rows, stop
                if (nextRowIndex == endRowIndex) break;
            }

            // If there were matches left, they must be in the last row
            if (countMatched != countFound)
            {
                vector.Set(nextRowIndex - 1 - startRowIndex);
            }
        }

        private void IndicesToEqualsRows(String8[] leftArray, int rightLength, ref int nextRowIndex, int startRowIndex, int endRowIndex, int countFound, BitVector vector)
        {
            // Find the row containing each match
            int countMatched = 0;
            for (; countMatched < countFound; ++countMatched)
            {
                int indexToFind = _indicesBuffer[countMatched] + rightLength;

                for (; nextRowIndex < endRowIndex; ++nextRowIndex)
                {
                    // If the next match is before this row...
                    if (indexToFind <= leftArray[nextRowIndex].Index)
                    {
                        // If it starts exactly on the previous row and ends exactly on this row, it's a match
                        if (_indicesBuffer[countMatched] == leftArray[nextRowIndex - 1].Index && indexToFind == leftArray[nextRowIndex].Index)
                        {
                            vector.Set(nextRowIndex - 1 - startRowIndex);
                        }

                        // Look for the next match (in this row again)
                        break;
                    }
                }

                // If we're out of rows, stop
                if (nextRowIndex == endRowIndex) break;
            }

            // If there were matches left, they must be in the last row
            if (countMatched != countFound)
            {
                if (_indicesBuffer[countMatched] == leftArray[endRowIndex - 1].Index && rightLength == leftArray[endRowIndex - 1].Length)
                {
                    vector.Set(endRowIndex - 1 - startRowIndex);
                }
            }
        }

        private void IndicesToStartsWithRows(String8[] leftArray, int rightLength, ref int nextRowIndex, int startRowIndex, int endRowIndex, int countFound, BitVector vector)
        {
            // Find the row containing each match
            int countMatched = 0;
            for (; countMatched < countFound; ++countMatched)
            {
                int indexToFind = _indicesBuffer[countMatched] + rightLength;

                for (; nextRowIndex < endRowIndex; ++nextRowIndex)
                {
                    // If the next match is before this row...
                    if (indexToFind <= leftArray[nextRowIndex].Index)
                    {
                        // If it starts exactly on the previous row, it's a match
                        if (_indicesBuffer[countMatched] == leftArray[nextRowIndex - 1].Index)
                        {
                            vector.Set(nextRowIndex - 1 - startRowIndex);
                        }

                        // Look for the next match (in this row again)
                        break;
                    }
                }

                // If we're out of rows, stop
                if (nextRowIndex == endRowIndex) break;
            }

            // If there were matches left, they must be in the last row
            if (countMatched != countFound)
            {
                if (_indicesBuffer[countMatched] == leftArray[endRowIndex - 1].Index)
                {
                    vector.Set(endRowIndex - 1 - startRowIndex);
                }
            }
        }

        private void IndicesToEndsWithRows(String8[] leftArray, int rightLength, ref int nextRowIndex, int startRowIndex, int endRowIndex, int countFound, BitVector vector)
        {
            // Find the row containing each match
            int countMatched = 0;
            for (; countMatched < countFound; ++countMatched)
            {
                int indexToFind = _indicesBuffer[countMatched] + rightLength;

                for (; nextRowIndex < endRowIndex; ++nextRowIndex)
                {
                    // If the next match is before this row...
                    if (indexToFind <= leftArray[nextRowIndex].Index)
                    {
                        // If it ends exactly on the previous row, it's a match
                        if (indexToFind == leftArray[nextRowIndex].Index)
                        {
                            vector.Set(nextRowIndex - 1 - startRowIndex);
                        }

                        // Look for the next match (in this row again)
                        break;
                    }
                }

                // If we're out of rows, stop
                if (nextRowIndex == endRowIndex) break;
            }

            // If there were matches left, they must be in the last row
            if (countMatched != countFound)
            {
                if (_indicesBuffer[countMatched] + rightLength == leftArray[endRowIndex - 1].Index + leftArray[endRowIndex - 1].Length)
                {
                    vector.Set(endRowIndex - 1 - startRowIndex);
                }
            }
        }
    }
}

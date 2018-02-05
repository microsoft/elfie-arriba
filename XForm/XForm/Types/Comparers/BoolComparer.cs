// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Query;

namespace XForm.Types.Comparers
{
    /// <summary>
    ///  IXArrayComparer for bool[]. NOT GENERATED.
    /// </summary>
    internal class BoolComparer : IXArrayComparer, IXArrayComparer<bool>
    {
        internal static ComparerExtensions.WhereSingle<bool> s_WhereSingleNative = null;
        internal static ComparerExtensions.Where<bool> s_WhereNative = null;

        public void GetHashCodes(XArray xarray, int[] hashes)
        {
            if (hashes.Length < xarray.Count) throw new ArgumentOutOfRangeException("hashes.Length");
            bool[] array = (bool[])xarray.Array;

            for (int i = 0; i < xarray.Count; ++i)
            {
                int index = xarray.Index(i);
                if (!xarray.HasNulls || xarray.NullRows[index] == false)
                {
                    hashes[i] = (hashes[i] << 5) - hashes[i] + GetHashCode(array[xarray.Index(i)]);
                }
            }
        }

        public int GetHashCode(bool value)
        {
            // Return every bit opposite and each distinct from zero (hash of null)
            return unchecked((int)(value ? 0xAAAAAAAA : 0x55555555));
        }

        public static void AndNotNull(XArray left, BitVector vector)
        {
            bool[] leftArray = (bool[])left.NullRows;
            if (leftArray == null) return;

            // Check how the arrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)]) vector.Clear(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.Equal, false, (byte)BooleanOperator.And, vector.Array, 0);
                }
                else
                {
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i]) vector.Clear(i - zeroOffset);
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (!leftArray[left.Selector.StartIndexInclusive])
                {
                    vector.None();
                }
            }
        }

        public static void WhereNull(XArray left, bool isValue, BitVector vector)
        {
            bool[] leftArray = (bool[])left.NullRows;
            if (leftArray == null)
            {
                if (isValue == false) vector.All(left.Count);
                return;
            }

            // Check how the arrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] == isValue) vector.Set(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.Equal, isValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i] == isValue) vector.Set(i - zeroOffset);
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] == isValue)
                {
                    vector.All(left.Count);
                }
                else
                {
                    vector.None();
                }
            }
        }

        public void WhereEqual(XArray left, XArray right, BitVector vector)
        {
            bool[] leftArray = (bool[])left.Array;
            bool[] rightArray = (bool[])right.Array;

            // Check how the arrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] == rightArray[right.Index(i)]) vector.Set(i);
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
                        if (leftArray[i] == rightArray[i + leftIndexToRightIndex]) vector.Set(i - zeroOffset);
                    }
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                bool rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.Equal, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i] == rightValue) vector.Set(i - zeroOffset);
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] == rightArray[right.Selector.StartIndexInclusive])
                {
                    vector.All(left.Count);
                }
            }

            // Remove nulls from matches
            BoolComparer.AndNotNull(left, vector);
            BoolComparer.AndNotNull(right, vector);
        }

        public bool WhereEqual(bool left, bool right)
        {
            return left == right;
        }

        public void WhereGreaterThan(XArray left, XArray right, BitVector vector)
        {
            throw new ArgumentException("Operator is not valid for boolean");
        }

        public void WhereGreaterThanOrEqual(XArray left, XArray right, BitVector vector)
        {
            throw new ArgumentException("Operator is not valid for boolean");
        }

        public void WhereLessThan(XArray left, XArray right, BitVector vector)
        {
            throw new ArgumentException("Operator is not valid for boolean");
        }

        public void WhereLessThanOrEqual(XArray left, XArray right, BitVector vector)
        {
            throw new ArgumentException("Operator is not valid for boolean");
        }

        public void WhereNotEqual(XArray left, XArray right, BitVector vector)
        {
            bool[] leftArray = (bool[])left.Array;
            bool[] rightArray = (bool[])right.Array;

            // Check how the arrays are configured and run the fastest loop possible for the configuration.
            if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] != rightArray[right.Index(i)]) vector.Set(i);
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
                        if (leftArray[i] != rightArray[i + leftIndexToRightIndex]) vector.Set(i - zeroOffset);
                    }
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                bool rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.NotEqual, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                }
                else
                {
                    for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                    {
                        if (leftArray[i] != rightValue) vector.Set(i - zeroOffset);
                    }
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] != rightArray[right.Selector.StartIndexInclusive])
                {
                    vector.All(left.Count);
                }
            }

            // Remove nulls from matches
            BoolComparer.AndNotNull(left, vector);
            BoolComparer.AndNotNull(right, vector);
        }

        public bool WhereNotEqual(bool left, bool right)
        {
            return left != right;
        }

        public bool WhereLessThan(bool left, bool right)
        {
            throw new ArgumentException("Operator is not valid for boolean");
        }

        public bool WhereLessThanOrEqual(bool left, bool right)
        {
            throw new ArgumentException("Operator is not valid for boolean");
        }

        public bool WhereGreaterThan(bool left, bool right)
        {
            throw new ArgumentException("Operator is not valid for boolean");
        }

        public bool WhereGreaterThanOrEqual(bool left, bool right)
        {
            throw new ArgumentException("Operator is not valid for boolean");
        }
    }
}

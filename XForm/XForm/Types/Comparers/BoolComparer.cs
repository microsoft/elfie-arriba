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

        public void WhereEqual(XArray left, XArray right, BitVector vector)
        {
            bool[] leftArray = (bool[])left.Array;
            bool[] rightArray = (bool[])right.Array;

            // Check how the arrays are configured and run the fastest loop possible for the configuration.
            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] == rightArray[rightIndex]) vector.Set(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
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
                    return;
                }

                int zeroOffset = left.Selector.StartIndexInclusive;
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] == rightArray[i + leftIndexToRightIndex]) vector.Set(i - zeroOffset);
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
                    return;
                }

                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] == rightValue) vector.Set(i - zeroOffset);
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
        }

        public bool WhereEqual(bool left, bool right)
        {
            return left == right;
        }

        public void GetHashCodes(XArray values, int[] hashes)
        {
            throw new ArgumentException("Operator is not valid for boolean");
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
            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] != rightArray[rightIndex]) vector.Set(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
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
                    return;
                }

                int zeroOffset = left.Selector.StartIndexInclusive;
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] != rightArray[i + leftIndexToRightIndex]) vector.Set(i - zeroOffset);
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
                    return;
                }

                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] != rightValue) vector.Set(i - zeroOffset);
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

        public int GetHashCode(bool value)
        {
            throw new ArgumentException("Operator is not valid for boolean");
        }
    }
}

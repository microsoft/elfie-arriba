
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// GENERATED by XForm.Generator\ComparerGenerator.cs

using System;

using XForm.Data;
using XForm.Query;

namespace XForm.Types.Comparers
{
    /// <summary>
    ///  IDataBatchComparer for short[].
    /// </summary>
    internal class ShortComparer : IDataBatchComparer, IDataBatchComparer<short>
    {
        internal static ComparerExtensions.WhereSingle<short> s_WhereSingleNative = null;
        internal static ComparerExtensions.Where<short> s_WhereNative = null;

        public void GetHashCodes(DataBatch batch, int[] hashes)
        {
            if (hashes.Length < batch.Count) throw new ArgumentOutOfRangeException("hashes.Length");
            short[] array = (short[])batch.Array;

            for (int i = 0; i < batch.Count; ++i)
            {
                int index = batch.Index(i);
                if (batch.IsNull == null || batch.IsNull[index] == false)
                {
                    hashes[i] = (hashes[i] << 5) - hashes[i] + unchecked((int) Hashing.Hash(array[batch.Index(i)], 0));
                }
            }
        }

        public int GetHashCode(short value)
        {
            return unchecked((int)Hashing.Hash(value, 0));
        }

		public void WhereEqual(DataBatch left, DataBatch right, BitVector vector)
        {
            short[] leftArray = (short[])left.Array;
            short[] rightArray = (short[])right.Array;

            // Check how the DataBatches are configured and run the fastest loop possible for the configuration.
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
                if(s_WhereNative != null)
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
                short rightValue = rightArray[0];

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

        public bool WhereEqual(short left, short right)
        {
            return left == right;
        }

		public void WhereNotEqual(DataBatch left, DataBatch right, BitVector vector)
        {
            short[] leftArray = (short[])left.Array;
            short[] rightArray = (short[])right.Array;

            // Check how the DataBatches are configured and run the fastest loop possible for the configuration.
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
                if(s_WhereNative != null)
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
                short rightValue = rightArray[0];

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

        public bool WhereNotEqual(short left, short right)
        {
            return left != right;
        }

		public void WhereLessThan(DataBatch left, DataBatch right, BitVector vector)
        {
            short[] leftArray = (short[])left.Array;
            short[] rightArray = (short[])right.Array;

            // Check how the DataBatches are configured and run the fastest loop possible for the configuration.
            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] < rightArray[rightIndex]) vector.Set(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] < rightArray[right.Index(i)]) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if(s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.LessThan, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                    return;
                }

                int zeroOffset = left.Selector.StartIndexInclusive;
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] < rightArray[i + leftIndexToRightIndex]) vector.Set(i - zeroOffset);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                short rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.LessThan, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                    return;
                }

                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] < rightValue) vector.Set(i - zeroOffset);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] < rightArray[right.Selector.StartIndexInclusive])
                {
                    vector.All(left.Count);
                }
            }
        }

        public bool WhereLessThan(short left, short right)
        {
            return left < right;
        }

		public void WhereLessThanOrEqual(DataBatch left, DataBatch right, BitVector vector)
        {
            short[] leftArray = (short[])left.Array;
            short[] rightArray = (short[])right.Array;

            // Check how the DataBatches are configured and run the fastest loop possible for the configuration.
            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] <= rightArray[rightIndex]) vector.Set(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] <= rightArray[right.Index(i)]) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if(s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.LessThanOrEqual, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                    return;
                }

                int zeroOffset = left.Selector.StartIndexInclusive;
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] <= rightArray[i + leftIndexToRightIndex]) vector.Set(i - zeroOffset);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                short rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.LessThanOrEqual, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                    return;
                }

                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] <= rightValue) vector.Set(i - zeroOffset);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] <= rightArray[right.Selector.StartIndexInclusive])
                {
                    vector.All(left.Count);
                }
            }
        }

        public bool WhereLessThanOrEqual(short left, short right)
        {
            return left <= right;
        }

		public void WhereGreaterThan(DataBatch left, DataBatch right, BitVector vector)
        {
            short[] leftArray = (short[])left.Array;
            short[] rightArray = (short[])right.Array;

            // Check how the DataBatches are configured and run the fastest loop possible for the configuration.
            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] > rightArray[rightIndex]) vector.Set(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] > rightArray[right.Index(i)]) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if(s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.GreaterThan, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                    return;
                }

                int zeroOffset = left.Selector.StartIndexInclusive;
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] > rightArray[i + leftIndexToRightIndex]) vector.Set(i - zeroOffset);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                short rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.GreaterThan, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                    return;
                }

                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] > rightValue) vector.Set(i - zeroOffset);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] > rightArray[right.Selector.StartIndexInclusive])
                {
                    vector.All(left.Count);
                }
            }
        }

        public bool WhereGreaterThan(short left, short right)
        {
            return left > right;
        }

		public void WhereGreaterThanOrEqual(DataBatch left, DataBatch right, BitVector vector)
        {
            short[] leftArray = (short[])left.Array;
            short[] rightArray = (short[])right.Array;

            // Check how the DataBatches are configured and run the fastest loop possible for the configuration.
            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] >= rightArray[rightIndex]) vector.Set(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] >= rightArray[right.Index(i)]) vector.Set(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                if(s_WhereNative != null)
                {
                    s_WhereNative(leftArray, left.Selector.StartIndexInclusive, (byte)CompareOperator.GreaterThanOrEqual, rightArray, right.Selector.StartIndexInclusive, left.Selector.Count, (byte)BooleanOperator.Or, vector.Array, 0);
                    return;
                }

                int zeroOffset = left.Selector.StartIndexInclusive;
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] >= rightArray[i + leftIndexToRightIndex]) vector.Set(i - zeroOffset);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int zeroOffset = left.Selector.StartIndexInclusive;
                short rightValue = rightArray[0];

                if (s_WhereSingleNative != null)
                {
                    s_WhereSingleNative(leftArray, left.Selector.StartIndexInclusive, left.Selector.Count, (byte)CompareOperator.GreaterThanOrEqual, rightValue, (byte)BooleanOperator.Or, vector.Array, 0);
                    return;
                }

                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] >= rightValue) vector.Set(i - zeroOffset);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] >= rightArray[right.Selector.StartIndexInclusive])
                {
                    vector.All(left.Count);
                }
            }
        }

        public bool WhereGreaterThanOrEqual(short left, short right)
        {
            return left >= right;
        }

    }
}

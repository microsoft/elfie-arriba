// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Transforms;

namespace XForm.Types
{
    /// <summary>
    ///  ComparableComparer exposes methods for each compare operator implemented in terms of IComparable&lt;T&gt;
    ///  It is slower than a hard-coded comparer for a primitive type because every CompareTo() is a function call.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ComparableComparer<T> : IDataBatchComparer where T : IComparable<T>
    {
        public void WhereEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;

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
                    if (leftArray[leftIndex].CompareTo(rightArray[rightIndex]) == 0) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) == 0) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i].CompareTo(rightArray[i + leftIndexToRightIndex]) == 0) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                T rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i].CompareTo(rightValue) == 0) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].CompareTo(rightArray[right.Selector.StartIndexInclusive]) == 0)
                {
                    result.All(left.Count);
                }
            }
        }

        public void WhereNotEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;

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
                    if (leftArray[leftIndex].CompareTo(rightArray[rightIndex]) != 0) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) != 0) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i].CompareTo(rightArray[i + leftIndexToRightIndex]) != 0) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                T rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i].CompareTo(rightValue) != 0) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].CompareTo(rightArray[right.Selector.StartIndexInclusive]) != 0)
                {
                    result.All(left.Count);
                }
            }
        }

        public void WhereLessThan(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;

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
                    if (leftArray[leftIndex].CompareTo(rightArray[rightIndex]) < 0) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) < 0) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i].CompareTo(rightArray[i + leftIndexToRightIndex]) < 0) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                T rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i].CompareTo(rightValue) < 0) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].CompareTo(rightArray[right.Selector.StartIndexInclusive]) < 0)
                {
                    result.All(left.Count);
                }
            }
        }

        public void WhereLessThanOrEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;

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
                    if (leftArray[leftIndex].CompareTo(rightArray[rightIndex]) <= 0) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) <= 0) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i].CompareTo(rightArray[i + leftIndexToRightIndex]) <= 0) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                T rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i].CompareTo(rightValue) <= 0) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].CompareTo(rightArray[right.Selector.StartIndexInclusive]) <= 0)
                {
                    result.All(left.Count);
                }
            }
        }

        public void WhereGreaterThan(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;

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
                    if (leftArray[leftIndex].CompareTo(rightArray[rightIndex]) > 0) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) > 0) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i].CompareTo(rightArray[i + leftIndexToRightIndex]) > 0) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                T rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i].CompareTo(rightValue) > 0) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].CompareTo(rightArray[right.Selector.StartIndexInclusive]) > 0)
                {
                    result.All(left.Count);
                }
            }
        }

        public void WhereGreaterThanOrEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;

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
                    if (leftArray[leftIndex].CompareTo(rightArray[rightIndex]) >= 0) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) >= 0) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i].CompareTo(rightArray[i + leftIndexToRightIndex]) >= 0) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                T rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i].CompareTo(rightValue) >= 0) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive].CompareTo(rightArray[right.Selector.StartIndexInclusive]) >= 0)
                {
                    result.All(left.Count);
                }
            }
        }
    }
}

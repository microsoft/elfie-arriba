// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Transforms;

namespace XForm.Types
{
    /// <summary>
    ///  ComparableComparer exposes methods for each compare operator
    ///  implemented in terms of IComparable&lt;T&gt;
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ComparableComparer<T> : IDataBatchComparer where T : IComparable<T>
    {
        public void WhereEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;

            for (int i = 0; i < left.Count; ++i)
            {
                if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) == 0) result.Add(i);
            }
        }

        public void WhereNotEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;

            for (int i = 0; i < left.Count; ++i)
            {
                if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) != 0) result.Add(i);
            }
        }

        public void WhereLessThan(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;

            for (int i = 0; i < left.Count; ++i)
            {
                if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) < 0) result.Add(i);
            }
        }

        public void WhereLessThanOrEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;

            for (int i = 0; i < left.Count; ++i)
            {
                if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) <= 0) result.Add(i);
            }
        }

        public void WhereGreaterThan(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;

            for (int i = 0; i < left.Count; ++i)
            {
                if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) > 0) result.Add(i);
            }
        }

        public void WhereGreaterThanOrEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;

            for (int i = 0; i < left.Count; ++i)
            {
                if (leftArray[left.Index(i)].CompareTo(rightArray[right.Index(i)]) >= 0) result.Add(i);
            }
        }
    }
}

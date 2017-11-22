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
        public Type Type => typeof(T);
        public T Value;

        public void SetValue(object value)
        {
            Value = (T)value;
        }

        public void WhereEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            T[] sourceArray = (T[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value.CompareTo(sourceArray[realIndex]) == 0) result.Add(i);
            }
        }

        public void WhereNotEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            T[] sourceArray = (T[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value.CompareTo(sourceArray[realIndex]) != 0) result.Add(i);
            }
        }

        public void WhereLessThan(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            T[] sourceArray = (T[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value.CompareTo(sourceArray[realIndex]) > 0) result.Add(i);
            }
        }

        public void WhereLessThanOrEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            T[] sourceArray = (T[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value.CompareTo(sourceArray[realIndex]) >= 0) result.Add(i);
            }
        }

        public void WhereGreaterThan(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            T[] sourceArray = (T[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value.CompareTo(sourceArray[realIndex]) < 0) result.Add(i);
            }
        }

        public void WhereGreaterThanOrEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            T[] sourceArray = (T[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value.CompareTo(sourceArray[realIndex]) <= 0) result.Add(i);
            }
        }
    }
}

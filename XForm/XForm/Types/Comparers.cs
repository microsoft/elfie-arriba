// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
using XForm.Query;

namespace XForm.Transforms
{
    public static class ComparerFactory
    {
        public static Action<DataBatch, RowRemapper> Build(Type type, CompareOperator op, object value)
        {
            // Build a comparer
            IDataBatchComparer comparer = null;
            if (type == typeof(DateTime))
            {
                comparer = new DateTimeComparer();
            }
            else if (type == typeof(int))
            {
                comparer = new IntComparer();
            }
            else if (type == typeof(String8))
            {
                comparer = new ComparableComparer<String8>();
            }
            else
            {
                // TODO: Arriba code to build a generic in terms of T
                throw new NotImplementedException(type.Name);
            }

            // TODO: Extensibility via app.config like Elfie Reader/Writers

            // Set (and cast) the value to compare against
            comparer.SetValue(value);

            // Return the function for the desired comparison operation
            switch (op)
            {
                case CompareOperator.Equals:
                    return comparer.WhereEquals;
                case CompareOperator.NotEquals:
                    return comparer.WhereNotEquals;
                case CompareOperator.GreaterThan:
                    return comparer.WhereGreaterThan;
                case CompareOperator.GreaterThanOrEqual:
                    return comparer.WhereGreaterThanOrEquals;
                case CompareOperator.LessThan:
                    return comparer.WhereLessThan;
                case CompareOperator.LessThanOrEqual:
                    return comparer.WhereLessThanOrEquals;
                default:
                    throw new NotImplementedException(op.ToString());
            }
        }
    }

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

    internal class DateTimeComparer : IDataBatchComparer
    {
        public Type Type => typeof(DateTime);
        public DateTime Value;

        public void SetValue(object value)
        {
            Value = (DateTime)value;
        }

        public void WhereEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            DateTime[] sourceArray = (DateTime[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value == sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereNotEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            DateTime[] sourceArray = (DateTime[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value != sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereLessThan(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            DateTime[] sourceArray = (DateTime[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value > sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereLessThanOrEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            DateTime[] sourceArray = (DateTime[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value >= sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereGreaterThan(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            DateTime[] sourceArray = (DateTime[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value < sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereGreaterThanOrEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            DateTime[] sourceArray = (DateTime[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value <= sourceArray[realIndex]) result.Add(i);
            }
        }
    }

    internal class IntComparer : IDataBatchComparer
    {
        public Type Type => typeof(int);
        public int Value;

        public void SetValue(object value)
        {
            Value = (int)value;
        }

        public void WhereEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            int[] sourceArray = (int[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value == sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereNotEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            int[] sourceArray = (int[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value != sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereLessThan(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            int[] sourceArray = (int[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value > sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereLessThanOrEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            int[] sourceArray = (int[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value >= sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereGreaterThan(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            int[] sourceArray = (int[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value < sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereGreaterThanOrEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            int[] sourceArray = (int[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value <= sourceArray[realIndex]) result.Add(i);
            }
        }
    }
}

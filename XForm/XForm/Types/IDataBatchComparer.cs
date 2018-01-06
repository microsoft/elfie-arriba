// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Query;

namespace XForm.Types
{
    public interface IDataBatchComparer
    {
        void WhereEqual(DataBatch left, DataBatch right, BitVector vector);
        void WhereNotEqual(DataBatch left, DataBatch right, BitVector vector);
        void WhereLessThan(DataBatch left, DataBatch right, BitVector vector);
        void WhereLessThanOrEqual(DataBatch left, DataBatch right, BitVector vector);
        void WhereGreaterThan(DataBatch left, DataBatch right, BitVector vector);
        void WhereGreaterThanOrEqual(DataBatch left, DataBatch right, BitVector vector);

        void GetHashCodes(DataBatch values, int[] hashes);
    }

    public interface IDataBatchComparer<T>
    {
        bool WhereEqual(T left, T right);
        bool WhereNotEqual(T left, T right);
        bool WhereLessThan(T left, T right);
        bool WhereLessThanOrEqual(T left, T right);
        bool WhereGreaterThan(T left, T right);
        bool WhereGreaterThanOrEqual(T left, T right);

        int GetHashCode(T value);
    }

    public static class ComparerExtensions
    {
        public delegate void Comparer(DataBatch left, DataBatch right, BitVector vector);

        public delegate void WhereSingle<T>(T[] left, int index, int length, byte compareOperator, T right, byte booleanOperator, ulong[] vector, int vectorIndex);
        public delegate void Where<T>(T[] left, int leftIndex, byte compareOperator, T[] right, int rightIndex, int length, byte booleanOperator, ulong[] vector, int vectorIndex);

        public static Comparer TryBuild(this IDataBatchComparer comparer, CompareOperator cOp)
        {
            // Return the function for the desired comparison operation
            switch (cOp)
            {
                case CompareOperator.Equal:
                    return comparer.WhereEqual;
                case CompareOperator.NotEqual:
                    return comparer.WhereNotEqual;
                case CompareOperator.GreaterThan:
                    return comparer.WhereGreaterThan;
                case CompareOperator.GreaterThanOrEqual:
                    return comparer.WhereGreaterThanOrEqual;
                case CompareOperator.LessThan:
                    return comparer.WhereLessThan;
                case CompareOperator.LessThanOrEqual:
                    return comparer.WhereLessThanOrEqual;
                default:
                    throw new NotImplementedException(cOp.ToString());
            }
        }
    }
}

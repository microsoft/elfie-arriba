// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Query;

namespace XForm.Types
{
    public interface IXArrayComparer
    {
        void WhereEqual(XArray left, XArray right, BitVector vector);
        void WhereNotEqual(XArray left, XArray right, BitVector vector);
        void WhereLessThan(XArray left, XArray right, BitVector vector);
        void WhereLessThanOrEqual(XArray left, XArray right, BitVector vector);
        void WhereGreaterThan(XArray left, XArray right, BitVector vector);
        void WhereGreaterThanOrEqual(XArray left, XArray right, BitVector vector);

        void GetHashCodes(XArray values, int[] hashes);
    }

    public interface IXArrayComparer<T> : IXArrayComparer
    {
        bool WhereEqual(T left, T right);
        bool WhereNotEqual(T left, T right);
        bool WhereLessThan(T left, T right);
        bool WhereLessThanOrEqual(T left, T right);
        bool WhereGreaterThan(T left, T right);
        bool WhereGreaterThanOrEqual(T left, T right);

        int GetHashCode(T value);
    }

    public interface IXArrayTextComparer : IXArrayComparer
    {
        void WhereContains(XArray left, XArray right, BitVector vector);
        void WhereContainsExact(XArray left, XArray right, BitVector vector);
        void WhereStartsWith(XArray left, XArray right, BitVector vector);
        void WhereEndsWith(XArray left, XArray right, BitVector vector);
    }

    public static class ComparerExtensions
    {
        public delegate void Comparer(XArray left, XArray right, BitVector vector);

        public delegate void WhereSingle<T>(T[] left, int index, int length, byte compareOperator, T right, byte booleanOperator, ulong[] vector, int vectorIndex);
        public delegate void Where<T>(T[] left, int leftIndex, byte compareOperator, T[] right, int rightIndex, int length, byte booleanOperator, ulong[] vector, int vectorIndex);

        public static Comparer TryBuild(this IXArrayComparer comparer, CompareOperator cOp)
        {
            // Return text comparisons if this is a text comparer only
            IXArrayTextComparer textComparer = comparer as IXArrayTextComparer;
            if (textComparer != null)
            {
                switch (cOp)
                {
                    case CompareOperator.Contains:
                        return textComparer.WhereContains;
                    case CompareOperator.ContainsExact:
                        return textComparer.WhereContainsExact;
                    case CompareOperator.StartsWith:
                        return textComparer.WhereStartsWith;
                    case CompareOperator.EndsWith:
                        return textComparer.WhereEndsWith;
                }
            }

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
                case CompareOperator.Contains:
                    // Contains resolves to Equals by default
                    return comparer.WhereEqual;
            }

            // Throw if this comparison isn't supported
            throw new ArgumentException($"{cOp} not supported for column");
        }
    }
}

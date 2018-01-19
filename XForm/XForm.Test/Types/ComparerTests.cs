// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Query;
using XForm.Transforms;
using XForm.Types;

namespace XForm.Test.Query
{
    [TestClass]
    public class ComparerTests
    {
        [TestMethod]
        public void Comparer_Where()
        {
            Comparer_AllTypes();
            NativeAccelerator.Enable();
            Comparer_AllTypes();
        }

        private static void Comparer_AllTypes()
        {
            int[] ascending = Enumerable.Range(0, 120).ToArray();
            int[] alternating = new int[120];
            for (int i = 0; i < 120; ++i)
            {
                alternating[i] = (i % 2 == 0 ? i : 120 - i);
            }

            Comparer_VerifyWhereAll<byte>(ascending.Select((i) => (byte)i).ToArray(), alternating.Select((i) => (byte)i).ToArray(), 100);
            Comparer_VerifyWhereAll<sbyte>(ascending.Select((i) => (sbyte)i).ToArray(), alternating.Select((i) => (sbyte)i).ToArray(), 100);
            Comparer_VerifyWhereAll<ushort>(ascending.Select((i) => (ushort)i).ToArray(), alternating.Select((i) => (ushort)i).ToArray(), 100);
            Comparer_VerifyWhereAll<short>(ascending.Select((i) => (short)i).ToArray(), alternating.Select((i) => (short)i).ToArray(), 100);
            Comparer_VerifyWhereAll<uint>(ascending.Select((i) => (uint)i).ToArray(), alternating.Select((i) => (uint)i).ToArray(), 100);
            Comparer_VerifyWhereAll<int>(ascending.Select((i) => (int)i).ToArray(), alternating.Select((i) => (int)i).ToArray(), 100);
            Comparer_VerifyWhereAll<ulong>(ascending.Select((i) => (ulong)i).ToArray(), alternating.Select((i) => (ulong)i).ToArray(), 100);
            Comparer_VerifyWhereAll<long>(ascending.Select((i) => (long)i).ToArray(), alternating.Select((i) => (long)i).ToArray(), 100);
            Comparer_VerifyWhereAll<float>(ascending.Select((i) => (float)i).ToArray(), alternating.Select((i) => (float)i).ToArray(), 100);
            Comparer_VerifyWhereAll<double>(ascending.Select((i) => (double)i).ToArray(), alternating.Select((i) => (double)i).ToArray(), 100);

            int[] someNegative = Enumerable.Range(-50, 50).ToArray();

            // Try with negative numbers for signed types
            Comparer_VerifyWhereAll<sbyte>(someNegative.Select((i) => (sbyte)i).ToArray(), alternating.Select((i) => (sbyte)i).ToArray(), 0);
            Comparer_VerifyWhereAll<short>(someNegative.Select((i) => (short)i).ToArray(), alternating.Select((i) => (short)i).ToArray(), 0);
            Comparer_VerifyWhereAll<int>(someNegative.Select((i) => (int)i).ToArray(), alternating.Select((i) => (int)i).ToArray(), 0);
            Comparer_VerifyWhereAll<long>(someNegative.Select((i) => (long)i).ToArray(), alternating.Select((i) => (long)i).ToArray(), 0);
        }

        private static void Comparer_VerifyWhereAll<T>(T[] left, T[] right, T value) where T : IComparable<T>
        {
            // Try operations between array and single value
            Comparer_VerifyWhere<T>(XArray.All(left, left.Length), XArray.Single(new T[1] { value }, left.Length), CompareOperator.Equal);
            Comparer_VerifyWhere<T>(XArray.All(left, left.Length), XArray.Single(new T[1] { value }, left.Length), CompareOperator.NotEqual);
            Comparer_VerifyWhere<T>(XArray.All(left, left.Length), XArray.Single(new T[1] { value }, left.Length), CompareOperator.GreaterThan);
            Comparer_VerifyWhere<T>(XArray.All(left, left.Length), XArray.Single(new T[1] { value }, left.Length), CompareOperator.GreaterThanOrEqual);
            Comparer_VerifyWhere<T>(XArray.All(left, left.Length), XArray.Single(new T[1] { value }, left.Length), CompareOperator.LessThan);
            Comparer_VerifyWhere<T>(XArray.All(left, left.Length), XArray.Single(new T[1] { value }, left.Length), CompareOperator.LessThanOrEqual);

            // Try operations between two arrays
            Comparer_VerifyWhere<T>(XArray.All(left, left.Length), XArray.All(right, right.Length), CompareOperator.Equal);
            Comparer_VerifyWhere<T>(XArray.All(left, left.Length), XArray.All(right, right.Length), CompareOperator.NotEqual);
            Comparer_VerifyWhere<T>(XArray.All(left, left.Length), XArray.All(right, right.Length), CompareOperator.GreaterThan);
            Comparer_VerifyWhere<T>(XArray.All(left, left.Length), XArray.All(right, right.Length), CompareOperator.GreaterThanOrEqual);
            Comparer_VerifyWhere<T>(XArray.All(left, left.Length), XArray.All(right, right.Length), CompareOperator.LessThan);
            Comparer_VerifyWhere<T>(XArray.All(left, left.Length), XArray.All(right, right.Length), CompareOperator.LessThanOrEqual);
        }

        private static void Comparer_VerifyWhere<T>(XArray left, XArray right, CompareOperator cOp) where T : IComparable<T>
        {
            ComparerExtensions.Comparer comparer = TypeProviderFactory.Get(typeof(T).Name).TryGetComparer(cOp);
            BitVector vector = new BitVector(left.Count);

            // Verify 'Set' on an empty set works as expected (normal matches)
            comparer(left, right, vector);
            Comparer_VerifySetMatches<T>(vector, left, right, cOp);
        }

        private static void Comparer_VerifySetMatches<T>(BitVector set, XArray left, XArray right, CompareOperator cOp) where T : IComparable<T>
        {
            // Validate each array entry is included (or not) as the individual comparison would
            int expectedCount = 0;
            T[] leftArray = (T[])left.Array;
            T[] rightArray = (T[])right.Array;
            for (int i = 0; i < left.Selector.Count; ++i)
            {
                bool shouldBeIncluded = CompareSingle(leftArray[left.Index(i)], rightArray[right.Index(i)], cOp);
                if (shouldBeIncluded) expectedCount++;
                Assert.AreEqual(shouldBeIncluded, set[i]);
            }

            // Verify overall count is right
            Assert.AreEqual(expectedCount, set.Count);

            // Get a page of all matching indices
            int[] values = new int[set.Capacity];
            int index = 0;
            int count = set.Page(values, ref index);

            // Verify the paged indices have the right count and every index there matched
            Assert.AreEqual(expectedCount, count);
            for (int i = 0; i < count; ++i)
            {
                Assert.IsTrue(CompareSingle(leftArray[left.Index(values[i])], rightArray[right.Index(values[i])], cOp));
            }
        }

        private static bool CompareSingle<T>(T left, T right, CompareOperator cOp) where T : IComparable<T>
        {
            // Implement dynamic comparison to validate V5 comparison
            int cmp = left.CompareTo(right);

            switch (cOp)
            {
                case CompareOperator.Equal:
                    return cmp == 0;
                case CompareOperator.NotEqual:
                    return cmp != 0;
                case CompareOperator.GreaterThan:
                    return cmp > 0;
                case CompareOperator.GreaterThanOrEqual:
                    return cmp >= 0;
                case CompareOperator.LessThan:
                    return cmp < 0;
                case CompareOperator.LessThanOrEqual:
                    return cmp <= 0;
            }

            throw new NotImplementedException(cOp.ToString());
        }
    }
}

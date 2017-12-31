using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
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
            // Try every operation on an ascending set
            Comparer_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (byte)i).ToArray(), (byte)100);
            Comparer_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (sbyte)i).ToArray(), (sbyte)100);
            Comparer_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (ushort)i).ToArray(), (ushort)100);
            Comparer_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (short)i).ToArray(), (short)100);
            Comparer_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (uint)i).ToArray(), (uint)100);
            Comparer_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (int)i).ToArray(), (int)100);
            Comparer_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (ulong)i).ToArray(), (ulong)100);
            Comparer_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (long)i).ToArray(), (long)100);
            Comparer_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (float)i).ToArray(), (float)100);
            Comparer_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (double)i).ToArray(), (double)100);

            // Try every operation on an alternating set
            int[] alternating = new int[120];
            for (int i = 0; i < 120; ++i)
            {
                alternating[i] = (i % 2 == 0 ? i : 120 - i);
            }

            Comparer_VerifyWhereAll(alternating.Select((i) => (byte)i).ToArray(), (byte)100);
            Comparer_VerifyWhereAll(alternating.Select((i) => (sbyte)i).ToArray(), (sbyte)100);
            Comparer_VerifyWhereAll(alternating.Select((i) => (ushort)i).ToArray(), (ushort)100);
            Comparer_VerifyWhereAll(alternating.Select((i) => (short)i).ToArray(), (short)100);
            Comparer_VerifyWhereAll(alternating.Select((i) => (uint)i).ToArray(), (uint)100);
            Comparer_VerifyWhereAll(alternating.Select((i) => (int)i).ToArray(), (int)100);
            Comparer_VerifyWhereAll(alternating.Select((i) => (ulong)i).ToArray(), (ulong)100);
            Comparer_VerifyWhereAll(alternating.Select((i) => (long)i).ToArray(), (long)100);
            Comparer_VerifyWhereAll(alternating.Select((i) => (float)i).ToArray(), (float)100);
            Comparer_VerifyWhereAll(alternating.Select((i) => (double)i).ToArray(), (double)100);


            // Try with negative numbers for signed types
            Comparer_VerifyWhereAll(Enumerable.Range(-50, 50).Select((i) => (sbyte)i).ToArray(), (sbyte)0);
            Comparer_VerifyWhereAll(Enumerable.Range(-50, 50).Select((i) => (short)i).ToArray(), (short)0);
            Comparer_VerifyWhereAll(Enumerable.Range(-50, 50).Select((i) => (int)i).ToArray(), (int)0);
            Comparer_VerifyWhereAll(Enumerable.Range(-50, 50).Select((i) => (long)i).ToArray(), (long)0);
        }

        private static void Comparer_VerifyWhereAll<T>(T[] array, T value) where T : IComparable<T>
        {
            Comparer_VerifyWhere(array, value, CompareOperator.Equal);
            Comparer_VerifyWhere(array, value, CompareOperator.NotEqual);
            Comparer_VerifyWhere(array, value, CompareOperator.GreaterThan);
            Comparer_VerifyWhere(array, value, CompareOperator.GreaterThanOrEqual);
            Comparer_VerifyWhere(array, value, CompareOperator.LessThan);
            Comparer_VerifyWhere(array, value, CompareOperator.LessThanOrEqual);
        }

        private static void Comparer_VerifyWhere<T>(T[] array, T value, CompareOperator cOp) where T : IComparable<T>
        {
            Action<DataBatch, DataBatch, RowRemapper> comparer = TypeProviderFactory.Get(typeof(T).Name).TryGetComparer(cOp);

            RowRemapper mapper = new RowRemapper();
            DataBatch left = DataBatch.All(array, array.Length);
            DataBatch right = DataBatch.Single(new T[1] { value }, array.Length);

            // Verify 'Set' on an empty set works as expected (normal matches)
            comparer(left, right, mapper);
            Comparer_VerifySetMatches(mapper.Vector, array, value, cOp);
        }

        private static void Comparer_VerifySetMatches<T>(BitVector set, T[] array, T value, CompareOperator cOp) where T : IComparable<T>
        {
            // Validate each array entry is included (or not) as the individual comparison would
            int expectedCount = 0;
            for (int i = 0; i < array.Length; ++i)
            {
                bool shouldBeIncluded = CompareSingle(array[i], value, cOp);
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
                Assert.IsTrue(CompareSingle(array[values[i]], value, cOp));
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using V5;
using V5.Collections;

namespace V5.Test.Collections
{
    [TestClass]
    public class IndexSetTests
    {
        [TestMethod]
        public void IndexSet_Basics()
        {
            IndexSet set = new IndexSet(999);

            // Verify Count, All, None
            Assert.AreEqual(0, set.Count, "Set should start empty");

            set.All(999);
            Assert.AreEqual(999, set.Count, "All should set through length only.");

            set.None();
            Assert.AreEqual(0, set.Count, "None should clear");

            // Verify individual set and get and Array.And
            byte[] values = new byte[999];
            for (int i = 0; i < 999; ++i)
            {
                // Set only 'i' via setter and verify
                set.None();
                set[i] = true;
                AssertOnly(set, 999, i);

                // Set only 'i' via And(Array) and verify
                set.None();
                Array.Clear(values, 0, values.Length);
                values[i] = 1;
                set.All(999).Where(BooleanOperator.And, values, CompareOperator.GreaterThan, (byte)0);
                AssertOnly(set, 999, i);
            }
        }

        [TestMethod]
        public void IndexSet_Page()
        {
            IndexSet set = new IndexSet(900);
            Span<int> page = new Span<int>(new int[10]);

            // Verify if nothing is set, page doesn't find anything and returns -1
            set.None();
            Assert.AreEqual(-1, set.Page(ref page, 0));
            Assert.AreEqual(0, page.Length);

            // Set 15 values (every 3rd under 45)
            for (int i = 0; i < 45; i += 3)
            {
                set[i] = true;
            }

            // Verify a full page of results is returned with the correct next index to check
            Assert.AreEqual(28, set.Page(ref page, 0));
            Assert.AreEqual(10, page.Length);
            Assert.AreEqual("0, 3, 6, 9, 12, 15, 18, 21, 24, 27", string.Join(", ", page));

            // Verify the second page is returned with -1 and the last five values
            Assert.AreEqual(-1, set.Page(ref page, 28));
            Assert.AreEqual(5, page.Length);
            Assert.AreEqual("30, 33, 36, 39, 42", string.Join(", ", page));
        }

        private static void AssertOnly(IndexSet set, int limit, int expected)
        {
            Assert.IsTrue(set[expected]);
            Assert.AreEqual(1, set.Count);

            for (int j = 0; j < limit; ++j)
            {
                Assert.AreEqual(j == expected, set[j]);
            }
        }

        [TestMethod]
        public void IndexSet_Where()
        {
            // Try every operation on an ascending set
            IndexSet_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (byte)i).ToArray(), (byte)100);
            IndexSet_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (sbyte)i).ToArray(), (sbyte)100);
            IndexSet_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (ushort)i).ToArray(), (ushort)100);
            IndexSet_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (short)i).ToArray(), (short)100);
            IndexSet_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (uint)i).ToArray(), (uint)100);
            IndexSet_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (int)i).ToArray(), (int)100);
            IndexSet_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (ulong)i).ToArray(), (ulong)100);
            IndexSet_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (long)i).ToArray(), (long)100);
            IndexSet_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (float)i).ToArray(), (float)100);
            IndexSet_VerifyWhereAll(Enumerable.Range(0, 120).Select((i) => (double)i).ToArray(), (double)100);

            // Try every operation on an alternating set
            int[] alternating = new int[120];
            for (int i = 0; i < 120; ++i)
            {
                alternating[i] = (i % 2 == 0 ? i : 120 - i);
            }

            IndexSet_VerifyWhereAll(alternating.Select((i) => (byte)i).ToArray(), (byte)100);
            IndexSet_VerifyWhereAll(alternating.Select((i) => (sbyte)i).ToArray(), (sbyte)100);
            IndexSet_VerifyWhereAll(alternating.Select((i) => (ushort)i).ToArray(), (ushort)100);
            IndexSet_VerifyWhereAll(alternating.Select((i) => (short)i).ToArray(), (short)100);
            IndexSet_VerifyWhereAll(alternating.Select((i) => (uint)i).ToArray(), (uint)100);
            IndexSet_VerifyWhereAll(alternating.Select((i) => (int)i).ToArray(), (int)100);
            IndexSet_VerifyWhereAll(alternating.Select((i) => (ulong)i).ToArray(), (ulong)100);
            IndexSet_VerifyWhereAll(alternating.Select((i) => (long)i).ToArray(), (long)100);
            IndexSet_VerifyWhereAll(alternating.Select((i) => (float)i).ToArray(), (float)100);
            IndexSet_VerifyWhereAll(alternating.Select((i) => (double)i).ToArray(), (double)100);
        }

        private static void IndexSet_VerifyWhereAll<T>(T[] array, T value) where T : IComparable<T>
        {
            IndexSet_VerifyWhere(array, value, CompareOperator.Equals);
            IndexSet_VerifyWhere(array, value, CompareOperator.NotEquals);
            IndexSet_VerifyWhere(array, value, CompareOperator.GreaterThan);
            IndexSet_VerifyWhere(array, value, CompareOperator.GreaterThanOrEqual);
            IndexSet_VerifyWhere(array, value, CompareOperator.LessThan);
            IndexSet_VerifyWhere(array, value, CompareOperator.LessThanOrEqual);
        }

        private static void IndexSet_VerifyWhere<T>(T[] array, T value, CompareOperator cOp) where T : IComparable<T>
        {
            IndexSet set = new IndexSet(array.Length);
            Assert.AreEqual(0, set.Count, "Verify set starts empty.");

            // Verify 'Set' on an empty set works as expected (normal matches)
            set.Where(BooleanOperator.Set, array, cOp, value);
            IndexSet_VerifySetMatches(set, array, value, cOp);

            // Verify 'Set' on a full set works as expected (normal matches)
            set.All(array.Length);
            set.Where(BooleanOperator.Set, array, cOp, value);
            IndexSet_VerifySetMatches(set, array, value, cOp);

            // Verify 'And' on an empty set works as expected (no matches)
            set.None();
            set.Where(BooleanOperator.And, array, cOp, value);
            Assert.AreEqual(0, set.Count);

            // Verify 'And' on a full set works as expected (normal matches)
            set.All(array.Length);
            set.Where(BooleanOperator.And, array, cOp, value);
            IndexSet_VerifySetMatches(set, array, value, cOp);

            // Verify 'Or' on an empty set works as expected (normal matches)
            set.None();
            set.Where(BooleanOperator.Or, array, cOp, value);
            IndexSet_VerifySetMatches(set, array, value, cOp);

            // Verify 'Or' on a full set works as expected (all matches)
            set.All(array.Length);
            set.Where(BooleanOperator.Or, array, cOp, value);
            Assert.AreEqual(array.Length, set.Count);

            // Verify 'AndNot' on an empty set works as expected (no matches)
            set.None();
            set.Where(BooleanOperator.AndNot, array, cOp, value);
            Assert.AreEqual(0, set.Count);

            // Verify 'AndNot' on a full set works as expected (complement of matches)
            set.All(array.Length);
            set.Where(BooleanOperator.AndNot, array, cOp, value);
            IndexSet_VerifySetMatches(set, array, value, Complement(cOp));
        }

        private static void IndexSet_VerifySetMatches<T>(IndexSet set, T[] array, T value, CompareOperator cOp) where T : IComparable<T>
        {
            // Validate each array entry is included (or not) as the individual comparison would
            int expectedCount = 0;
            for(int i = 0; i < array.Length; ++i)
            {
                bool shouldBeIncluded = CompareSingle(array[i], value, cOp);
                if (shouldBeIncluded) expectedCount++;
                Assert.AreEqual(shouldBeIncluded, set[i]);
            }

            // Verify overall count is right
            Assert.AreEqual(expectedCount, set.Count);

            // Get a page of all matching indices
            Span<int> values = new Span<int>(new int[set.Capacity]);
            set.Page(ref values, 0);

            // Verify the paged indices have the right count and every index there matched
            Assert.AreEqual(expectedCount, values.Length);
            for(int i = 0; i < values.Length; ++i)
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
                case CompareOperator.Equals:
                    return cmp == 0;
                case CompareOperator.NotEquals:
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

        private static CompareOperator Complement(CompareOperator cOp)
        {
            switch(cOp)
            {
                case CompareOperator.Equals:
                    return CompareOperator.NotEquals;

                case CompareOperator.NotEquals:
                    return CompareOperator.Equals;

                case CompareOperator.GreaterThan:
                    return CompareOperator.LessThanOrEqual;

                case CompareOperator.GreaterThanOrEqual:
                    return CompareOperator.LessThan;

                case CompareOperator.LessThan:
                    return CompareOperator.GreaterThanOrEqual;

                case CompareOperator.LessThanOrEqual:
                    return CompareOperator.GreaterThan;
            }

            throw new NotImplementedException(cOp.ToString());
        }
    }
}
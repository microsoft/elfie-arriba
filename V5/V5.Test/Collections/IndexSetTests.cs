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
            IndexSet_Where(Enumerable.Range(0, 120).Select((i) => (byte)i).ToArray(), (byte)120);
        }

        private static void IndexSet_Where<T>(T[] zeroToN, T oneHundred)
        {
            IndexSet set = new IndexSet((uint)zeroToN.Length);
            Assert.AreEqual(0, set.Count, "Verify set starts empty.");

            Assert.AreEqual(zeroToN.Length - 101, set.All((uint)zeroToN.Length).Where(BooleanOperator.And, zeroToN, CompareOperator.GreaterThan, oneHundred).Count);
            Assert.AreEqual(zeroToN.Length - 100, set.All((uint)zeroToN.Length).Where(BooleanOperator.And, zeroToN, CompareOperator.GreaterThanOrEqual, oneHundred).Count);
            Assert.AreEqual(100, set.All((uint)zeroToN.Length).Where(BooleanOperator.And, zeroToN, CompareOperator.LessThan, oneHundred).Count);
            Assert.AreEqual(101, set.All((uint)zeroToN.Length).Where(BooleanOperator.And, zeroToN, CompareOperator.LessThanOrEqual, oneHundred).Count);
            Assert.AreEqual(1, set.All((uint)zeroToN.Length).Where(BooleanOperator.And, zeroToN, CompareOperator.Equals, oneHundred).Count);
            Assert.AreEqual(zeroToN.Length - 1, set.All((uint)zeroToN.Length).Where(BooleanOperator.And, zeroToN, CompareOperator.NotEquals, oneHundred).Count);
        }
    }
}

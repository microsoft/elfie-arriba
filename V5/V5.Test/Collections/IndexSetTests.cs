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
                set.All(999).And(values, CompareOperator.GreaterThan, (byte)0);
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

            // Set 15 values (even indices under 30)
            for (int i = 0; i < 30; i += 2)
            {
                set[i] = true;
            }

            // Verify a full page of results is returned with the correct next index to check
            Assert.AreEqual(19, set.Page(ref page, 0));
            Assert.AreEqual(10, page.Length);
            Assert.AreEqual("0, 2, 4, 6, 8, 10, 12, 14, 16, 18", string.Join(", ", page));

            // Verify the second page is returned with -1 and the last five values
            Assert.AreEqual(-1, set.Page(ref page, 19));
            Assert.AreEqual(5, page.Length);
            Assert.AreEqual("20, 22, 24, 26, 28", string.Join(", ", page));
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
        public void IndexSet_And()
        {
            IndexSet set = new IndexSet(120);
            byte[] compareTo = Enumerable.Range(0, 120).Select((i) => (byte)i).ToArray();

            Assert.AreEqual(0, set.Count, "Verify set starts empty.");
            Assert.AreEqual(19, set.All(120).And<byte>(compareTo, CompareOperator.GreaterThan, 100).Count, "Verify 19 values > 100");
            Assert.AreEqual(20, set.All(120).And<byte>(compareTo, CompareOperator.GreaterThanOrEqual, 100).Count, "Verify 20 values >= 100");
            Assert.AreEqual(100, set.All(120).And<byte>(compareTo, CompareOperator.LessThan, 100).Count, "Verify 100 values <= 100");
            Assert.AreEqual(101, set.All(120).And<byte>(compareTo, CompareOperator.LessThanOrEqual, 100).Count, "Verify 101 values < 100");
            Assert.AreEqual(1, set.All(120).And<byte>(compareTo, CompareOperator.Equals, 100).Count, "Verify 1 value == 100");
            Assert.AreEqual(119, set.All(120).And<byte>(compareTo, CompareOperator.NotEquals, 100).Count, "Verify 119 values != 100");
        }
    }
}

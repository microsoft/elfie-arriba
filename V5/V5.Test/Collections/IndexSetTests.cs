using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
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

            Assert.AreEqual(0, set.Count, "Set should start empty");

            set.All(999);
            Assert.AreEqual(999, set.Count, "All should set through length only.");

            set.None();
            Assert.AreEqual(0, set.Count, "None should clear");

            byte[] values = new byte[999];

            for (int i = 0; i < 999; ++i)
            {
                set.None();
                set[i] = true;
                AssertOnly(set, 999, i);

                set.None();

                Array.Clear(values, 0, values.Length);
                values[i] = 1;
                set.All(999).And(values, Query.Operator.GreaterThan, (byte)0);
                AssertOnly(set, 999, i);
            }
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
            IndexSet set = new IndexSet(120);
            byte[] compareTo = Enumerable.Range(0, 120).Select((i) => (byte)i).ToArray();

            Assert.AreEqual(0, set.Count, "Verify set starts empty.");
            Assert.AreEqual(19, set.All(120).And<byte>(compareTo, Query.Operator.GreaterThan, 100).Count, "Verify 19 values > 100");

            
        }
    }
}

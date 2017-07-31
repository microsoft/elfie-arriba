using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace V5.Test.Collections
{
    [TestClass]
    public class HashSet5Tests
    {
        [TestMethod]
        public void HashSet5_Basics()
        {
            HashSet<int> expected = new HashSet<int>();
            HashSet5<int> actual = new HashSet5<int>();

            // Add random items. Verify the HashSet correctly reports whether they're already there and were added
            Random r = new Random(5);
            for(int i = 0; i < 100000; ++i)
            {
                int value = r.Next() << 1;

                bool alreadyAdded = expected.Contains(value);
                Assert.AreEqual(alreadyAdded, actual.Contains(value));
                Assert.AreEqual(expected.Add(value), actual.Add(value));
                if (!actual.Contains(value)) Debugger.Break();
                Assert.IsTrue(actual.Contains(value));
            }

            // Verify counts match
            Assert.AreEqual(expected.Count, actual.Count);

            // Enumerate everything. Verify everything removed.
            foreach(int value in actual)
            {
                Assert.IsTrue(expected.Remove(value));
            }

            Assert.AreEqual(0, expected.Count);
        }
    }
}

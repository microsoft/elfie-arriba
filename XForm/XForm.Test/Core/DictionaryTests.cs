using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace XForm.Core
{
    [TestClass]
    public class HashSet5Tests
    {
        [TestMethod]
        public void Dictionary_Basics()
        {
            HashSet<int> expected = new HashSet<int>();
            Dictionary<int> actual = new Dictionary<int>();

            // Add random items. Verify the HashSet correctly reports whether they're already there and were added
            Random r = new Random(5);
            for(int i = 0; i < 100000; ++i)
            {
                int value = r.Next();
                Assert.AreEqual(expected.Add(value), actual.Add(value));
                if (!actual.Contains(value)) Debugger.Break();
                Assert.IsTrue(actual.Contains(value));
            }

            Trace.WriteLine($"Mean: {actual.DistanceMean():n2}, Max Probe: {(actual.MaxProbeLength)}");

            // Verify counts match
            Assert.AreEqual(expected.Count, actual.Count);

            // Enumerate expected values and verify they're all still found
            foreach(int value in expected)
            {
                if (!actual.Contains(value)) Debugger.Break();
                Assert.IsTrue(actual.Contains(value));
            }

            // Enumerate everything, removing from expected as we go. Verify everything enumerated once.
            foreach(int value in actual)
            {
                Assert.IsTrue(expected.Remove(value));
            }
            Assert.AreEqual(0, expected.Count);

            // Remove everything. Verify everything removed, items not yet removed are properly found
            HashSet<int> copy = new HashSet<int>(actual);
            foreach(int value in copy)
            {
                Assert.IsTrue(actual.Contains(value));
                Assert.IsTrue(actual.Remove(value));
                Assert.IsFalse(actual.Contains(value));
            }

            Assert.AreEqual(0, actual.Count);
        }
    }
}

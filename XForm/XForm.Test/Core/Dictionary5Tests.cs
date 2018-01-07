using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using XForm.Types;

namespace XForm.Core
{
    [TestClass]
    public class Dictionary5Tests
    {
        [TestMethod]
        public void Dictionary5_Basics()
        {
            Dictionary<int, int> expected = new Dictionary<int, int>();
            Dictionary5<int, int> actual = new Dictionary5<int, int>(new EqualityComparerAdapter<int>(TypeProviderFactory.Get(typeof(int)).TryGetComparer()));

            // Add random items. Verify the HashSet correctly reports whether they're already there and were added
            Random r = new Random(5);
            for(int i = 0; i < 100000; ++i)
            {
                int value = r.Next();
                expected.Add(value, i);
                actual.Add(value, i);
                if (!actual.Contains(value)) Debugger.Break();
                Assert.IsTrue(actual.Contains(value));
            }

            Trace.WriteLine($"Mean: {actual.DistanceMean():n2}, Max Probe: {(actual.MaxProbeLength)}");

            // Verify counts match
            Assert.AreEqual(expected.Count, actual.Count);

            // Enumerate expected values and verify they're all still found
            foreach(int value in expected.Keys)
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

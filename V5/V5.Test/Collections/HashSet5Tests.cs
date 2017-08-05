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
        public void Murmur_Basics()
        {
            HashSet<uint> set2 = new HashSet<uint>();
            HashSet<uint> set3 = new HashSet<uint>();
            for(uint i = 0; i < 100000; ++i)
            {
                Assert.IsTrue(set2.Add(MurmurHasher.Murmur2(i, 0)));
                Assert.IsTrue(set3.Add(MurmurHasher.Murmur3(i, 0)));
            }
        }

        [TestMethod]
        public void HashSet5_Basics()
        {
            HashSet<int> expected = new HashSet<int>();
            HashSet5<int> actual = new HashSet5<int>();

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

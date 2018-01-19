// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            for (int i = 0; i < 10000; ++i)
            {
                int key = r.Next();
                expected.Add(key, i);
                actual.Add(key, i);
                Assert.IsTrue(actual.ContainsKey(key));
            }

            Trace.WriteLine($"Mean: {actual.DistanceMean():n2}, Max Probe: {(actual.MaxProbeLength)}");

            // Verify counts match
            Assert.AreEqual(actual.Count, actual.AllKeys.Count());
            Assert.AreEqual(expected.Count, actual.Count);

            // Enumerate expected values and verify they're all still found
            foreach (int key in expected.Keys)
            {
                if (!actual.ContainsKey(key)) Debugger.Break();
                Assert.IsTrue(actual.ContainsKey(key));
            }

            // Enumerate everything, removing from expected as we go. Verify everything enumerated once.
            foreach (int key in actual.AllKeys)
            {
                Assert.IsTrue(expected.Remove(key));
            }
            Assert.AreEqual(0, expected.Count);

            // Remove everything. Verify everything removed, items not yet removed are properly found
            HashSet<int> copy = new HashSet<int>(actual.AllKeys);
            foreach (int key in copy)
            {
                Assert.IsTrue(actual.ContainsKey(key));
                Assert.IsTrue(actual.Remove(key));
                Assert.IsFalse(actual.ContainsKey(key));
            }

            Assert.AreEqual(0, actual.Count);
        }
    }
}

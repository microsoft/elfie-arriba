// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Test.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model.Structures
{
    [TestClass]
    public class PartialArrayTests
    {
        [TestMethod]
        public void PartialArray_Basics()
        {
            // Default Constructor - no fixed size
            PartialArray<int> a = new PartialArray<int>();

            // Verify empty to start
            Assert.AreEqual(0, a.Count);
            Assert.AreEqual(0, a.Capacity);
            Assert.IsFalse(a.IsStaticSize);
            Assert.IsFalse(a.IsFull);

            // Verify Add doesn't throw
            for (int i = 0; i < 100; ++i)
            {
                a.Add(i);
            }

            // Verify count and capacity are right, IsFull is still false
            Assert.AreEqual(100, a.Count);
            Assert.IsTrue(a.Capacity >= 100);
            Assert.IsFalse(a.IsFull);

            // Verify we can get values back
            for (int i = 0; i < 100; ++i)
            {
                Assert.AreEqual(i, a[i]);
            }

            // Verify changing a value works
            a[0] = 50;
            Assert.AreEqual(50, a[0]);
            a[0] = 0;

            // Verify round trip works [Primitives only]
            PartialArray<int> readArray = new PartialArray<int>();
            Verify.RoundTrip<PartialArray<int>>(a, readArray);
            a = readArray;

            // Verify count and capacity are right, IsFull is still false
            Assert.AreEqual(100, a.Count);
            Assert.IsTrue(a.Capacity >= 100);
            Assert.IsFalse(a.IsFull);

            // Verify we can get values back
            for (int i = 0; i < 100; ++i)
            {
                Assert.AreEqual(i, a[i]);
            }

            // Verify clear works
            a.Clear();
            Assert.AreEqual(0, a.Count);
            Assert.IsTrue(a.Capacity >= 100);
            Assert.IsFalse(a.IsFull);

            // Verify Add after Clear works
            a.Add(10);
            Assert.AreEqual(1, a.Count);
            Assert.AreEqual(10, a[0]);
        }

        [TestMethod]
        public void PartialArray_NonResizable()
        {
            // Create a fixed PartialArray
            PartialArray<int> a = new PartialArray<int>(10, true);

            // Verify it was allocated already
            Assert.AreEqual(10, a.Capacity);

            // Try to add too many values to it (checking IsFull)
            for (int i = 0; i < 100; ++i)
            {
                if (a.IsFull) break;
                a.Add(i);
            }

            // Verify the first Capacity were added, and no resize happened
            Assert.AreEqual(10, a.Count);
            Assert.AreEqual(10, a.Capacity);
        }

        [TestMethod]
        public void PartialArray_MembersProvided()
        {
            // Build a partially filled array
            int[] array = new int[10];
            for (int i = 0; i < 5; ++i)
            {
                array[i] = i;
            }

            // Build a PartialArray around it
            PartialArray<int> a = new PartialArray<int>(array, 5, true);

            // Verify it knows it's partially full already
            Assert.AreEqual(5, a.Count);
            Assert.AreEqual(3, a[3]);
            Assert.AreEqual(10, a.Capacity);
            Assert.IsTrue(a.IsStaticSize);
            Assert.IsFalse(a.IsFull);

            // Add another value; verify it goes to the right place
            a.Add(5);
            Assert.AreEqual(6, a.Count);
            Assert.AreEqual(5, a[5]);
            Assert.AreEqual(5, array[5]);
        }
    }
}

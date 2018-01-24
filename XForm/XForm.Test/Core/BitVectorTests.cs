// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XForm.Test.Core
{
    [TestClass]
    public class BitVectorTests
    {
        // Page fully through a set [Performance Scenario]
        public static int PageAll(BitVector set, int[] page)
        {
            int count = 0;

            int next = 0;
            while (next != -1)
            {
                count += set.Page(page, ref next);
            }

            return count;
        }

        [TestMethod]
        public void BitVector_Basics()
        {
            BitVector set = new BitVector(999);

            // Verify Count, All, None
            Assert.AreEqual(0, set.Count, "Set should start empty");

            set.All(999);
            Assert.AreEqual(999, set.Count, "All should set through length only.");

            set.ClearAbove(900);
            Assert.AreEqual(900, set.Count, "ClearAbove should clear past length only.");

            set.None();
            Assert.AreEqual(0, set.Count, "None should clear");

            // Verify individual set and get
            byte[] values = new byte[999];
            for (int i = 0; i < 999; ++i)
            {
                // Set only 'i' via setter and verify
                set.None();
                set[i] = true;
                AssertOnly(set, 999, i);
            }
        }

        [TestMethod]
        public void BitVector_Page()
        {
            BitVector set = new BitVector(900);
            int[] page = new int[10];
            int index;

            // Verify if nothing is set, page doesn't find anything and returns -1
            set.None();

            index = 0;
            int count = set.Page(page, ref index);
            Assert.AreEqual(-1, index);
            Assert.AreEqual(0, count);

            // Set 15 values (every 3rd under 45)
            for (int i = 0; i < 45; i += 3)
            {
                set[i] = true;
            }

            // Verify a full page of results is returned with the correct next index to check
            index = 0;
            count = set.Page(page, ref index);
            Assert.AreEqual(28, index);
            Assert.AreEqual(10, count);
            Assert.AreEqual("0, 3, 6, 9, 12, 15, 18, 21, 24, 27", Join(page, count));

            // Verify the second page is returned with -1 and the last five values
            count = set.Page(page, ref index);
            Assert.AreEqual(-1, index);
            Assert.AreEqual(5, count);
            Assert.AreEqual("30, 33, 36, 39, 42", Join(page, count));
        }

        private static void AssertOnly(BitVector set, int limit, int expected)
        {
            Assert.IsTrue(set[expected]);
            Assert.AreEqual(1, set.Count);

            for (int j = 0; j < limit; ++j)
            {
                Assert.AreEqual(j == expected, set[j]);
            }
        }

        private static string Join(int[] values, int length)
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < length; ++i)
            {
                if (i > 0) result.Append(", ");
                result.Append(values[i]);
            }

            return result.ToString();
        }
    }
}
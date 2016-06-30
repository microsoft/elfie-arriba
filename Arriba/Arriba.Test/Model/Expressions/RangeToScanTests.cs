// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Model.Expressions;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test
{
    [TestClass]
    public class RangeToScanTests
    {
        [TestMethod]
        public void RangeToScan_Build_Empty()
        {
            foreach (Operator op in Enum.GetValues(typeof(Operator)))
            {
                Assert.AreEqual("{EMPTY}", Build(op, -1, -1, 0).ToString());
            }
        }

        [TestMethod]
        public void RangeToScan_Build_SingleItem()
        {
            // Equals for below only value
            Assert.AreEqual("{EMPTY}", Build(Operator.Equals, -1, -1, 1).ToString());

            // Equals for only value
            Assert.AreEqual("{ALL}", Build(Operator.Equals, 0, 0, 1).ToString());

            // Equals for above only value
            Assert.AreEqual("{EMPTY}", Build(Operator.Equals, -2, -2, 1).ToString());


            // Not Equals for below only value
            Assert.AreEqual("{ALL}", Build(Operator.NotEquals, -1, -1, 1).ToString());

            // Not Equals for only value
            Assert.AreEqual("{EMPTY}", Build(Operator.NotEquals, 0, 0, 1).ToString());

            // Not Equals for above only value
            Assert.AreEqual("{ALL}", Build(Operator.NotEquals, -2, -2, 1).ToString());


            // Less than below only value
            Assert.AreEqual("{EMPTY}", Build(Operator.LessThan, -1, -1, 1).ToString());

            // Less than only value
            Assert.AreEqual("{EMPTY}", Build(Operator.LessThan, 0, 0, 1).ToString());

            // Less than above only value
            Assert.AreEqual("{ALL}", Build(Operator.LessThan, -2, -2, 1).ToString());


            // Less than or equal to below only value
            Assert.AreEqual("{EMPTY}", Build(Operator.LessThanOrEqual, -1, -1, 1).ToString());

            // Less than or equal to only value
            Assert.AreEqual("{ALL}", Build(Operator.LessThanOrEqual, 0, 0, 1).ToString());

            // Less than or equal above only value
            Assert.AreEqual("{ALL}", Build(Operator.LessThanOrEqual, -2, -2, 1).ToString());


            // Greater than for value below all values
            Assert.AreEqual("{ALL}", Build(Operator.GreaterThan, -1, -1, 20).ToString());

            // Greater than for value below only value
            Assert.AreEqual("{EMPTY}", Build(Operator.GreaterThan, 0, 0, 1).ToString());

            // Greater than for value above only value
            Assert.AreEqual("{EMPTY}", Build(Operator.GreaterThan, -2, -2, 1).ToString());


            // Greater than or equal for value below only value
            Assert.AreEqual("{ALL}", Build(Operator.GreaterThanOrEqual, -1, -1, 1).ToString());

            // Greater than or equal to only value
            Assert.AreEqual("{ALL}", Build(Operator.GreaterThanOrEqual, 0, 0, 1).ToString());

            // Greater than or equal to above only value
            Assert.AreEqual("{EMPTY}", Build(Operator.GreaterThanOrEqual, -2, -2, 1).ToString());
        }

        [TestMethod]
        public void RangeToScan_Build()
        {
            // Equals for below all values
            Assert.AreEqual("{EMPTY}", Build(Operator.Equals, -1, -1, 20).ToString());

            // Equals for above all values
            Assert.AreEqual("{EMPTY}", Build(Operator.Equals, -21, -21, 20).ToString());

            // Equals for whole range
            Assert.AreEqual("{ALL}", Build(Operator.Equals, 0, 19, 20).ToString());

            // Equals for small subset
            Assert.AreEqual("5=> <=8", Build(Operator.Equals, 5, 8, 20).ToString());

            // Equals for large subset
            Assert.AreEqual("! <=5 16=>", Build(Operator.Equals, 5, 16, 20).ToString());

            // Matches fall through to equals
            Assert.AreEqual("! <=5 16=>", Build(Operator.Matches, 5, 16, 20).ToString());

            // MatchesExact fall through to equals
            Assert.AreEqual("! <=5 16=>", Build(Operator.MatchesExact, 5, 16, 20).ToString());


            // Not Equals for below all values in set
            Assert.AreEqual("{ALL}", Build(Operator.NotEquals, -1, -1, 20).ToString());

            // Not Equals for above all values in set
            Assert.AreEqual("{ALL}", Build(Operator.NotEquals, -21, -21, 20).ToString());

            // Not Equals for whole set
            Assert.AreEqual("{EMPTY}", Build(Operator.NotEquals, 0, 19, 20).ToString());

            // Not Equals for large subset
            Assert.AreEqual("17=> <=19", Build(Operator.NotEquals, 0, 16, 20).ToString());

            // Not Equals for disjoint sets
            Assert.AreEqual("<=3 16=>", Build(Operator.NotEquals, 3, 16, 20).ToString());

            // Not Equals for small subset
            Assert.AreEqual("! 5=> <=8", Build(Operator.NotEquals, 5, 8, 20).ToString());


            // Less than for value below all values
            Assert.AreEqual("{EMPTY}", Build(Operator.LessThan, -1, -1, 20).ToString());

            // Less than for value above all values
            Assert.AreEqual("{ALL}", Build(Operator.LessThan, -21, -21, 20).ToString());

            // Less than for value between values (SortedIDs[3] would be insertion point, so [0-2] are less than value and [3] is greater.
            Assert.AreEqual("0=> <=2", Build(Operator.LessThan, -4, -4, 20).ToString());

            // Less than or equal for value equal to the smallest
            Assert.AreEqual("{EMPTY}", Build(Operator.LessThan, 0, 0, 20).ToString());

            // Less than or equal for value equal to the largest
            Assert.AreEqual("! 18=> <=19", Build(Operator.LessThan, 18, 19, 20).ToString());

            // Less than for a small range
            Assert.AreEqual("0=> <=2", Build(Operator.LessThan, 3, 5, 20).ToString());


            // Less than or equal for value below all values
            Assert.AreEqual("{EMPTY}", Build(Operator.LessThanOrEqual, -1, -1, 20).ToString());

            // Less than or equal for value above all values
            Assert.AreEqual("{ALL}", Build(Operator.LessThanOrEqual, -21, -21, 20).ToString());

            // Less than or equal for value between values (SortedIDs[3] would be insertion point, so [0-2] are Less than or equal value and [3] is greater.
            Assert.AreEqual("0=> <=2", Build(Operator.LessThanOrEqual, -4, -4, 20).ToString());

            // Less than or equal for value equal to the smallest
            Assert.AreEqual("0=> <=0", Build(Operator.LessThanOrEqual, 0, 0, 20).ToString());

            // Less than or equal for value equal to the largest
            Assert.AreEqual("{ALL}", Build(Operator.LessThanOrEqual, 18, 19, 20).ToString());

            // Less than or equal for a small range
            Assert.AreEqual("0=> <=5", Build(Operator.LessThanOrEqual, 3, 5, 20).ToString());

            // Less than or equal for a large range
            Assert.AreEqual("! 17=> <=19", Build(Operator.LessThanOrEqual, 15, 16, 20).ToString());


            // Greater than for value below all values
            Assert.AreEqual("{ALL}", Build(Operator.GreaterThan, -1, -1, 20).ToString());

            // Greater than for value above all values
            Assert.AreEqual("{EMPTY}", Build(Operator.GreaterThan, -21, -21, 20).ToString());

            // Greater than for value between values (SortedIDs[3] would be insertion point, so [0-2] are Greater than value and [3] is greater.
            Assert.AreEqual("! 0=> <=2", Build(Operator.GreaterThan, -4, -4, 20).ToString());

            // Greater than for a value equal to the smallest
            Assert.AreEqual("! 0=> <=0", Build(Operator.GreaterThan, 0, 0, 20).ToString());

            // Greater than for a value equal to the largest
            Assert.AreEqual("{EMPTY}", Build(Operator.GreaterThan, 18, 19, 20).ToString());

            // Greater than for a small range
            Assert.AreEqual("17=> <=19", Build(Operator.GreaterThan, 15, 16, 20).ToString());

            // Greater than for a large range
            Assert.AreEqual("! 0=> <=5", Build(Operator.GreaterThan, 3, 5, 20).ToString());


            // Greater than or equal for value below all values
            Assert.AreEqual("{ALL}", Build(Operator.GreaterThanOrEqual, -1, -1, 20).ToString());

            // Greater than or equal for value above all values
            Assert.AreEqual("{EMPTY}", Build(Operator.GreaterThanOrEqual, -21, -21, 20).ToString());

            // Greater than or equal for value between values (SortedIDs[3] would be insertion point, so [0-2] are Greater than or equal value and [3] is greater.
            Assert.AreEqual("! 0=> <=2", Build(Operator.GreaterThanOrEqual, -4, -4, 20).ToString());

            // Greater than or equal for a value equal to the smallest
            Assert.AreEqual("{ALL}", Build(Operator.GreaterThanOrEqual, 0, 0, 20).ToString());

            // Greater than or equal for a value equal to the largest
            Assert.AreEqual("18=> <=19", Build(Operator.GreaterThanOrEqual, 18, 19, 20).ToString());

            // Greater than or equal for a small range
            Assert.AreEqual("15=> <=19", Build(Operator.GreaterThanOrEqual, 15, 16, 20).ToString());

            // Greater than or equal for a large range
            Assert.AreEqual("! 0=> <=2", Build(Operator.GreaterThanOrEqual, 3, 5, 20).ToString());
        }

        [TestMethod]
        public void RangeToScan_AddMatches()
        {
            ushort[] sortedIDs = GetSampleSortedIDs(20);

            // Verify AddMatches for a small contiguous range (no previous values)
            Assert.AreEqual("0, 1, 2, 3, 4, 5", AddMatches(Build(Operator.LessThanOrEqual, 3, 5, 20), sortedIDs, null));

            // Verify AddMatches for a small contiguous range (with previous values)
            Assert.AreEqual("0, 1, 2, 3, 4, 5, 6, 7", AddMatches(Build(Operator.LessThanOrEqual, 3, 5, 20), sortedIDs, new ushort[] { 4, 5, 6, 7 }));

            // Verify AddMatches for a small non-contiguous range (no previous values)
            Assert.AreEqual("0, 1, 2, 17, 18, 19", AddMatches(Build(Operator.NotEquals, 3, 16, 20), sortedIDs, null));

            // Verify AddMatches for a small non-contiguous range (with previous values)
            Assert.AreEqual("0, 1, 2, 3, 16, 17, 18, 19", AddMatches(Build(Operator.NotEquals, 3, 16, 20), sortedIDs, new ushort[] { 2, 3, 16, 17 }));

            // Verify AddMatches for a large negated range (no previous values)
            Assert.AreEqual("6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19", AddMatches(Build(Operator.GreaterThan, 3, 5, 20), sortedIDs, null));

            // Verify AddMatches for a large negated range (with previous values)
            // [Bug Verification: Range didn't use a separate ResultSet to ensure negation didn't mess up previously set values]
            Assert.AreEqual("4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19", AddMatches(Build(Operator.GreaterThan, 3, 5, 20), sortedIDs, new ushort[] { 4, 5, 6, 7 }));

            // Verify AddMatches for a small non-contiguous negated range (no previous values)
            Assert.AreEqual("3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16", AddMatches(Build(Operator.Equals, 3, 16, 20), sortedIDs, null));

            // Verify AddMatches for a small non-contiguous negated range (with previous values)
            Assert.AreEqual("2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17", AddMatches(Build(Operator.Equals, 3, 16, 20), sortedIDs, new ushort[] { 2, 3, 16, 17 }));
        }

        private static RangeToScan Build(Operator op, int firstSortedIndexWithValue, int lastSortedIndexWithValue, int count)
        {
            RangeToScan r = new RangeToScan();
            RangeToScan.TryBuild(op, firstSortedIndexWithValue, lastSortedIndexWithValue, count, ref r);
            return r;
        }

        private static string AddMatches(RangeToScan range, ushort[] sortedIDs, ushort[] previousValues)
        {
            ShortSet resultSet = new ShortSet((ushort)range.Count);

            // If previous values are specified, add them
            if (previousValues != null)
            {
                for (int i = 0; i < previousValues.Length; ++i)
                {
                    resultSet.Add(previousValues[i]);
                }
            }

            // Add matches for the range
            range.AddMatches(sortedIDs, resultSet);

            // Return the result as a string
            return String.Join(", ", resultSet.Values);
        }

        private static ushort[] GetSampleSortedIDs(ushort length)
        {
            ushort[] sortedIDs = new ushort[length];
            for (int i = 0; i < length; ++i)
            {
                sortedIDs[i] = (ushort)(i);
            }

            return sortedIDs;
        }
    }
}

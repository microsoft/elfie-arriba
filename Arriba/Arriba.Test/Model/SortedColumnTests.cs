// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Arriba.Extensions;
using Arriba.Model;
using Arriba.Model.Expressions;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class SortedColumnTests
    {
        [TestMethod]
        public void SortedColumn_Basic()
        {
            ColumnTests.ColumnTest_Basics(() => ColumnFactory.CreateSortedColumn<int>(new ValueTypeColumn<int>(-1), 0), -10, 20);
            ColumnTests.ColumnTest_Basics(() => ColumnFactory.CreateSortedColumn<ByteBlock>(new ByteBlockColumn(ByteBlock.Zero), 0), "One", "Two");
            ColumnTests.ColumnTest_Basics(() => ColumnFactory.CreateSortedColumn<TimeSpan>(new ValueTypeColumn<TimeSpan>(default(TimeSpan)), 0), TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));

            ColumnTests.ColumnTest_Basics(() => ColumnFactory.CreateSortedColumn<int>(new ValueTypeColumn<int>(-1), ArrayExtensions.MinimumSize + 1), -10, 20);
            ColumnTests.ColumnTest_Basics(() => ColumnFactory.CreateSortedColumn<ByteBlock>(new ByteBlockColumn(ByteBlock.Zero), ArrayExtensions.MinimumSize + 1), "One", "Two");
            ColumnTests.ColumnTest_Basics(() => ColumnFactory.CreateSortedColumn<TimeSpan>(new ValueTypeColumn<TimeSpan>(default(TimeSpan)), ArrayExtensions.MinimumSize + 1), TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));

            ColumnTests.ColumnTest_Basics(() => ColumnFactory.CreateSortedColumn<int>(new ValueTypeColumn<int>(-1), ushort.MaxValue), -10, 20);
            ColumnTests.ColumnTest_Basics(() => ColumnFactory.CreateSortedColumn<ByteBlock>(new ByteBlockColumn(ByteBlock.Zero), ushort.MaxValue), "One", "Two");
            ColumnTests.ColumnTest_Basics(() => ColumnFactory.CreateSortedColumn<TimeSpan>(new ValueTypeColumn<TimeSpan>(default(TimeSpan)), ushort.MaxValue), TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));
        }

        [TestMethod]
        public void SortedColumn_Where()
        {
            SortedColumn<int> c = ColumnFactory.CreateSortedColumn<int>(new ValueTypeColumn<int>(-1), 0);

            c.SetSize(8);
            c[0] = 0;
            c[1] = 1;
            c[2] = 1;
            c[3] = 2;
            c[4] = 2;
            c[5] = 8;
            c[6] = 8;
            c[7] = 9;
            ColumnTests.AssertConsistent(c);

            // Find a middle value
            Assert.AreEqual("3, 4", ColumnTests.GetMatches(c, Operator.Equals, 2));

            // Find the minimum value
            Assert.AreEqual("0", ColumnTests.GetMatches(c, Operator.Equals, 0));

            // Find the maximum value
            Assert.AreEqual("7", ColumnTests.GetMatches(c, Operator.Equals, 9));

            // Find a value not in set (too small)
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.Equals, -1));

            // Find a value not in set (too big)
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.Equals, 10));


            // Find not of a value
            Assert.AreEqual("0, 1, 2, 5, 6, 7", ColumnTests.GetMatches(c, Operator.NotEquals, 2));

            // Find not of a value not in set
            Assert.AreEqual("0, 1, 2, 3, 4, 5, 6, 7", ColumnTests.GetMatches(c, Operator.NotEquals, -1));


            // Find less than not in set (too small)
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.LessThan, -1));

            // Find less than an existing value
            Assert.AreEqual("0, 1, 2", ColumnTests.GetMatches(c, Operator.LessThan, 2));

            // Find less than a gap between values
            Assert.AreEqual("0, 1, 2, 3, 4", ColumnTests.GetMatches(c, Operator.LessThan, 4));

            // Find less than more than set (too big)
            Assert.AreEqual("0, 1, 2, 3, 4, 5, 6, 7", ColumnTests.GetMatches(c, Operator.LessThan, 99));


            // Find less than or equal not in set (too small)
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.LessThanOrEqual, -1));

            // Find less than or equal an existing value
            Assert.AreEqual("0, 1, 2, 3, 4", ColumnTests.GetMatches(c, Operator.LessThanOrEqual, 2));

            // Find less than or equal a gap between values
            Assert.AreEqual("0, 1, 2, 3, 4", ColumnTests.GetMatches(c, Operator.LessThanOrEqual, 4));

            // Find less than or equal more than set (too big)
            Assert.AreEqual("0, 1, 2, 3, 4, 5, 6, 7", ColumnTests.GetMatches(c, Operator.LessThanOrEqual, 99));


            // Find greater than not in set (too small)
            Assert.AreEqual("0, 1, 2, 3, 4, 5, 6, 7", ColumnTests.GetMatches(c, Operator.GreaterThan, -1));

            // Find greater than an existing value
            Assert.AreEqual("5, 6, 7", ColumnTests.GetMatches(c, Operator.GreaterThan, 2));

            // Find greater than a gap between values
            Assert.AreEqual("5, 6, 7", ColumnTests.GetMatches(c, Operator.GreaterThan, 4));

            // Find greater than more than set (too big)
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.GreaterThan, 99));

            // Find greater than or equal not in set (too small)
            Assert.AreEqual("0, 1, 2, 3, 4, 5, 6, 7", ColumnTests.GetMatches(c, Operator.GreaterThanOrEqual, -1));

            // Find greater than or equal an existing value
            Assert.AreEqual("3, 4, 5, 6, 7", ColumnTests.GetMatches(c, Operator.GreaterThanOrEqual, 2));

            // Find greater than or equal a gap between values
            Assert.AreEqual("5, 6, 7", ColumnTests.GetMatches(c, Operator.GreaterThanOrEqual, 4));

            // Find greater than or equal more than set (too big)
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.GreaterThanOrEqual, 99));

            // Verify sorting consistency problems are detected
            ((IColumn<int>)c.InnerColumn)[0] = 8;
            ExecutionDetails d = new ExecutionDetails();
            c.VerifyConsistency(VerificationLevel.Full, d);
            Assert.IsFalse(d.Succeeded);
        }

        [TestMethod]
        public void SortedColumn_Consistency()
        {
            SortedColumn<int> c = ColumnFactory.CreateSortedColumn<int>(new ValueTypeColumn<int>(-1), 0);

            c.SetSize(2);
            c[0] = 1;
            c[1] = 2;
            ColumnTests.AssertConsistent(c);
            c.SetSize(4);
            c[2] = -5;
            c[3] = -4;
            ColumnTests.AssertConsistent(c);
            c.SetSize(6);
            c[4] = 1;
            c[5] = 0;
            ColumnTests.AssertConsistent(c);
        }

        [TestMethod]
        public void SortedColumn_IndexOf()
        {
            SortedColumn<int> c = ColumnFactory.CreateSortedColumn<int>(new ValueTypeColumn<int>(-1), 0);

            c.SetSize(8);
            c[0] = 0;
            c[1] = 1;
            c[2] = 1;
            c[3] = 2;
            c[4] = 2;
            c[5] = 8;
            c[6] = 8;
            c[7] = 9;

            ColumnTests.AssertConsistent(c);
            Assert.AreEqual(ushort.MaxValue, ColumnTests.GetIndex(c, 10));
            Assert.AreEqual(1, ColumnTests.GetIndex(c, 1));
            Assert.AreEqual(3, ColumnTests.GetIndex(c, 2));
            Assert.AreEqual(7, ColumnTests.GetIndex(c, 9));
        }

        [TestMethod]
        public void SortedColumn_InsertionOrders()
        {
            SortedColumn<int> c = ColumnFactory.CreateSortedColumn<int>(new ValueTypeColumn<int>(-1), 0);

            c.SetSize(8);

            // Value greater than default
            c[0] = 10;

            // Value less than default
            c[1] = -10;

            // Insert between existing value and end of defaults
            c[2] = 5;

            // Insert between existing value and beginning of defaults
            c[3] = -2;

            // Insert at end
            c[4] = 20;

            // Insert at beginning
            c[5] = -20;

            // Insert in middle
            c[6] = 8;

            // Leave with default
            //c[7] = -1;

            ColumnTests.AssertConsistent(c);
            Assert.AreEqual("5, 1, 3, 7, 2, 6, 0, 4", ColumnTests.GetSortedIndexes(c));
        }

        [TestMethod]
        public void SortedColumn_Strings()
        {
            SortedColumn<ByteBlock> c = ColumnFactory.CreateSortedColumn<ByteBlock>(new ByteBlockColumn(ByteBlock.Zero), 0);

            // Set all three to default (empty)
            c.SetSize(3);
            CommitIfRequired(c);

            // Reset a value to empty (SortedIDs need to stay in order to find insertion positions properly)
            c[2] = "";
            Assert.AreEqual("0, 1, 2", ColumnTests.GetSortedIndexes(c));

            // Reset a value to something (needs to be after all other values; in this case, it'll stay in place in SortedIDs)
            c[2] = "TaoSuiteSrc";
            Assert.AreEqual("0, 1, 2", ColumnTests.GetSortedIndexes(c));

            // Add two more values (empty); empty values should insert after other empty values
            c.SetSize(5);
            CommitIfRequired(c);
            Assert.AreEqual("0, 1, 3, 4, 2", ColumnTests.GetSortedIndexes(c));

            // Set a larger ID to an existing value; needs SortedID after other copy of value
            c[3] = "TaoSuiteSrc";
            Assert.AreEqual("0, 1, 4, 2, 3", ColumnTests.GetSortedIndexes(c));

            // Set another value before existing ones; needs SortedID in value order
            c[0] = "MauiRemoval";
            Assert.AreEqual("1, 4, 0, 2, 3", ColumnTests.GetSortedIndexes(c));

            // Set a smaller ID to an existing value; needs SortedID before other copies of value
            c[1] = "TaoSuiteSrc";
            Assert.AreEqual("4, 0, 1, 2, 3", ColumnTests.GetSortedIndexes(c));

            // Add two more values; should appear after other empties
            c.SetSize(7);
            CommitIfRequired(c);
            Assert.AreEqual("4, 5, 6, 0, 1, 2, 3", ColumnTests.GetSortedIndexes(c));

            // Set two more empty duplicates - they need to be reinserted among empties in original sequence
            c[5] = "";
            c[6] = "";
            Assert.AreEqual("4, 5, 6, 0, 1, 2, 3", ColumnTests.GetSortedIndexes(c));

            ColumnTests.AssertConsistent(c);
        }

        [TestMethod]
        public void SortedColumn_StartsWith_NonString()
        {
            ExecutionDetails details = new ExecutionDetails();
            SortedColumn<int> column = ColumnFactory.CreateSortedColumn<int>(new ValueTypeColumn<int>(-1), 0);

            column.TryWhere(Operator.StartsWith, 5, new ShortSet(ushort.MaxValue), details);
            Assert.IsFalse(details.Succeeded);
        }

        [TestMethod]
        public void SortedColumn_StartsWith()
        {
            SortedColumn<ByteBlock> column = ColumnFactory.CreateSortedColumn<ByteBlock>(new ByteBlockColumn(ByteBlock.Zero), 0);
            column.SetSize(6);

            column[0] = "Active";
            column[1] = "Activate";
            column[2] = "Activity";
            column[3] = "Resolved";
            column[4] = "Resolution";
            column[5] = "State";
            CommitIfRequired(column);

            Assert.AreEqual("0, 1, 2", ColumnTests.GetMatches(column, Operator.StartsWith, "Activ"));
            Assert.AreEqual("1", ColumnTests.GetMatches(column, Operator.StartsWith, "Activa"));
            Assert.AreEqual("", ColumnTests.GetMatches(column, Operator.StartsWith, "Balmy"));
            Assert.AreEqual("3, 4", ColumnTests.GetMatches(column, Operator.StartsWith, "Reso"));
            Assert.AreEqual("3", ColumnTests.GetMatches(column, Operator.StartsWith, "Resolv"));
            Assert.AreEqual("5", ColumnTests.GetMatches(column, Operator.StartsWith, "St"));
        }

        [TestMethod]
        public void SortedColumn_GrowAndShrink()
        {
            SortedColumn<int> c = ColumnFactory.CreateSortedColumn<int>(new ValueTypeColumn<int>(-1), 0);

            c.SetSize(5);
            CommitIfRequired(c);
            ColumnTests.AssertConsistent(c);

            c[0] = 10;
            c[1] = -10;
            c[2] = 5;
            c[3] = -2;
            c[4] = 20;
            CommitIfRequired(c);
            ColumnTests.AssertConsistent(c);

            Assert.AreEqual("1, 3, 2, 0, 4", ColumnTests.GetSortedIndexes(c));

            // Grow - verify IDs inserted for -1 default
            c.SetSize(8);
            CommitIfRequired(c);
            ColumnTests.AssertConsistent(c);
            Assert.AreEqual("1, 3, 5, 6, 7, 2, 0, 4", ColumnTests.GetSortedIndexes(c));

            // Shrink (unset values) - verify IDs removed properly
            c.SetSize(5);
            ColumnTests.AssertConsistent(c);
            Assert.AreEqual("1, 3, 2, 0, 4", ColumnTests.GetSortedIndexes(c));

            // Shrink (set values) - verify IDs removed properly
            c.SetSize(3);
            ColumnTests.AssertConsistent(c);
            Assert.AreEqual("1, 2, 0", ColumnTests.GetSortedIndexes(c));

            // Shrink (everything)
            c.SetSize(0);
            ColumnTests.AssertConsistent(c);
            Assert.AreEqual("", ColumnTests.GetSortedIndexes(c));
            Assert.AreEqual(0, (int)c.Count);

            // Grow enough to require underlying array resize
            IList<ushort> actualArray;
            int actualArrayCount;
            c.SetSize(ArrayExtensions.MinimumSize * 2);
            CommitIfRequired(c);
            ColumnTests.AssertConsistent(c);
            Assert.IsTrue(c.TryGetSortedIndexes(out actualArray, out actualArrayCount));
            Assert.AreEqual(ArrayExtensions.MinimumSize * 2, (int)c.Count);
            Assert.AreEqual(ArrayExtensions.MinimumSize * 2, (int)actualArray.Count);
            Assert.AreEqual(ArrayExtensions.MinimumSize * 2, actualArrayCount);

            // Shrink enough to require underlying array resize
            c.SetSize(ArrayExtensions.MinimumSize);
            ColumnTests.AssertConsistent(c);
            Assert.IsTrue(c.TryGetSortedIndexes(out actualArray, out actualArrayCount));
            Assert.AreEqual(ArrayExtensions.MinimumSize, (int)c.Count);
            Assert.AreEqual(ArrayExtensions.MinimumSize, (int)actualArray.Count);
            Assert.AreEqual(ArrayExtensions.MinimumSize, actualArrayCount);
        }

        [TestMethod]
        public void SortedColumn_SortedIndexesSize()
        {
            SortedColumn<int> c = ColumnFactory.CreateSortedColumn<int>(new ValueTypeColumn<int>(-1), 0);

            // preload 1 more than minimum size which will force a resize bigger
            // resize policy will never allow resizing by just 1 element so this 
            // should ensure that realSize and actualSize are not the same
            ushort insertSize = ArrayExtensions.MinimumSize + 1;

            c.SetSize(insertSize);
            for (ushort i = 0; i < insertSize; ++i)
            {
                c[i] = i;
            }
            CommitIfRequired(c);

            // check the size
            IList<ushort> sortedIndexes;
            int sortedIndexesCount;
            Assert.IsTrue(c.TryGetSortedIndexes(out sortedIndexes, out sortedIndexesCount));

            int realSize = sortedIndexes.Count;

            Assert.AreNotEqual(insertSize, realSize);
            Assert.AreEqual(insertSize, sortedIndexesCount);
        }
        [TestMethod]
        public void FastAddSortedColumn_InsertMax()
        {
            SortedColumn<int> c = ColumnFactory.CreateSortedColumn<int>(new ValueTypeColumn<int>(-1), ushort.MaxValue);

            ushort seedLimit = ushort.MaxValue / 2;

            c.SetSize(seedLimit);
            for (ushort i = 0; i < seedLimit; ++i)
            {
                c[i] = ushort.MaxValue + (int)i * 10;
            }
            ColumnTests.AssertConsistent(c);

            c.SetSize(ushort.MaxValue);
            for (ushort i = seedLimit; i < ushort.MaxValue; ++i)
            {
                c[i] = (int)i * 10;
            }
            ColumnTests.AssertConsistent(c);

            c.SetSize(0);
            ColumnTests.AssertConsistent(c);

            seedLimit = (ushort.MaxValue / 2) - 1;

            c.SetSize(seedLimit);
            for (ushort i = 0; i < seedLimit; ++i)
            {
                c[i] = ushort.MaxValue + (int)i * 10;
            }
            ColumnTests.AssertConsistent(c);

            c.SetSize(ushort.MaxValue);
            for (ushort i = seedLimit; i < ushort.MaxValue; ++i)
            {
                c[i] = (int)i * 10;
            }
            ColumnTests.AssertConsistent(c);

            c.SetSize(0);
            ColumnTests.AssertConsistent(c);

            seedLimit = (ushort.MaxValue / 2) + 1;

            c.SetSize(seedLimit);
            for (ushort i = 0; i < seedLimit; ++i)
            {
                c[i] = ushort.MaxValue + (int)i * 10;
            }
            ColumnTests.AssertConsistent(c);

            c.SetSize(ushort.MaxValue);
            for (ushort i = seedLimit; i < ushort.MaxValue; ++i)
            {
                c[i] = (int)i * 10;
            }
            ColumnTests.AssertConsistent(c);
        }

        [TestMethod]
        public void FastAddSortedColumn_InsertMax2()
        {
            SortedColumn<int> c = ColumnFactory.CreateSortedColumn<int>(new ValueTypeColumn<int>(-1), ushort.MaxValue);

            ushort seedLimit = ushort.MaxValue - 1;
            int lastValue = 0;

            c.SetSize(seedLimit);
            for (ushort i = 0; i < seedLimit; ++i)
            {
                lastValue = ushort.MaxValue + (int)i * 10;
                c[i] = lastValue;
            }
            ColumnTests.AssertConsistent(c);

            int firstValue = -1;
            c.SetSize(ushort.MaxValue);
            for (ushort i = seedLimit; i < ushort.MaxValue; ++i)
            {
                if (firstValue == -1) firstValue = (int)i;

                c[i] = (int)i;
            }
            ColumnTests.AssertConsistent(c);

            Assert.AreEqual("65534", ColumnTests.GetMatches(c, Operator.Equals, firstValue));
            Assert.AreEqual("65533", ColumnTests.GetMatches(c, Operator.Equals, lastValue));

            IList<ushort> sortedIndexes;
            int sortedIndexesCount;
            Assert.IsTrue(c.TryGetSortedIndexes(out sortedIndexes, out sortedIndexesCount));

            // Check sort order - lowest one was the last value added
            Assert.AreEqual("65534", sortedIndexes.First().ToString());
            Assert.AreEqual("65533", sortedIndexes.Last().ToString());
        }

        private static void CommitIfRequired(object o)
        {
            if (o is ICommittable)
            {
                (o as ICommittable).Commit();
            }
        }
    }
}

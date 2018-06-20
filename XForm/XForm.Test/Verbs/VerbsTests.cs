// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XForm.Aggregators;
using XForm.Data;
using XForm.Extensions;
using XForm.Verbs;

namespace XForm.Test.Query
{
    [TestClass]
    public class VerbsTests
    {
        [TestMethod]
        public void Verb_Join()
        {
            String8Block block = new String8Block();

            // Build 99-0 descending
            int[] joinTo = Enumerable.Range(0, 100).Reverse().ToArray();

            // Build an array with some values in range and some outside
            int[] joinFrom = new int[] { 11, 15, 9, 8, 1000, 9, 0, 99, -1000 };

            // Expect the in range rows to match and out-of-range ones to be omitted (inner join)
            int[] expected = new int[] { 11, 15, 9, 8, 9, 0, 99 };

            // Run via integer
            RunJoinAndVerify(joinTo, joinFrom, expected);

            // Run as String8
            RunJoinAndVerify(
                joinTo.Select((i) => block.GetCopy(i.ToString())).ToArray(),
                joinFrom.Select((i) => block.GetCopy(i.ToString())).ToArray(),
                expected.Select((i) => block.GetCopy(i.ToString())).ToArray());
        }

        private static void RunJoinAndVerify(Array joinTo, Array joinFrom, Array expected)
        {
            Type t = joinTo.GetType().GetElementType();

            // Build a table with padded nulls to join from (so we see nulls are also filtered out)
            XArray joinFromxarray = TableTestHarness.Nulls(XArray.All(joinFrom));
            IXTable joinFromTable = TableTestHarness.DatabaseContext.FromArrays(joinFromxarray.Count)
                .WithColumn(new ColumnDetails("ServerID", t), joinFromxarray);

            // Build the table to join to
            IXTable joinToTable = TableTestHarness.DatabaseContext.FromArrays(joinTo.Length)
               .WithColumn(new ColumnDetails("ID", t), XArray.All(joinTo));

            // Run the join - verify the expected values without padding are found
            IXTable result = new Join(joinFromTable, "ServerID", joinToTable, "ID", "Server.");
            Func<XArray> serverID = result.Columns.Find("Server.ID").CurrentGetter();

            IXTable expectedTable = TableTestHarness.DatabaseContext.FromArrays(expected.Length).WithColumn("Server.ID", expected);
            TableTestHarness.AssertAreEqual(expectedTable, result, 2);
        }

        [TestMethod]
        public void Verb_Choose()
        {
            XDatabaseContext context = new XDatabaseContext();
            int[] rankPattern = new int[] { 2, 3, 1 };

            // Build three arrays
            int distinctCount = 100;
            int length = 3 * distinctCount;
            int[] id = new int[length];
            int[] rank = new int[length];
            int[] value = new int[length];

            for (int i = 0; i < length; ++i)
            {
                // ID is the same for three rows at a time
                id[i] = i / 3;

                // Rank is [2, 3, 1] repeating (so the middle is the biggest)
                rank[i] = rankPattern[i % 3];

                // Value is the index of the real row
                value[i] = i;
            }

            // Build the expected results - one for each distinct ID, each with max rank and from the right row
            int[] expectedIds = new int[distinctCount];
            int[] expectedRanks = new int[distinctCount];
            int[] expectedValues = new int[distinctCount];

            for (int i = 0; i < distinctCount; ++i)
            {
                expectedIds[i] = i;
                expectedRanks[i] = 3;
                expectedValues[i] = 3 * i + 1;
            }

            IXTable expected = TableTestHarness.DatabaseContext.FromArrays(distinctCount)
                .WithColumn("ID", expectedIds)
                .WithColumn("Rank", expectedRanks)
                .WithColumn("Value", expectedValues);

            IXTable actual = TableTestHarness.DatabaseContext.FromArrays(length)
                .WithColumn("ID", id)
                .WithColumn("Rank", rank)
                .WithColumn("Value", value);

            // Run and compare (as integer)
            TableTestHarness.AssertAreEqual(expected, actual.Query("choose Max [Rank] [ID]", context), distinctCount / 3);

            // Run and compare (as String8)
            TableTestHarness.AssertAreEqual(
                expected.Query("select Cast([ID], String8), Cast([Rank], String8), Cast([Value], String8)", context),
                actual.Query("select Cast([ID], String8), Cast([Rank], String8), Cast([Value], String8)", context).Query("choose Max [Rank] [ID]", context),
                distinctCount);
        }

        [TestMethod]
        public void Verb_Schema()
        {
            int[] id = new int[0];
            ushort[] size = new ushort[0];

            string[] name = new string[] { "ID", "Size" };
            string[] types = new string[] { "Int32", "UInt16" };

            IXTable actual = TableTestHarness.DatabaseContext.FromArrays(0)
                .WithColumn("ID", id)
                .WithColumn("Size", size)
                .Query("schema", TableTestHarness.DatabaseContext);

            IXTable expected = TableTestHarness.DatabaseContext.FromArrays(2)
                .WithColumn("Name", name)
                .WithColumn("Type", types);

            TableTestHarness.AssertAreEqual(expected, actual, 2);
        }

        private string ToPercent(double value)
        {
            return value.ToString("P0");
        }

        [TestMethod]
        public void Verb_Peek()
        {
            String8Block block = new String8Block();
            int[] values = BuildPeekSample();

            IXTable expected = TableTestHarness.DatabaseContext.FromArrays(3)
                .WithColumn("Value", new int[] { 0, 1, 2 })
                .WithColumn("Count", new int[] { 500, 250, 150 })
                .WithColumn("Percentage", TableTestHarness.ToString8(new string[]
                {
                    PercentageAggregator.TwoSigFigs(500, 1000),
                    PercentageAggregator.TwoSigFigs(250, 1000),
                    PercentageAggregator.TwoSigFigs(150, 1000)
                }));

            IXTable actual = TableTestHarness.DatabaseContext.FromArrays(values.Length)
                .WithColumn("Value", values)
                .Query("peek [Value]", TableTestHarness.DatabaseContext);

            TableTestHarness.AssertAreEqual(expected, actual, 2);
        }

        [TestMethod]
        public void Verb_GroupBy()
        {
            String8Block block = new String8Block();
            int[] values = BuildPeekSample();

            IXTable expected = TableTestHarness.DatabaseContext.FromArrays(3)
                .WithColumn("Value", new int[] { 0, 1, 2 })
                .WithColumn("Count", new int[] { 500, 250, 150 })
                .WithColumn("Percentage", TableTestHarness.ToString8(new string[] 
                {
                    PercentageAggregator.ThreeSigFigs(500, 1000),
                    PercentageAggregator.ThreeSigFigs(250, 1000),
                    PercentageAggregator.ThreeSigFigs(150, 1000)
                }));

            IXTable actual = TableTestHarness.DatabaseContext.FromArrays(values.Length)
                .WithColumn("Value", values)
                .Query(@"
                    groupBy [Value] with Count(), Percentage()
                    where [Count] > 5", TableTestHarness.DatabaseContext);

            TableTestHarness.AssertAreEqual(expected, actual, 2);
        }

        private static int[] BuildPeekSample()
        {
            int[] values = new int[1000];

            int i = 0;
            for (; i < 500; ++i)
            {
                // 500 are "0"
                values[i] = 0;
            }

            for (; i < 750; ++i)
            {
                // 250 are "1"
                values[i] = 1;
            }

            for (; i < 900; ++i)
            {
                // 150 are "2"
                values[i] = 2;
            }

            for (; i < values.Length; ++i)
            {
                // 100 are four each of the same (under 0.5% threshold)
                values[i] = i / 4;
            }

            return values;
        }

        [TestMethod]
        public void Verb_Skip()
        {
            // Verify skipping the first 100 rows of 0-1000 produces 100-1000
            IXTable expected = TableTestHarness.DatabaseContext.FromArrays(900)
                .WithColumn("Value", Enumerable.Range(100, 900).ToArray<int>());

            IXTable actual = TableTestHarness.DatabaseContext.FromArrays(1000)
                .WithColumn("Value", Enumerable.Range(0, 1000).ToArray<int>())
                .Query(@"
                    skip 100", TableTestHarness.DatabaseContext);

            TableTestHarness.AssertAreEqual(expected, actual, 25);
        }
    }
}

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
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
            DataBatch joinFromBatch = DataBatchTransformer.Nulls(DataBatch.All(joinFrom));
            IDataBatchEnumerator joinFromTable = XFormTable.FromArrays(joinFromBatch.Count)
                .WithColumn(new ColumnDetails("ServerID", t, true), joinFromBatch);

            // Build the table to join to
            IDataBatchEnumerator joinToTable = XFormTable.FromArrays(joinTo.Length)
               .WithColumn(new ColumnDetails("ID", t, false), DataBatch.All(joinTo));

            // Run the join - verify the expected values without padding are found
            IDataBatchEnumerator result = new Join(joinFromTable, "ServerID", joinToTable, "ID", "Server.");
            Func<DataBatch> serverID = result.ColumnGetter(result.Columns.IndexOfColumn("Server.ID"));
            int resultCount = result.Next(1024);

            DataBatchTransformer.AssertAreEqual(DataBatch.All(expected), serverID(), resultCount);
        }

        [TestMethod]
        public void Verb_Choose()
        {
            WorkflowContext context = new WorkflowContext();
            int[] rankPattern = new int[] { 2, 3, 1 };

            // Build three arrays
            int distinctCount = 100;
            int length = 3 * distinctCount;
            int[] id = new int[length];
            int[] rank = new int[length];
            int[] value = new int[length];

            for(int i = 0; i < length; ++i)
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

            for(int i = 0; i < distinctCount; ++i)
            {
                expectedIds[i] = i;
                expectedRanks[i] = 3;
                expectedValues[i] = 3 * i + 1;
            }

            IDataBatchEnumerator expected = XFormTable.FromArrays(distinctCount)
                .WithColumn("ID", expectedIds)
                .WithColumn("Rank", expectedRanks)
                .WithColumn("Value", expectedValues);

            IDataBatchEnumerator query = XFormTable.FromArrays(length)
                .WithColumn("ID", id)
                .WithColumn("Rank", rank)
                .WithColumn("Value", value)
                .Query("choose Max [Rank] [ID]", context);

            // Compare the result table with the expected one
            DataBatchTransformer.AssertAreEqual(expected, query, length);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;
using Elfie.Test;
using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class FunctionsTests
    {
        public static DateTime TestAsOfDateTime = new DateTime(2017, 12, 10, 0, 0, 0, DateTimeKind.Utc);

        [TestMethod]
        public void Function_DateAdd()
        {
            DateTime[] values = new DateTime[]
            {
                new DateTime(2017, 12, 01, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2017, 12, 02, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2017, 12, 03, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2017, 12, 04, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2017, 12, 05, 0, 0, 0, DateTimeKind.Utc),
            };

            DateTime[] expected = new DateTime[]
            {
                new DateTime(2017, 11, 29, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2017, 11, 30, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2017, 12, 01, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2017, 12, 02, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2017, 12, 03, 0, 0, 0, DateTimeKind.Utc),
            };

            RunQueryAndVerify(values, "When", expected, "Result", "set [Result] DateAdd([When], \"-2d\")");
        }

        [TestMethod]
        public void Function_Cast_Basics()
        {
            int[] expected = Enumerable.Range(-2, 10).ToArray();
            string[] values = expected.Select((i) => i.ToString()).ToArray();

            // Try casting int to String8 and back
            RunQueryAndVerify(values, "Score", expected, "Result", "set [Result] Cast(Cast([Score], String8), Int32)");

            // Verify an unavailable cast throws
            Verify.Exception<UsageException>(() => RunQueryAndVerify(expected, "Score", expected, "Result", "set [Result] Cast(Cast([Score], TimeSpan), DateTime)"));
        }

        [TestMethod]
        public void Function_Cast_Conversions()
        {
            int[] values = Enumerable.Range(0, 10).ToArray();
            RunCastConversions(typeof(sbyte), values);
            RunCastConversions(typeof(byte), values);
            RunCastConversions(typeof(short), values);
            RunCastConversions(typeof(ushort), values);
            RunCastConversions(typeof(int), values);
            RunCastConversions(typeof(uint), values);
            RunCastConversions(typeof(long), values);
            RunCastConversions(typeof(ulong), values);

            // No String8 to floating point cast yet
            //RunCastConversions(typeof(float), values);
            //RunCastConversions(typeof(double), values);
        }

        private static void RunCastConversions(Type type, int[] values)
        {
            // Convert to type and back to int
            RunQueryAndVerify(values, "Value", values, "Value", $"select Cast(Cast([Value], {type.Name}), Int32)");

            // Convert to string and back to int
            TableTestHarness.AssertAreEqual(
                TableTestHarness.DatabaseContext.FromArrays(values.Length).WithColumn("Value", values).Query($"select Cast(Cast(Cast([Value], String8), {type.Name}), Int32)", TableTestHarness.DatabaseContext),
                TableTestHarness.DatabaseContext.FromArrays(values.Length).WithColumn("Value", values), 10);
        }

        [TestMethod]
        public void Function_Trim()
        {
            String8Block block = new String8Block();
            String8[] values = new String8[]
            {
                String8.Empty,
                block.GetCopy("Simple"),
                block.GetCopy("  Simple Spaced  "),
                block.GetCopy("   "),
                block.GetCopy("\t\t\t"),
            };

            String8[] expected = new String8[]
            {
                String8.Empty,
                block.GetCopy("Simple"),
                block.GetCopy("Simple Spaced"),
                String8.Empty,
                String8.Empty
            };

            RunQueryAndVerify(values, "Name", expected, "Name", "set [Name] Trim([Name])");
        }

        [TestMethod]
        public void Function_ToUpper()
        {
            String8Block block = new String8Block();
            String8[] values = new String8[]
            {
                String8.Empty,
                block.GetCopy("Simple"),
                block.GetCopy("ALREADY"),
                block.GetCopy("   "),
            };

            String8[] expected = new String8[]
            {
                String8.Empty,
                block.GetCopy("SIMPLE"),
                block.GetCopy("ALREADY"),
                block.GetCopy("   "),
            };

            RunQueryAndVerify(values, "Name", expected, "Name", "set [Name] ToUpper([Name])");
        }

        [TestMethod]
        public void Function_AsOfDate()
        {
            int[] values = Enumerable.Range(0, 5).ToArray();
            DateTime[] expected = new DateTime[] { TestAsOfDateTime, TestAsOfDateTime, TestAsOfDateTime, TestAsOfDateTime, TestAsOfDateTime };

            // Can't try other scenarios with AsOfDate because it doesn't care of the inputs are null or indirect
            RunAndCompare(DataBatch.All(values), "RowNumber", DataBatch.All(expected), "Result", "set [Result] AsOfDate()");
        }

        public static void RunQueryAndVerify(Array values, string inputColumnName, Array expected, string outputColumnName, string queryText)
        {
            Assert.IsTrue(expected.Length > 3, "Must have at least four values for a proper test.");

            DataBatch inputBatch = DataBatch.All(values, values.Length);
            DataBatch expectedBatch = DataBatch.All(expected, expected.Length);

            // Run with full array DataBatches
            RunAndCompare(inputBatch, inputColumnName, expectedBatch, outputColumnName, queryText);

            // Add indices and gaps and verify against original set
            RunAndCompare(TableTestHarness.Pad(inputBatch), inputColumnName, expectedBatch, outputColumnName, queryText);

            // Add alternating nulls and verify alternating null/expected
            RunAndCompare(TableTestHarness.Nulls(inputBatch), inputColumnName, TableTestHarness.Nulls(expectedBatch), outputColumnName, queryText);

            // Test a single value by itself
            RunAndCompare(TableTestHarness.First(inputBatch), inputColumnName, TableTestHarness.First(expectedBatch), outputColumnName, queryText);
        }

        public static void RunAndCompare(DataBatch input, string inputColumnName, DataBatch expected, string outputColumnName, string queryText)
        {
            XDatabaseContext context = new XDatabaseContext();
            context.RequestedAsOfDateTime = TestAsOfDateTime;

            IDataBatchEnumerator query = TableTestHarness.DatabaseContext.FromArrays(input.Count)
                .WithColumn(new ColumnDetails(inputColumnName, input.Array.GetType().GetElementType()), input)
                .Query(queryText, context);

            Func<DataBatch> resultGetter = query.ColumnGetter(query.Columns.IndexOfColumn(outputColumnName));
            int pageCount;

            // Get one row only and verify
            pageCount = query.Next(1);
            TableTestHarness.AssertAreEqual(TableTestHarness.Slice(expected, 0, 1), resultGetter(), pageCount);

            // Get another (ensure values set again)
            pageCount = query.Next(1);
            TableTestHarness.AssertAreEqual(TableTestHarness.Slice(expected, 1, 2), resultGetter(), pageCount);

            // Get the rest (ensure arrays expanded when needed)
            pageCount = query.Next(expected.Count - 2);
            Assert.AreEqual(expected.Count - 2, pageCount);
            TableTestHarness.AssertAreEqual(TableTestHarness.Slice(expected, 2, expected.Count), resultGetter(), pageCount);

            // Reset and ask for all of them at once
            query.Reset();
            pageCount = query.Next(expected.Count + 1);
            Assert.AreEqual(expected.Count, pageCount);
            TableTestHarness.AssertAreEqual(expected, resultGetter(), pageCount);
        }
    }
}




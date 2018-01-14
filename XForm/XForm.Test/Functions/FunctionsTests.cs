// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
                XFormTable.FromArrays(values.Length).WithColumn("Value", values).Query($"select Cast(Cast(Cast([Value], String8), {type.Name}), Int32)", TableTestHarness.WorkflowContext),
                XFormTable.FromArrays(values.Length).WithColumn("Value", values), 10);
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
            DataBatch dataBatch = DataBatch.All(values);
            ColumnDetails details = new ColumnDetails("RowNumber", dataBatch.Array.GetType().GetElementType(), true);
            RunAndCompare(new List<ColumnDetails>() { details }, new List<DataBatch>() { dataBatch }, DataBatch.All(expected), "Result", "set [Result] AsOfDate()");
        }

        public static void RunQueryAndVerify(Array values, string inputColumnName, Array expected, string outputColumnName, string queryText)
        {
            RunQueryAndVerify(new[] { values }, new bool[][] { null }, new[] { inputColumnName }, expected, null, outputColumnName, queryText);
        }

        public static void RunQueryAndVerify(Array[] inputColumnsValues, bool[][] inputColumnsNulls, string[] inputColumnNames, Array expectedValues, bool[] expectedNulls, string outputColumnName, string queryText)
        {
            Assert.IsTrue(expectedValues.Length > 3, "Must have at least four values for a proper test.");

            List<ColumnDetails> inputColumnDetails = new List<ColumnDetails>();
            List<DataBatch> inputColumnData = new List<DataBatch>();
            List<DataBatch> inputColumnPaddedData = new List<DataBatch>();
            List<DataBatch> inputColumnNullsData = new List<DataBatch>();
            List<DataBatch> inputColumnConstantData = new List<DataBatch>();

            for (int i = 0; i < inputColumnsValues.Length; i++)
            {
                DataBatch data = DataBatch.All(inputColumnsValues[i], inputColumnsValues[i].Length, inputColumnsNulls[i]);
                ColumnDetails details = new ColumnDetails(inputColumnNames[i], data.Array.GetType().GetElementType(), true);
                inputColumnDetails.Add(details);
                inputColumnData.Add(data);
                inputColumnPaddedData.Add(TableTestHarness.Pad(data));
                inputColumnNullsData.Add(TableTestHarness.Nulls(data));
                inputColumnConstantData.Add(TableTestHarness.First(data));
            }

            DataBatch expectedBatch = DataBatch.All(expectedValues, expectedValues.Length, expectedNulls);
            DataBatch expectedNullsBatch = TableTestHarness.Nulls(expectedBatch);
            DataBatch expectedConstantBatch = TableTestHarness.First(expectedBatch);

            // Run with full array DataBatches
            RunAndCompare(inputColumnDetails, inputColumnData, expectedBatch, outputColumnName, queryText);

            // Add indices and gaps and verify against original set
            RunAndCompare(inputColumnDetails, inputColumnPaddedData, expectedBatch, outputColumnName, queryText);

            // Add alternating nulls and verify alternating null/expected
            RunAndCompare(inputColumnDetails, inputColumnNullsData, expectedNullsBatch, outputColumnName, queryText);

            // Test a single value by itself
            RunAndCompare(inputColumnDetails, inputColumnConstantData, expectedConstantBatch, outputColumnName, queryText);
        }

        public static void RunAndCompare(IList<ColumnDetails> inputColumnDetails, IList<DataBatch> inputColumnData, DataBatch expected, string outputColumnName, string queryText)
        {
            WorkflowContext context = new WorkflowContext();
            context.RequestedAsOfDateTime = TestAsOfDateTime;

            var table = XFormTable.FromArrays(inputColumnData[0].Count);
            for (int i = 0; i < inputColumnDetails.Count; i++)
            {
                table = table.WithColumn(inputColumnDetails[i], inputColumnData[i]);
            }

            var query = table.Query(queryText, context);

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

            // Test resetting of the IsNull array
            // Get two rows and verify. This will return an IsNull set of {true,false}.
            query.Reset();
            pageCount = query.Next(2);
            TableTestHarness.AssertAreEqual(TableTestHarness.Slice(expected, 0, 2), resultGetter(), pageCount);

            // Evaluate a DataBatch which returns an IsNull set of {false,true}. If the function doesn't reset the IsNull array, this should fail with the IsNull result set as {true,true}.
            query.Reset();
            pageCount = query.Next(1);
            pageCount = query.Next(2);
            TableTestHarness.AssertAreEqual(TableTestHarness.Slice(expected, 1, 3), resultGetter(), pageCount);
        }
    }
}




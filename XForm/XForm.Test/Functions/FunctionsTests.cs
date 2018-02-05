﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class FunctionsTests
    {
        public static DateTime TestAsOfDateTime = new DateTime(2017, 12, 10, 0, 0, 0, DateTimeKind.Utc);

        [TestMethod]
        public void Function_IsNull()
        {
            int[] values = new int[] { 1, 2, 0, 3, 4 };
            bool[] nulls = new bool[] { false, false, true, false, false };

            XArray input = XArray.All(values, values.Length, nulls);
            XArray expected = XArray.All(nulls, nulls.Length);
            RunQueryAndVerify(input, "Name", expected, "Name", "set [Name] IsNull([Name])");
        }

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
            RunAndCompare(XArray.All(values), "RowNumber", XArray.All(expected), "Result", "set [Result] AsOfDate()");
        }

        public static void RunQueryAndVerify(Array values, string inputColumnName, Array expected, string outputColumnName, string queryText)
        {
            XArray inputxarray = XArray.All(values, values.Length);
            XArray expectedxarray = XArray.All(expected, expected.Length);
            RunQueryAndVerify(inputxarray, inputColumnName, expectedxarray, outputColumnName, queryText);
        }

        public static void RunQueryAndVerify(XArray inputXArray, string inputColumnName, XArray expectedXArray, string outputColumnName, string queryText)
        {
            Assert.IsTrue(expectedXArray.Count > 3, "Must have at least four values for a proper test.");

            // Run with full array arrays
            RunAndCompare(inputXArray, inputColumnName, expectedXArray, outputColumnName, queryText);

            // Add indices and gaps and verify against original set
            RunAndCompare(TableTestHarness.Pad(inputXArray), inputColumnName, expectedXArray, outputColumnName, queryText);

            if (!inputXArray.HasNulls)
            {
                // Add alternating nulls and verify alternating null/expected
                RunAndCompare(TableTestHarness.Nulls(inputXArray), inputColumnName, TableTestHarness.Nulls(expectedXArray), outputColumnName, queryText);
            }

            // Test a single value by itself
            RunAndCompare(TableTestHarness.First(inputXArray), inputColumnName, TableTestHarness.First(expectedXArray), outputColumnName, queryText);
        }

        public static void RunAndCompare(XArray input, string inputColumnName, XArray expected, string outputColumnName, string queryText)
        {
            XDatabaseContext context = new XDatabaseContext
            {
                RequestedAsOfDateTime = TestAsOfDateTime
            };

            IXTable query = TableTestHarness.DatabaseContext.FromArrays(input.Count)
                .WithColumn(new ColumnDetails(inputColumnName, input.Array.GetType().GetElementType()), input)
                .Query(queryText, context);

            Func<XArray> resultGetter = query.Columns.Find(outputColumnName).CurrentGetter();
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




// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;
using XForm;

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
        public void Function_Cast()
        {
            int[] expected = Enumerable.Range(-2, 10).ToArray();
            string[] values = expected.Select((i) => i.ToString()).ToArray();

            RunQueryAndVerify(values, "Score", expected, "Result", "set [Result] Cast(Cast([Score], String8), Int32)");
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
            RunAndCompare(DataBatchTransformer.Pad(inputBatch), inputColumnName, expectedBatch, outputColumnName, queryText);

            // Add alternating nulls and verify alternating null/expected
            RunAndCompare(DataBatchTransformer.Nulls(inputBatch), inputColumnName, DataBatchTransformer.Nulls(expectedBatch), outputColumnName, queryText);

            // Test a single value by itself
            RunAndCompare(DataBatchTransformer.First(inputBatch), inputColumnName, DataBatchTransformer.First(expectedBatch), outputColumnName, queryText);
        }

        public static void RunAndCompare(DataBatch input, string inputColumnName, DataBatch expected, string outputColumnName, string queryText)
        {
            WorkflowContext context = new WorkflowContext();
            context.RequestedAsOfDateTime = TestAsOfDateTime;

            IDataBatchEnumerator query = XFormTable.FromArrays(input.Count)
                .WithColumn(new ColumnDetails(inputColumnName, input.Array.GetType().GetElementType(), true), input)
                .Query(queryText, context);

            Func<DataBatch> resultGetter = query.ColumnGetter(query.Columns.IndexOfColumn(outputColumnName));
            int pageCount;

            // Get one row only and verify
            pageCount = query.Next(1);
            DataBatchTransformer.AssertAreEqual(DataBatchTransformer.Slice(expected, 0, 1), resultGetter(), pageCount);

            // Get another (ensure values set again)
            pageCount = query.Next(1);
            DataBatchTransformer.AssertAreEqual(DataBatchTransformer.Slice(expected, 1, 2), resultGetter(), pageCount);

            // Get the rest (ensure arrays expanded when needed)
            pageCount = query.Next(expected.Count - 2);
            Assert.AreEqual(expected.Count - 2, pageCount);
            DataBatchTransformer.AssertAreEqual(DataBatchTransformer.Slice(expected, 2, expected.Count), resultGetter(), pageCount);

            // Reset and ask for all of them at once
            query.Reset();
            pageCount = query.Next(expected.Count + 1);
            Assert.AreEqual(expected.Count, pageCount);
            DataBatchTransformer.AssertAreEqual(expected, resultGetter(), pageCount);
        }
    }
}

public static class DataBatchTransformer
{
    public static void AssertAreEqual(IDataBatchEnumerator expected, IDataBatchEnumerator actual, int rowCountToGet)
    {
        // Get the column getters for every expected column and the columns of the same names in actual
        Func<DataBatch>[] expectedGetters = new Func<DataBatch>[expected.Columns.Count];
        Func<DataBatch>[] actualGetters = new Func<DataBatch>[actual.Columns.Count];

        for(int i = 0; i < expected.Columns.Count; ++i)
        {
            expectedGetters[i] = expected.ColumnGetter(i);
            actualGetters[i] = actual.ColumnGetter(actual.Columns.IndexOfColumn(expected.Columns[i].Name));
        }

        // Ask for the number of rows the caller specified
        int expectedCount = expected.Next(rowCountToGet);
        int actualCount = actual.Next(rowCountToGet);
        Assert.AreEqual(expectedCount, actualCount, "Actual Enumerator didn't return the expected number of rows.");

        // Validate the column batches match
        for(int i = 0; i < expected.Columns.Count; ++i)
        {
            AssertAreEqual(expectedGetters[i](), actualGetters[i](), expectedCount);
        }
    }

    public static void AssertAreEqual(DataBatch expected, DataBatch actual, int nextRowCount)
    {
        Assert.AreEqual(expected.Count, nextRowCount, "Next() didn't return expected row count.");
        Assert.AreEqual(expected.Count, actual.Count, "Row count returned was not correct");
        Assert.AreEqual(expected.Array.GetType().GetElementType(), actual.Array.GetType().GetElementType(), "Result isn't of the expected type");

        bool areAnyNull = false;
        for (int i = 0; i < expected.Count; ++i)
        {
            int expectedIndex = expected.Index(i);
            int actualIndex = actual.Index(i);
            bool isNull = false;

            if (expected.IsNull != null)
            {
                isNull = expected.IsNull[expectedIndex];
                Assert.AreEqual(isNull, actual.IsNull != null && actual.IsNull[actualIndex], $"Value for row {i:n0} wasn't null or not as expected.");
            }

            if (!isNull)
            {
                Assert.AreEqual(expected.Array.GetValue(expectedIndex), actual.Array.GetValue(actualIndex), $"Value for row {i:n0} was not expected.");
            }

            areAnyNull |= isNull;
        }

        if (!areAnyNull) Assert.IsTrue(actual.IsNull == null, "Result shouldn't have a null array if no values are null");
    }

    // Return an IsSingleElement array with just the first value of the batch, but the same count
    public static DataBatch First(DataBatch values)
    {
        Array modifiedArray = Allocator.AllocateArray(values.Array.GetType().GetElementType(), 1);
        modifiedArray.SetValue(values.Array.GetValue(values.Index(0)), 0);

        return DataBatch.Single(modifiedArray, values.Count);
    }

    // Return a DataBatch with two empty array elements before and after the valid portion and indices pointing to the valid portion
    public static DataBatch Pad(DataBatch values)
    {
        Array modifiedArray = Allocator.AllocateArray(values.Array.GetType().GetElementType(), values.Array.Length + 4);
        int[] indices = new int[modifiedArray.Length];

        // Copy values shifted over two (so, two default values at the beginning and two at the end)
        for (int i = 0; i < values.Array.Length; ++i)
        {
            indices[i] = i + 2;
            modifiedArray.SetValue(values.Array.GetValue(values.Index(i)), indices[i]);
        }

        // Return a DataBatch with the padded array with the indices and shorter real length
        int[] remapArray = null;
        return DataBatch.All(modifiedArray, values.Count).Select(ArraySelector.Map(indices, values.Count), ref remapArray);
    }

    // Return a DataBatch with nulls inserted for every other value
    public static DataBatch Nulls(DataBatch values)
    {
        Array modifiedArray = Allocator.AllocateArray(values.Array.GetType().GetElementType(), values.Array.Length * 2);
        bool[] isNull = new bool[modifiedArray.Length];

        // Every other value is null
        for (int i = 0; i < modifiedArray.Length; ++i)
        {
            isNull[i] = (i % 2 == 0);
            modifiedArray.SetValue(values.Array.GetValue(values.Index(i / 2)), i);
        }

        // Return a DataBatch with the doubled length and alternating nulls
        return DataBatch.All(modifiedArray, modifiedArray.Length, isNull);
    }

    // Return a DataBatch with a slice of the rows only
    public static DataBatch Slice(DataBatch values, int startIndexInclusive, int endIndexExclusive)
    {
        int[] remapArray = null;
        return values.Select(values.Selector.Slice(startIndexInclusive, endIndexExclusive), ref remapArray);
    }
}


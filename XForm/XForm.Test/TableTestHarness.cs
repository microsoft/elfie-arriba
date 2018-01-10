using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using XForm.Data;
using XForm.Extensions;

namespace XForm.Test
{
    /// <summary>
    ///  TableTestHarness has methods to compare DataBatches and IDataBatchEnumerators and to transform DataBatches
    ///  into the different valid forms they have (full array, array slice, indices, nulls, single value).
    /// </summary>
    public static class TableTestHarness
    {
        public static void AssertAreEqual(IDataBatchEnumerator expected, IDataBatchEnumerator actual, int pageSize)
        {
            // Reset both tables (so they can be used for repeated scenarios)
            expected.Reset();
            actual.Reset();

            // Get the column getters for every expected column and the columns of the same names in actual
            Func<DataBatch>[] expectedGetters = new Func<DataBatch>[expected.Columns.Count];
            Func<DataBatch>[] actualGetters = new Func<DataBatch>[actual.Columns.Count];

            for (int i = 0; i < expected.Columns.Count; ++i)
            {
                expectedGetters[i] = expected.ColumnGetter(i);
                actualGetters[i] = actual.ColumnGetter(actual.Columns.IndexOfColumn(expected.Columns[i].Name));
            }

            // Loop over rows, comparing as many rows as available each time
            int totalRowCount = 0;
            int expectedCurrentCount = 0, expectedNextIndex = 0;
            int actualCurrentCount = 0, actualNextIndex = 0;
            DataBatch[] expectedBatches = new DataBatch[expected.Columns.Count];
            DataBatch[] actualBatches = new DataBatch[expected.Columns.Count];

            while (true)
            {
                // Get new expected rows if we've compared all of the current ones already
                if (expectedNextIndex >= expectedCurrentCount)
                {
                    expectedNextIndex = 0;
                    expectedCurrentCount = expected.Next(pageSize);

                    for (int i = 0; i < expected.Columns.Count; ++i)
                    {
                        expectedBatches[i] = expectedGetters[i]();
                    }
                }

                // Get new actual rows if we've compared all of the current ones already
                if (actualNextIndex >= actualCurrentCount)
                {
                    actualNextIndex = 0;
                    actualCurrentCount = actual.Next(pageSize);

                    for (int i = 0; i < expected.Columns.Count; ++i)
                    {
                        actualBatches[i] = actualGetters[i]();
                    }
                }

                // If we're out of rows from both sides, stop
                if (expectedCurrentCount == 0 && actualCurrentCount == 0) break;

                // Figure out how many rows we can compare this time (the minimum available from both sides)
                int countToCompare = Math.Min(expectedCurrentCount - expectedNextIndex, actualCurrentCount - actualNextIndex);

                string errorMessage = "";
                int firstMismatchedRow = -1;

                // If we ran out of rows on one side before the other, fail
                if (countToCompare == 0)
                {
                    errorMessage = $"Ran out of rows after {totalRowCount + expectedCurrentCount - expectedNextIndex:n0} Expected rows but {totalRowCount + actualCurrentCount - actualNextIndex:n0} Actual rows.";
                    firstMismatchedRow = Math.Max(expectedCurrentCount - expectedNextIndex, actualCurrentCount - actualNextIndex);
                }
                else
                {
                    // Get the current batch for each column, slice to the set of rows to compare, and compare them
                    for (int i = 0; i < expected.Columns.Count; ++i)
                    {
                        DataBatch expectedBatch = expectedBatches[i].Slice(expectedNextIndex, expectedNextIndex + countToCompare);
                        DataBatch actualBatch = actualBatches[i].Slice(actualNextIndex, actualNextIndex + countToCompare);

                        firstMismatchedRow = FirstMismatchedRow(expectedBatch, actualBatch, countToCompare, expected.Columns[i].Name, out errorMessage);
                        if (!String.IsNullOrEmpty(errorMessage)) break;
                    }
                }

                // If the table spans weren't equal, show the rows and error message
                if(!String.IsNullOrEmpty(errorMessage))
                {
                    Trace.WriteLine("Expected:");
                    TraceWrite(expectedBatches, expected.Columns, expectedNextIndex + firstMismatchedRow, expectedCurrentCount - (expectedNextIndex + firstMismatchedRow));

                    Trace.WriteLine("Actual:");
                    TraceWrite(actualBatches, expected.Columns, actualNextIndex + firstMismatchedRow, actualCurrentCount - (actualNextIndex + firstMismatchedRow));

                    Assert.Fail(errorMessage);
                }

                expectedNextIndex += countToCompare;
                actualNextIndex += countToCompare;
                totalRowCount += countToCompare;
            }
        }

        public static void AssertAreEqual(DataBatch expected, DataBatch actual, int rowCount, string columnName = "")
        {
            string errorMessage = "";
            int firstMismatchedRow = FirstMismatchedRow(expected, actual, rowCount, columnName, out errorMessage);
            
            if(!String.IsNullOrEmpty(errorMessage))
            {
                Trace.WriteLine("Expected:");
                TraceWrite(expected, columnName, firstMismatchedRow, expected.Count - firstMismatchedRow);

                Trace.WriteLine("Actual:");
                TraceWrite(actual, columnName, firstMismatchedRow, actual.Count - firstMismatchedRow);

                Assert.Fail(errorMessage);
            }
        }

        public static int FirstMismatchedRow(DataBatch expected, DataBatch actual, int rowCount, string columnName, out string errorMessage)
        {
            errorMessage = "";
            AssertAreEqual(rowCount, expected.Count, "Expected Set RowCount", ref errorMessage);
            AssertAreEqual(rowCount, actual.Count, "Actual Set RowCount", ref errorMessage);
            AssertAreEqual(expected.Array.GetType().GetElementType(), actual.Array.GetType().GetElementType(), "Array Type", ref errorMessage);
            if (errorMessage != "") return 0;

            bool areAnyNull = false;
            for (int i = 0; i < expected.Count; ++i)
            {
                int expectedIndex = expected.Index(i);
                int actualIndex = actual.Index(i);
                bool isNull = false;

                if (expected.IsNull != null)
                {
                    isNull = expected.IsNull[expectedIndex];
                    if (!AssertAreEqual(isNull, (actual.IsNull != null && actual.IsNull[actualIndex]), $"{columnName}[{i:n0}].IsNull", ref errorMessage)) return i;
                }

                if (!isNull)
                {
                    if(!AssertAreEqual(expected.Array.GetValue(expectedIndex), actual.Array.GetValue(actualIndex), $"{columnName}[{i:n0}].Value", ref errorMessage)) return i;
                }

                areAnyNull |= isNull;
            }

            if (!areAnyNull) AssertAreEqual(true, actual.IsNull == null, "Result Null Array (when no null values)", ref errorMessage);
            return 0;
        }

        /// <summary>
        ///  Compare expected and actual and set the error message if they don't match.
        ///  Don't run if the error message is already non-empty.
        ///  Return true if the values were equal, false if not (or if there is an earlier error).
        /// </summary>
        private static bool AssertAreEqual(object expected, object actual, string context, ref string errorMessage)
        {
            if (!String.IsNullOrEmpty(errorMessage)) return false;
            if (!expected.Equals(actual))
            {
                errorMessage = $"{context}: Was \"{actual}\", Expected \"{expected}\"";
                return false;
            }

            return true;
        }

        /// <summary>
        ///  Write a single column to the Tracing system for debugging.
        /// </summary>
        public static void TraceWrite(DataBatch column, string columnName, int startRowIndexInclusive = 0, int endRowIndexExclusive = -1)
        {
            TraceWrite(new DataBatch[] { column }, new ColumnDetails[] { new ColumnDetails(columnName, typeof(String8), false) }, startRowIndexInclusive, endRowIndexExclusive);
        }

        /// <summary>
        ///  Write a table to the Tracing system for debugging.
        /// </summary>
        public static void TraceWrite(DataBatch[] columns, IReadOnlyList<ColumnDetails> columnDetails, int startRowIndexInclusive = 0, int endRowIndexExclusive = -1)
        {
            StringBuilder row = new StringBuilder();

            for (int columnIndex = 0; columnIndex < columns.Length; ++columnIndex)
            {
                AppendWithPadding(columnDetails[columnIndex].Name, 10, row);
            }

            Trace.WriteLine(row.ToString());

            if (endRowIndexExclusive == -1 || endRowIndexExclusive > columns[0].Count) endRowIndexExclusive = columns[0].Count;
            for (int rowIndex = startRowIndexInclusive; rowIndex < endRowIndexExclusive; ++rowIndex)
            {
                row.Clear();

                for (int columnIndex = 0; columnIndex < columns.Length; ++columnIndex)
                {
                    DataBatch column = columns[columnIndex];
                    int realRowIndex = column.Index(rowIndex);

                    // NOTE: Untyped Array access via object in XForm is very expensive. Don't do this in non-test code.
                    object value = null;
                    if (column.IsNull == null || column.IsNull[realRowIndex] == false)
                    {
                        value = column.Array.GetValue(realRowIndex);
                    }

                    if (columnIndex > 0) row.Append("    ");
                    AppendWithPadding(value, 10, row);
                }

                Trace.WriteLine(row.ToString());
            }

            Trace.WriteLine("");
        }

        private static void AppendWithPadding(object value, int length, StringBuilder output)
        {
            string asString = (value ?? "<null>").ToString();
            output.Append(asString);
            output.Append(' ');
            if (asString.Length < length) output.Append(' ', length - asString.Length);
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
}

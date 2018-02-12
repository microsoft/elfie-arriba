// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;

namespace XForm.Test
{
    /// <summary>
    ///  TableTestHarness has methods to compare arrays and IXTables and to transform arrays
    ///  into the different valid forms they have (full array, array slice, indices, nulls, single value).
    /// </summary>
    public static class TableTestHarness
    {
        private static XDatabaseContext s_DatabaseContext;

        public static XDatabaseContext DatabaseContext
        {
            get
            {
                if (s_DatabaseContext == null) s_DatabaseContext = new XDatabaseContext();
                return s_DatabaseContext;
            }
        }

        public static void AssertAreEqual(IXTable expected, IXTable actual, int pageSize)
        {
            // Reset both tables (so they can be used for repeated scenarios)
            expected.Reset();
            actual.Reset();

            // Get the column getters for every expected column and the columns of the same names in actual
            Func<XArray>[] expectedGetters = new Func<XArray>[expected.Columns.Count];
            Func<XArray>[] actualGetters = new Func<XArray>[actual.Columns.Count];

            for (int i = 0; i < expected.Columns.Count; ++i)
            {
                expectedGetters[i] = expected.Columns[i].CurrentGetter();
                actualGetters[i] = actual.Columns.Find(expected.Columns[i].ColumnDetails.Name).CurrentGetter();
            }

            // Loop over rows, comparing as many rows as available each time
            int totalRowCount = 0;
            int expectedCurrentCount = 0, expectedNextIndex = 0;
            int actualCurrentCount = 0, actualNextIndex = 0;
            XArray[] expectedArrays = new XArray[expected.Columns.Count];
            XArray[] actualArrays = new XArray[expected.Columns.Count];

            while (true)
            {
                // Get new expected rows if we've compared all of the current ones already
                if (expectedNextIndex >= expectedCurrentCount)
                {
                    expectedNextIndex = 0;
                    expectedCurrentCount = expected.Next(pageSize);

                    for (int i = 0; i < expected.Columns.Count; ++i)
                    {
                        expectedArrays[i] = expectedGetters[i]();
                    }
                }

                // Get new actual rows if we've compared all of the current ones already
                if (actualNextIndex >= actualCurrentCount)
                {
                    actualNextIndex = 0;
                    actualCurrentCount = actual.Next(pageSize);

                    for (int i = 0; i < expected.Columns.Count; ++i)
                    {
                        actualArrays[i] = actualGetters[i]();
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
                    // Get the current xarray for each column, slice to the set of rows to compare, and compare them
                    for (int i = 0; i < expected.Columns.Count; ++i)
                    {
                        XArray expectedArray = expectedArrays[i].Slice(expectedNextIndex, expectedNextIndex + countToCompare);
                        XArray actualArray = actualArrays[i].Slice(actualNextIndex, actualNextIndex + countToCompare);

                        firstMismatchedRow = FirstMismatchedRow(
                            expectedArray,
                            actualArray,
                            countToCompare,
                            expected.Columns[i].ColumnDetails.Name,
                            out errorMessage);

                        if (!String.IsNullOrEmpty(errorMessage)) break;
                    }
                }

                // If the table spans weren't equal, show the rows and error message
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    Trace.WriteLine("Expected:");
                    TraceWrite(expectedArrays, expected.Columns.Select((col) => col.ColumnDetails).ToArray(), expectedNextIndex + firstMismatchedRow, expectedCurrentCount - (expectedNextIndex + firstMismatchedRow));

                    Trace.WriteLine("Actual:");
                    TraceWrite(actualArrays, expected.Columns.Select((col) => col.ColumnDetails).ToArray(), actualNextIndex + firstMismatchedRow, actualCurrentCount - (actualNextIndex + firstMismatchedRow));

                    Assert.Fail(errorMessage);
                }

                expectedNextIndex += countToCompare;
                actualNextIndex += countToCompare;
                totalRowCount += countToCompare;
            }
        }

        public static void AssertAreEqual(XArray expected, XArray actual, int rowCount, string columnName = "")
        {
            string errorMessage = "";
            int firstMismatchedRow = FirstMismatchedRow(expected, actual, rowCount, columnName, out errorMessage);

            if (!String.IsNullOrEmpty(errorMessage))
            {
                Trace.WriteLine("Expected:");
                TraceWrite(expected, columnName, firstMismatchedRow, expected.Count - firstMismatchedRow);

                Trace.WriteLine("Actual:");
                TraceWrite(actual, columnName, firstMismatchedRow, actual.Count - firstMismatchedRow);

                Assert.Fail(errorMessage);
            }
        }

        public static int FirstMismatchedRow(XArray expected, XArray actual, int rowCount, string columnName, out string errorMessage)
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

                if (expected.HasNulls)
                {
                    isNull = expected.NullRows[expectedIndex];
                    if (!AssertAreEqual(isNull, (actual.HasNulls && actual.NullRows[actualIndex]), $"{columnName}[{i:n0}].IsNull", ref errorMessage)) return i;
                }

                if (!isNull)
                {
                    if (!AssertAreEqual(expected.Array.GetValue(expectedIndex), actual.Array.GetValue(actualIndex), $"{columnName}[{i:n0}].Value", ref errorMessage)) return i;
                }

                areAnyNull |= isNull;
            }

            // NOTE: We should not filter the null array if we aren't looping over every item, so this rule isn't always valid.
            //if (!areAnyNull) AssertAreEqual(true, actual.HasNulls, "Result Null Array (when no null values)", ref errorMessage);

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
        public static void TraceWrite(XArray column, string columnName, int startRowIndexInclusive = 0, int endRowIndexExclusive = -1)
        {
            TraceWrite(new XArray[] { column }, new ColumnDetails[] { new ColumnDetails(columnName, typeof(String8)) }, startRowIndexInclusive, endRowIndexExclusive);
        }

        /// <summary>
        ///  Write a table to the Tracing system for debugging.
        /// </summary>
        public static void TraceWrite(IXTable table, int rowCount = XTableExtensions.DefaultBatchSize)
        {
            Func<XArray>[] columnGetters = new Func<XArray>[table.Columns.Count];
            XArray[] columns = new XArray[table.Columns.Count];

            for (int i = 0; i < columns.Length; ++i)
            {
                columnGetters[i] = table.Columns[i].CurrentGetter();
            }

            table.Next(rowCount);

            for (int i = 0; i < columns.Length; ++i)
            {
                columns[i] = columnGetters[i]();
            }

            TraceWrite(columns, table.Columns.Select((col) => col.ColumnDetails).ToArray());
            table.Reset();
        }

        /// <summary>
        ///  Write a table to the Tracing system for debugging.
        /// </summary>
        public static void TraceWrite(XArray[] arrays, ColumnDetails[] columnDetails, int startRowIndexInclusive = 0, int endRowIndexExclusive = -1)
        {
            StringBuilder row = new StringBuilder();

            for (int columnIndex = 0; columnIndex < arrays.Length; ++columnIndex)
            {
                AppendWithPadding(columnDetails[columnIndex].Name, 10, row);
            }

            Trace.WriteLine(row.ToString());

            if (endRowIndexExclusive == -1 || endRowIndexExclusive > arrays[0].Count) endRowIndexExclusive = arrays[0].Count;
            for (int rowIndex = startRowIndexInclusive; rowIndex < endRowIndexExclusive; ++rowIndex)
            {
                row.Clear();

                for (int columnIndex = 0; columnIndex < arrays.Length; ++columnIndex)
                {
                    XArray column = arrays[columnIndex];
                    int realRowIndex = column.Index(rowIndex);

                    // NOTE: Untyped Array access via object in XForm is very expensive. Don't do this in non-test code.
                    object value = null;
                    if (!column.HasNulls || !column.NullRows[realRowIndex])
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

        public static String8[] ToString8(String[] values)
        {
            String8Block block = new String8Block();

            String8[] result = new String8[values.Length];
            for (int i = 0; i < values.Length; ++i)
            {
                result[i] = block.GetCopy(values[i]);
            }

            return result;
        }

        // Return an IsSingleElement array with just the first value of the xarray, but the same count
        public static XArray First(XArray values)
        {
            Array modifiedArray = null;
            Allocator.AllocateToSize(ref modifiedArray, 1, values.Array.GetType().GetElementType());
            modifiedArray.SetValue(values.Array.GetValue(values.Index(0)), 0);

            return XArray.Single(modifiedArray, values.Count);
        }

        // Return an XArray with two empty array elements before and after the valid portion and indices pointing to the valid portion
        public static XArray Pad(XArray values)
        {
            Array modifiedArray = null;
            bool[] nulls = null;
            Allocator.AllocateToSize(ref modifiedArray, values.Array.Length + 4, values.Array.GetType().GetElementType());

            if (values.HasNulls)
            {
                nulls = new bool[values.Array.Length + 4];
            }

            int[] indices = new int[modifiedArray.Length];

            // Copy values shifted over two (so, two default values at the beginning and two at the end)
            for (int i = 0; i < values.Array.Length; ++i)
            {
                indices[i] = i + 2;
                modifiedArray.SetValue(values.Array.GetValue(values.Index(i)), indices[i]);

                if (values.HasNulls)
                {
                    nulls.SetValue(values.NullRows.GetValue(values.Index(i)), indices[i]);
                }
            }

            // Return an XArray with the padded array with the indices and shorter real length
            int[] remapArray = null;
            return XArray.All(modifiedArray, values.Count, nulls).Select(ArraySelector.Map(indices, values.Count), ref remapArray);
        }

        // Return an XArray with nulls inserted for every other value
        public static XArray Nulls(XArray values)
        {
            Array modifiedArray = null;
            Allocator.AllocateToSize(ref modifiedArray, values.Array.Length * 2, values.Array.GetType().GetElementType());
            bool[] nulls = new bool[modifiedArray.Length];

            // Every other value is null
            for (int i = 0; i < modifiedArray.Length; ++i)
            {
                nulls[i] = (i % 2 == 0);
                modifiedArray.SetValue(values.Array.GetValue(values.Index(i / 2)), i);
            }

            // Return an XArray with the doubled length and alternating nulls
            return XArray.All(modifiedArray, modifiedArray.Length, nulls);
        }

        // Return an XArray with a slice of the rows only
        public static XArray Slice(XArray values, int startIndexInclusive, int endIndexExclusive)
        {
            int[] remapArray = null;
            return values.Select(values.Selector.Slice(startIndexInclusive, endIndexExclusive), ref remapArray);
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

                // If we ran out of rows on one side before the other, fail
                if (countToCompare == 0)
                {
                    Assert.Fail($"Ran out of rows after {totalRowCount + expectedCurrentCount - expectedNextIndex:n0} Expected rows but {totalRowCount + actualCurrentCount - actualNextIndex:n0} Actual rows.");
                    break;
                }

                // Get the current batch for each column, slice to the set of rows to compare, and compare them
                for (int i = 0; i < expected.Columns.Count; ++i)
                {
                    DataBatch expectedBatch = expectedBatches[i].Slice(expectedNextIndex, expectedNextIndex + countToCompare);
                    DataBatch actualBatch = actualBatches[i].Slice(actualNextIndex, actualNextIndex + countToCompare);
                    AssertAreEqual(expectedBatch, actualBatch, countToCompare, expected.Columns[i].Name);
                }

                expectedNextIndex += countToCompare;
                actualNextIndex += countToCompare;
                totalRowCount += countToCompare;
            }
        }

        public static void AssertAreEqual(DataBatch expected, DataBatch actual, int rowCount, string columnName = "")
        {
            Assert.AreEqual(expected.Count, rowCount, "Expected didn't have the expected row count.");
            Assert.AreEqual(expected.Count, rowCount, "Actual didn't have the expected row count.");
            Assert.AreEqual(expected.Array.GetType().GetElementType(), actual.Array.GetType().GetElementType(), $"{columnName} Result isn't of the expected type");

            bool areAnyNull = false;
            for (int i = 0; i < expected.Count; ++i)
            {
                int expectedIndex = expected.Index(i);
                int actualIndex = actual.Index(i);
                bool isNull = false;

                if (expected.IsNull != null)
                {
                    isNull = expected.IsNull[expectedIndex];
                    Assert.AreEqual(isNull, actual.IsNull != null && actual.IsNull[actualIndex], $"Value for {columnName} row {i:n0} wasn't null or not as expected.");
                }

                if (!isNull)
                {
                    Assert.AreEqual(expected.Array.GetValue(expectedIndex), actual.Array.GetValue(actualIndex), $"Value for {columnName} row {i:n0} was not expected.");
                }

                areAnyNull |= isNull;
            }

            if (!areAnyNull) Assert.IsTrue(actual.IsNull == null, $"{columnName} Result shouldn't have a null array if no values are null");
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

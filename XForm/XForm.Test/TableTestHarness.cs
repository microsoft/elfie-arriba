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
        public static void AssertAreEqual(IDataBatchEnumerator expected, IDataBatchEnumerator actual, int rowCountToGet)
        {
            // Get the column getters for every expected column and the columns of the same names in actual
            Func<DataBatch>[] expectedGetters = new Func<DataBatch>[expected.Columns.Count];
            Func<DataBatch>[] actualGetters = new Func<DataBatch>[actual.Columns.Count];

            for (int i = 0; i < expected.Columns.Count; ++i)
            {
                expectedGetters[i] = expected.ColumnGetter(i);
                actualGetters[i] = actual.ColumnGetter(actual.Columns.IndexOfColumn(expected.Columns[i].Name));
            }

            // Ask for the number of rows the caller specified
            int expectedCount = expected.Next(rowCountToGet);
            int actualCount = actual.Next(rowCountToGet);
            Assert.AreEqual(expectedCount, actualCount, "Actual Enumerator didn't return the expected number of rows.");

            // Validate the column batches match
            for (int i = 0; i < expected.Columns.Count; ++i)
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
}

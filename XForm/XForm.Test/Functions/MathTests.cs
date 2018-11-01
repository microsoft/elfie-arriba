using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using XForm.Data;
using XForm.Extensions;

namespace XForm.Test.Functions
{
    [TestClass]
    public class MathTests
    {
        [TestMethod]
        public void Function_Add()
        {
            int[] left = new int[]       { 10, 20, 30, 40, 50 };
            int[] right = new int[]      { 55, 56, 57, 58, 59 };
            long[] expected = new long[] { 65, 76, 87, 98, 109 };

            RunQueryAndVerify(left, right, expected, "set [Result] Add([Left], [Right])");
        }

        [TestMethod]
        public void Function_Subtract()
        {
            ushort[] left = new ushort[]  { 100, 110, 120, 130, 140, 150 };
            ushort[] right = new ushort[] { 100, 120, 110,   0,  90, 300 };
            long[] expected = new long[]  {   0, -10,  10, 130,  50, -150 };

            RunQueryAndVerify(left, right, expected, "set [Result] Subtract([Left], [Right])");
        }

        [TestMethod]
        public void Function_Multiply()
        {
            ushort[] left = new ushort[] { 5,  6,  7,  8, 9,  10 };
            sbyte[] right = new sbyte[]  { 1,  3,  5,  7, 0,  -1 };
            long[] expected = new long[] { 5, 18, 35, 56, 0, -10 };

            RunQueryAndVerify(left, right, expected, "set [Result] Multiply([Left], [Right])");
        }

        [TestMethod]
        public void Function_Divide()
        {
            ulong[] left = new ulong[]   { 100, 81, 66, 36, 25, 16, 9, 1 };
            uint[] right = new uint[]    {  10,  9,  8,  6,  5,  4, 3, 1 };
            long[] expected = new long[] {  10,  9,  8,  6,  5,  4, 3, 1 };

            RunQueryAndVerify(left, right, expected, "set [Result] Divide([Left], [Right])");
        }

        public static void RunQueryAndVerify(Array left, Array right, Array expected, string queryText)
        {
            XArray leftXArray = XArray.All(left, left.Length);
            XArray rightXArray = XArray.All(right, right.Length);
            XArray expectedXArray = XArray.All(expected, expected.Length);

            RunQueryAndVerify(leftXArray, rightXArray, expectedXArray, queryText);
        }

        public static void RunQueryAndVerify(XArray left, XArray right, XArray expected, string queryText)
        {
            Assert.IsTrue(expected.Count > 3, "Must have at least four values for a proper test.");

            // Run with full array arrays
            RunAndCompare(left, right, expected, queryText);

            // Add indices and gaps and verify against original set
            RunAndCompare(TableTestHarness.Pad(left), TableTestHarness.Pad(right), expected, queryText);

            if (!left.HasNulls)
            {
                // Add alternating nulls and verify alternating null/expected
                RunAndCompare(TableTestHarness.Nulls(left), TableTestHarness.Nulls(right), TableTestHarness.Nulls(expected), queryText);
            }

            // Test a single value by itself
            RunAndCompare(TableTestHarness.First(left), TableTestHarness.First(right), TableTestHarness.First(expected), queryText);
        }

        public static void RunAndCompare(XArray left, XArray right, XArray expected, string queryText)
        {
            XDatabaseContext context = new XDatabaseContext
            {
                RequestedAsOfDateTime = DateTime.UtcNow
            };

            IXTable query = TableTestHarness.DatabaseContext.FromArrays(left.Count)
                .WithColumn(new ColumnDetails("Left", left.Array.GetType().GetElementType()), left)
                .WithColumn(new ColumnDetails("Right", right.Array.GetType().GetElementType()), right)
                .Query(queryText, context);

            Func<XArray> resultGetter = query.Columns.Find("Result").CurrentGetter();
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

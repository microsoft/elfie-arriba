// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Arriba.Model.Column;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Structures
{
    [TestClass]
    public class DataBlockTests
    {
        [TestMethod]
        public void DataBlock_Basic()
        {
            string[] columns = new string[] { "ID", "Created Date", "Priority" };
            DataBlock b = new DataBlock(columns, 4,
                new object[] {
                    new int[] { 11512, 11643, 11943, 11999 },
                    new DateTime[] { new DateTime(2013, 01, 01), new DateTime(2013, 01, 02), new DateTime(2013, 01, 03), new DateTime(2013, 01, 04) },
                    new int[] { 0, 3, 1, 3 }
                });

            // Verify columns, rows, accessor
            Assert.AreEqual(4, b.RowCount);
            Assert.AreEqual(3, b.ColumnCount);
            Assert.AreEqual(String.Join(", ", columns), String.Join(", ", b.Columns.Select((cd) => cd.Name)));
            Assert.AreEqual(11512, b[0, 0]);
            Assert.AreEqual(11643, b[1, 0]);
            Assert.AreEqual(new DateTime(2013, 01, 01), b[0, 1]);

            // Verify SetRow
            b.SetRow(1, new object[] { 11644, new DateTime(2013, 01, 03, 0, 0, 0, DateTimeKind.Utc), 4 });
            Assert.AreEqual(11512, b[0, 0]);
            Assert.AreEqual(11644, b[1, 0]);
            Assert.AreEqual(4, b[1, 2]);

            // Verify SetColumn
            b.SetColumn(2, new object[] { 1, 2, 3, 4 });
            Assert.AreEqual(2, b[1, 2]);
            Assert.AreEqual(4, b[3, 2]);

            // Verify StripToColumns
            DataBlock stripped = b.StripToColumns(new string[] { "ID", "Priority" });
            Assert.AreEqual(4, stripped.RowCount);
            Assert.AreEqual(2, stripped.ColumnCount);
            Assert.AreEqual(11999, stripped[3, 0]);
            Assert.AreEqual(4, stripped[3, 1]);

            // Verify StripToColumns validates column name
            Verify.Exception<ArgumentException>(() => b.StripToColumns(new string[] { "ID", "Missing" }));

            // Verify IndexOfColumn
            Assert.AreEqual(0, b.IndexOfColumn("ID"));
            Assert.AreEqual(-1, b.IndexOfColumn("Missing"));

            // Verify GetValue, SetValue
            b.SetValue(0, 0, 12000);
            b.SetValue(1, 0, 13000);
            Assert.AreEqual(12000, b.GetValue(0, 0));
            Assert.AreEqual(13000, b.GetValue(1, 0));
        }

        [TestMethod]
        public void DataBlock_RowArrayConstructor()
        {
            // DEPRECATED: This is inefficient - don't build DataBlocks this way.
            DataBlock b = new DataBlock(new string[] { "ID", "Title" },
                new object[] {
                    new object[] { 12345, "Sample Value" },
                    new object[] { 12346, "Second Value" }
                });

            Assert.AreEqual(12345, b[0, 0]);
            Assert.AreEqual("Sample Value", b[0, 1]);
            Assert.AreEqual(12346, b[1, 0]);
        }

        [TestMethod]
        public void DataBlock_ConstructorValidation()
        {
            // Null ColumnNames
            Verify.Exception<ArgumentNullException>(() => new DataBlock((string[])null, 1));
            Verify.Exception<ArgumentNullException>(() => new DataBlock((ColumnDetails[])null, 1));

            // Null Rows
            Verify.Exception<ArgumentNullException>(() => new DataBlock(new string[] { "ID", "Title" }, null));

            // Null Row
            Verify.Exception<ArgumentNullException>(() => new DataBlock(new string[] { "ID", "Title", "Priority" },
                new Array[] {
                    new object[] { 1, "Sample Value", 2 },
                    new object[] { 3, "Second Value", 3 },
                    null
                }));

            // Null Columns
            Verify.Exception<ArgumentNullException>(() => new DataBlock(new string[] { "ID", "Title" }, 2, null));

            // Null Column
            Verify.Exception<ArgumentNullException>(() => new DataBlock(new string[] { "ID", "Title", "Priority" }, 2,
                new Array[] {
                    new int[] { 1, 2 },
                    new string[] { "Sample Value", "Second Value" },
                    null
                }));
        }

        [TestMethod]
        public void DataBlock_SetRow_WrongColumnCount()
        {
            DataBlock b = new DataBlock(new string[] { "One", "Two", "Three" }, 4);
            Verify.Exception<ArgumentException>(
                    () => b.SetRow(2, new object[] { 1, 2, 3, 4, 5 })
                );
        }

        [TestMethod]
        public void DataBlock_SetColumn_WrongRowCount()
        {
            DataBlock b = new DataBlock(new string[] { "One", "Two", "Three" }, 4);

            // Too many rows is fine
            b.SetColumn(0, new object[] { 1, 2, 3, 4, 5 });
            b.SetColumn(1, new int[] { 10, 11, 12, 13, 14 });
            b.SetColumn(2, new float[] { 1, 2, 3, 4 });

            // Too few rows is not allowed
            Verify.Exception<ArgumentException>(
                    () => b.SetColumn(2, new object[] { 1, 2, 3 })
                );
        }

        [TestMethod]
        public void DataBlock_SetRowCount()
        {
            // Claim four rows and set five, five, four
            DataBlock b = new DataBlock(new string[] { "One", "Two", "Three" }, 4);
            b.SetColumn(0, new object[] { 1, 2, 3, 4, 5 });
            b.SetColumn(1, new int[] { 10, 11, 12, 13, 14 });
            b.SetColumn(2, new float[] { 1, 2, 3, 4 });

            // Verify row and column verification on read
            object unused = 7;
            Verify.Exception<ArgumentOutOfRangeException>(() => unused = b[4, 0]);
            Verify.Exception<ArgumentOutOfRangeException>(() => unused = b[0, 3]);

            // Verify allowed to write values to actual array limit, not rowCount
            b[4, 0] = unused;
            Verify.Exception<ArgumentOutOfRangeException>(() => b[5, 0] = unused);
            Verify.Exception<ArgumentOutOfRangeException>(() => b[0, 3] = unused);

            // SetRowCount down is fine [allows reading last page from a source and then setting the length to the count found]
            b.SetRowCount(3);
            Assert.AreEqual(3, b.RowCount);

            // Verify RowCount is validated when reading values
            Verify.Exception<ArgumentOutOfRangeException>(() => unused = b[3, 0]);

            // Verify allowed to write values to actual array limit, not rowCount
            b[3, 0] = unused;
            Verify.Exception<ArgumentOutOfRangeException>(() => b[5, 0] = unused);

            // SetRowCount back up to [minimum of] actual row counts is fine
            b.SetRowCount(4);
            Assert.AreEqual(4, b.RowCount);

            // Verify newly "unlocked" row allowed and verification above that
            unused = b[3, 0];
            Verify.Exception<ArgumentOutOfRangeException>(() => unused = b[4, 0]);

            // SetRowCount too high for any column is not allowed
            Verify.Exception<ArgumentException>(() => b.SetRowCount(5));
        }

        [TestMethod]
        public void DataBlock_MethodNullChecks()
        {
            DataBlock b = new DataBlock(new string[] { "One", "Two", "Three" }, 4,
                new Array[]
                {
                new object[] { 1, 2, 3, 4, 5 },
                new int[] { 10, 11, 12, 13, 14 },
                new float[] { 1, 2, 3, 4 }
                });

            Verify.Exception<ArgumentNullException>(() => b.SetColumn(0, null));
            Verify.Exception<ArgumentNullException>(() => b.SetRow(0, null));
            Verify.Exception<ArgumentNullException>(() => b.StripToColumns(null));
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;

using Arriba.Serialization.Csv;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Csv
{
    /// <summary>
    /// Summary description for CsvTests
    /// </summary>
    [TestClass]
    public class CsvReaderTests
    {
        [TestMethod]
        public void ParseNoHeaders()
        {
            string content = @"
                             A,B,C
                             D,E,F";

            var settings = new CsvReaderSettings()
            {
                HasHeaders = false
            };

            using (var reader = GetTestReader(content, settings))
            {
                Assert.AreEqual(3, reader.ColumnCount);

                var rows = reader.Rows.ToArray();

                Assert.AreEqual(2, rows.Length);

                Assert.AreEqual("A", rows[0][0].ToString());
                Assert.AreEqual("B", rows[0][1].ToString());
                Assert.AreEqual("C", rows[0][2].ToString());

                Assert.AreEqual("D", rows[1][0].ToString());
                Assert.AreEqual("E", rows[1][1].ToString());
                Assert.AreEqual("F", rows[1][2].ToString());
            }
        }

        [TestMethod]
        public void ParseWithHeadersHeaders()
        {
            string content = @"
                             First,Second,Third
                             A,B,C
                             D,E,F";

            var settings = new CsvReaderSettings()
            {
                HasHeaders = true
            };

            using (var reader = GetTestReader(content, settings))
            {
                Assert.AreEqual(3, reader.ColumnCount);

                var rows = reader.Rows.ToArray();

                Assert.AreEqual(2, rows.Length);

                Assert.AreEqual("A", rows[0][0].ToString());
                Assert.AreEqual("B", rows[0][1].ToString());
                Assert.AreEqual("C", rows[0][2].ToString());

                Assert.AreEqual("D", rows[1][0].ToString());
                Assert.AreEqual("E", rows[1][1].ToString());
                Assert.AreEqual("F", rows[1][2].ToString());

                Assert.AreEqual("First", reader.ColumnNames[0]);
                Assert.AreEqual("Second", reader.ColumnNames[1]);
                Assert.AreEqual("Third", reader.ColumnNames[2]);
            }
        }

        [TestMethod]
        public void ParseQuoted()
        {
            string content = @"
                             ""A"",""B"",""C""
                             ""D"",""E"",""F""";

            var settings = new CsvReaderSettings()
            {
                HasHeaders = false
            };

            using (var reader = GetTestReader(content, settings))
            {
                Assert.AreEqual(3, reader.ColumnCount);

                var rows = reader.Rows.ToArray();

                Assert.AreEqual(2, rows.Length);

                Assert.AreEqual("A", rows[0][0].ToString());
                Assert.AreEqual("B", rows[0][1].ToString());
                Assert.AreEqual("C", rows[0][2].ToString());

                Assert.AreEqual("D", rows[1][0].ToString());
                Assert.AreEqual("E", rows[1][1].ToString());
                Assert.AreEqual("F", rows[1][2].ToString());
            }
        }

        [TestMethod]
        public void ParseDoubleQuoted()
        {
            // This is realy """A""","B","C" 
            string content = @"""""""A"""""",""B"",""C""";

            var settings = new CsvReaderSettings()
            {
                HasHeaders = false
            };

            using (var reader = GetTestReader(content, settings))
            {
                Assert.AreEqual(3, reader.ColumnCount);

                var rows = reader.Rows.ToArray();


                Assert.AreEqual("\"A\"", rows[0][0].ToString());
                Assert.AreEqual("B", rows[0][1].ToString());
                Assert.AreEqual("C", rows[0][2].ToString());
            }
        }

        [TestMethod]
        public void ParseQuotedNewLine()
        {
            // This is realy """A""","B","C" 
            string content = "\"A\r\nA\",\"B\",\"C\"";

            var settings = new CsvReaderSettings()
            {
                HasHeaders = false
            };

            using (var reader = GetTestReader(content, settings))
            {
                Assert.AreEqual(3, reader.ColumnCount);

                var rows = reader.Rows.ToArray();


                Assert.AreEqual("A\r\nA", rows[0][0].ToString());
                Assert.AreEqual("B", rows[0][1].ToString());
                Assert.AreEqual("C", rows[0][2].ToString());
            }
        }

        [TestMethod]
        public void ParseWithRowNumber()
        {
            // This is realy """A""","B","C" 
            string content = "A,B,C";

            var settings = new CsvReaderSettings()
            {
                HasHeaders = false,
                IncludeRowNumberAsColumn = true
            };

            using (var reader = GetTestReader(content, settings))
            {
                Assert.AreEqual(4, reader.ColumnCount);

                var rows = reader.Rows.ToArray();


                Assert.AreEqual("1", rows[0][0].ToString());
                Assert.AreEqual("A", rows[0][1].ToString());
                Assert.AreEqual("B", rows[0][2].ToString());
                Assert.AreEqual("C", rows[0][3].ToString());
            }
        }

        [TestMethod]
        public void ParseWithRowNumberAndHeaders()
        {
            // This is realy """A""","B","C" 
            string content = "C1,C2,C3\r\nA,B,C";

            var settings = new CsvReaderSettings()
            {
                HasHeaders = true,
                IncludeRowNumberAsColumn = true,
                RowNumberColumnName = "RowNumber"
            };

            using (var reader = GetTestReader(content, settings))
            {
                Assert.AreEqual(4, reader.ColumnCount);

                var rows = reader.Rows.ToArray();

                Assert.AreEqual("RowNumber", reader.ColumnNames[0]);

                Assert.AreEqual("1", rows[0][0].ToString());
                Assert.AreEqual("A", rows[0][1].ToString());
                Assert.AreEqual("B", rows[0][2].ToString());
                Assert.AreEqual("C", rows[0][3].ToString());
            }
        }

        private static CsvReader GetTestReader(string content, CsvReaderSettings settings = null)
        {
            return new CsvReader(StreamFromString(content), settings);
        }


        private static MemoryStream StreamFromString(string content)
        {
            MemoryStream ms = new MemoryStream();

            using (StreamWriter writer = new StreamWriter(ms, Encoding.UTF8, 1024, true))
            {
                foreach (var line in content.Split('\n'))
                {
                    var finalLine = line.Trim().TrimEnd('\r');

                    if (String.IsNullOrWhiteSpace(finalLine))
                    {
                        continue;
                    }

                    writer.WriteLine(finalLine);
                }
            }

            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}

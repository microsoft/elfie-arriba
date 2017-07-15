// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

using Arriba.Serialization;
using Arriba.Serialization.Csv;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Serialization
{
    [TestClass]
    public class CsvWriterTests
    {
        [TestMethod]
        public void CsvWriter_Basic()
        {
            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                using (CsvWriter writer = new CsvWriter(context, new string[] { "ID", "Changed Date", "Title" }))
                {
                    writer.AppendRow(new object[] { 1519, new DateTime(2013, 12, 29, 0, 0, 0, DateTimeKind.Utc), "Value with no escaping." });
                    writer.AppendRow(new object[] { 1520, new DateTime(2013, 12, 30, 0, 0, 0, DateTimeKind.Utc), "Value with quote \"escaping\"." });
                    writer.AppendRow(new object[] { 1521, new DateTime(2013, 12, 31, 0, 0, 0, DateTimeKind.Utc), "Value, escaping required." });
                    writer.AppendRow(new object[] { 1522, new DateTime(2014, 01, 01, 0, 0, 0, DateTimeKind.Utc), "Value, escaping and \"quote wrapping\" required." });
                    writer.AppendRow(new object[] { 1523, new DateTime(2014, 01, 02, 0, 0, 0, DateTimeKind.Utc), "Value\r\nrequiring escaping." });
                    writer.AppendRow(new object[] { 1524, new DateTime(2014, 01, 03, 0, 0, 0, DateTimeKind.Utc), (ByteBlock)"ByteBlock Value" });

                    string expected =
@"ID,Changed Date,Title
1519,2013-12-29 00:00:00Z,Value with no escaping.
1520,2013-12-30 00:00:00Z,""Value with quote """"escaping"""".""
1521,2013-12-31 00:00:00Z,""Value, escaping required.""
1522,2014-01-01 00:00:00Z,""Value, escaping and """"quote wrapping"""" required.""
1523,2014-01-02 00:00:00Z,""Value
requiring escaping.""
1524,2014-01-03 00:00:00Z,ByteBlock Value
";
                    string actual = GetStreamContent(context);
                    Verify.AreStringsEqual(expected, actual);
                }

                // Verify CsvWriter.Dispose disposed the stream
                Assert.IsNull(context.Stream);
            }
        }

        [TestMethod]
        public void CsvWriter_Append()
        {
            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                CsvWriter writer;

                // Write one row
                writer = new CsvWriter(context, new string[] { "ID", "Changed Date", "Title" });
                writer.AppendRow(new object[] { 1520, new DateTime(2013, 12, 30, 0, 0, 0, DateTimeKind.Utc), "Value with no escaping." });

                // Create another writer to append a second row
                writer = new CsvWriter(context, new string[] { "ID", "Changed Date", "Title" });
                writer.AppendRow(new object[] { 1521, new DateTime(2013, 12, 31, 0, 0, 0, DateTimeKind.Utc), "Value, escaping required." });

                string expected =
@"ID,Changed Date,Title
1520,2013-12-30 00:00:00Z,Value with no escaping.
1521,2013-12-31 00:00:00Z,""Value, escaping required.""
";
                string actual = GetStreamContent(context);
                Verify.AreStringsEqual(expected, actual);
            }
        }

        [TestMethod]
        public void CsvWriter_Types()
        {
            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                using (CsvWriter writer = new CsvWriter(context, new string[] { "Value" }))
                {
                    writer.AppendRow(new object[] { null });
                    writer.AppendRow(new object[] { true });
                    writer.AppendRow(new object[] { (byte)127 });
                    writer.AppendRow(new object[] { new DateTime(2013, 01, 01, 0, 0, 0, DateTimeKind.Utc) });
                    writer.AppendRow(new object[] { "String" });
                    writer.AppendRow(new object[] { (ByteBlock)"ByteBlock" });
                    writer.AppendRow(new object[] { new byte[] { 49, 50, 51 } });
                    writer.AppendRow(new object[] { Guid.Empty });

                    string expected =
@"Value

True
127
2013-01-01 00:00:00Z
String
ByteBlock
123
00000000-0000-0000-0000-000000000000
";
                    string actual = GetStreamContent(context);
                    Verify.AreStringsEqual(expected, actual);
                }
            }
        }

        [TestMethod]
        public void CsvWriter_EveryByte()
        {
            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                using (CsvWriter writer = new CsvWriter(context, new string[] { "Value" }))
                {
                    long valueStartPosition = context.Stream.Position;

                    byte[] allValues = new byte[256];
                    for (int i = 0; i < allValues.Length; ++i)
                    {
                        allValues[i] = (byte)i;
                    }

                    writer.AppendRow(new object[] { allValues });

                    long valueEndPosition = context.Stream.Position;

                    context.Stream.Seek(valueStartPosition, SeekOrigin.Begin);
                    byte[] writtenBytes = context.Reader.ReadBytes((int)(valueEndPosition - valueStartPosition));

                    // Verify 261 bytes - 256 values, wrapping quotes, escaped quote, and CRLF terminator
                    Assert.AreEqual(261, writtenBytes.Length);

                    // Verify wrapping quotes
                    Assert.AreEqual(UTF8.DoubleQuote, writtenBytes[0]);
                    Assert.AreEqual(UTF8.DoubleQuote, writtenBytes[writtenBytes.Length - 3]);

                    // Verify doubled quote
                    Assert.AreEqual(UTF8.DoubleQuote, writtenBytes[1 + UTF8.DoubleQuote]);
                    Assert.AreEqual(UTF8.DoubleQuote, writtenBytes[2 + UTF8.DoubleQuote]);
                }
            }
        }

        [TestMethod]
        public void CsvWriter_NotEnoughColumns()
        {
            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                using (CsvWriter writer = new CsvWriter(context, new string[] { "ID", "Changed Date", "Title" }))
                {
                    Verify.Exception<InvalidOperationException>(
                            () => writer.AppendRow(new object[] { 1520, new DateTime(2013, 12, 30, 0, 0, 0, DateTimeKind.Utc) })
                        );
                }
            }
        }

        [TestMethod]
        public void CsvWriter_TooManyColumns()
        {
            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                using (CsvWriter writer = new CsvWriter(context, new string[] { "ID", "Changed Date", "Title" }))
                {
                    Verify.Exception<InvalidOperationException>(
                            () => writer.AppendRow(new object[] { 1520, new DateTime(2013, 12, 30, 0, 0, 0, DateTimeKind.Utc), true, Guid.NewGuid() })
                        );
                }
            }
        }

        [TestMethod]
        public void CsvWriter_AppendHeaderMismatch()
        {
            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                CsvWriter writer;

                // Write one row
                writer = new CsvWriter(context, new string[] { "ID", "Changed Date", "Title" });
                writer.AppendRow(new object[] { 1520, new DateTime(2013, 12, 30, 0, 0, 0, DateTimeKind.Utc), "Value with no \"escaping\"." });

                // Create another writer to append a second row (wrong header)
                Verify.Exception<IOException>(
                        () => { writer = new CsvWriter(context, new string[] { "ID", "Changed Date", "Description" }); }
                    );
            }
        }

        [TestMethod]
        public void CsvWriter_AppendBadTerminator()
        {
            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                CsvWriter writer;

                // Write one row
                writer = new CsvWriter(context, new string[] { "ID", "Changed Date", "Title" });
                writer.AppendRow(new object[] { 1520, new DateTime(2013, 12, 30), "Value with no \"escaping\"." });

                // Write an incomplete second row [directly; CsvWriter now only flushes on row boundaries]
                context.Writer.Write("1251");

                // Create another writer to append a second row (last row not terminated)
                Verify.Exception<IOException>(
                        () => { writer = new CsvWriter(context, new string[] { "ID", "Changed Date", "Title" }); }
                    );
            }
        }

        private static string GetStreamContent(ISerializationContext context)
        {
            context.Stream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(context.Stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}

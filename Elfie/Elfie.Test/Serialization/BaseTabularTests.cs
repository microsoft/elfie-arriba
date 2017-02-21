// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Serialization
{
    [TestClass]
    public class BaseTabularTests
    {
        [TestMethod]
        public void CsvReaderWriter_BaseTests()
        {
            ReaderWriterAll("Sample.csv", (stream) => new CsvWriter(stream), (filePath, hasHeaderRow) => new CsvReader(filePath, hasHeaderRow));
        }

        [TestMethod]
        public void TsvReaderWriter_BaseTests()
        {
            ReaderWriterAll("Sample.tsv", (stream) => new TsvWriter(stream), (filePath, hasHeaderRow) => new TsvReader(filePath, hasHeaderRow));
        }

        public void ReaderWriterAll(string sampleFilePath, Func<Stream, ITabularWriter> buildWriter, Func<string, bool, ITabularReader> buildReader)
        {
            if (!File.Exists(sampleFilePath))
            {
                WriteSampleFileWithIssues(new FileStream(sampleFilePath, FileMode.Create, FileAccess.ReadWrite), buildWriter);
            }

            Reader_Basics(sampleFilePath, buildReader);
            Reader_NewlineVariations(buildWriter, buildReader);
            Reader_Roundtrip(buildReader, buildWriter);

#if PERFORMANCE
            Reader_Performance(sampleFilePath, buildReader);
            Writer_Performance(buildWriter);
#endif
        }

        private static void WriteSampleFileWithIssues(Stream stream, Func<Stream, ITabularWriter> buildWriter)
        {
            Random r = new Random();
            string huge = new string('Z', 100000);
            String8 huge8 = String8.Convert(huge, new byte[String8.GetLength(huge)]);
            String8 abcdef = String8.Convert("ABCDEF", new byte[6]);

            using (ITabularWriter writer = buildWriter(stream))
            {
                writer.SetColumns(new string[] { "LineNumber", "Count", "Description" });

                for (int i = writer.RowCountWritten + 1; i <= 10000; ++i)
                {
                    if (i % 100 == 99)
                    {
                        // Write an empty row (1/100)
                        long rowStartPosition = stream.Position;

                        // Make the writer think everything is ok (it'll throw if you don't write enough values)
                        writer.Write(String8.Empty);
                        writer.Write(String8.Empty);

                        writer.WriteValueStart();
                        writer.WriteValueEnd();

                        // Wipe out what was written
                        stream.Seek(rowStartPosition, SeekOrigin.Begin);
                    }
                    else if (i == 5000)
                    {
                        // Write a huge row
                        writer.Write(i);

                        writer.WriteValueStart();
                        writer.WriteValuePart(r.Next(100000));
                        writer.WriteValueEnd();

                        writer.Write(huge8);
                    }
                    else
                    {
                        // Write a normal row
                        writer.Write(i);
                        writer.Write(r.Next(100000));
                        writer.Write(abcdef);
                    }

                    writer.NextRow();
                }
            }
        }

        private static void WriteValidSample(Stream stream, Func<Stream, ITabularWriter> buildWriter)
        {
            String8Block block = new String8Block();
            String8 simple = block.GetCopy("Simple");
            String8 commasAndQuotes = block.GetCopy("Value, but with \"quotes\" and commas");

            using (ITabularWriter writer = buildWriter(stream))
            {
                writer.SetColumns(new string[] { "LineNumber", "Count", "Description" });

                for (int i = 2; i < 10; ++i)
                {
                    writer.Write(i);
                    writer.Write(simple);
                    writer.Write(commasAndQuotes);

                    writer.NextRow();
                }
            }
        }

        public void Reader_Basics(string sampleFilePath, Func<string, bool, ITabularReader> buildReader)
        {
            // File Not Found
            Verify.Exception<FileNotFoundException>(() => buildReader("NonExistantFile.xsv", false));

            // Empty File
            File.WriteAllText("Empty.xsv", "");

            // Verify Reader throws on construction if trying to read headers
            Verify.Exception<IOException>(() => buildReader("Empty.xsv", true));

            // Verify Reader returns false immediately if not reading headers
            using (ITabularReader r = buildReader("Empty.xsv", false))
            {
                Assert.IsFalse(r.NextRow());
            }

            // Verify Reader doesn't consume header row if asked not to
            using (ITabularReader r = buildReader(sampleFilePath, false))
            {
                Assert.IsTrue(r.NextRow());
                Assert.AreEqual("LineNumber", r.Current[0].ToString());

                // Get column name (no header row read)
                Verify.Exception<ColumnNotFoundException>(() => r.ColumnIndex("Missing"));
            }

            // Open the sample Tsv the 'expected' way
            using (ITabularReader r = buildReader(sampleFilePath, true))
            {
                // Get column name (valid)
                int lineNumberColumnIndex = r.ColumnIndex("LineNumber");
                Assert.AreEqual(0, lineNumberColumnIndex, "LineNumber column not expected");

                // Get column name (different case, but valid)
                int descriptionColumnIndex = r.ColumnIndex("deSCRiption");
                Assert.AreEqual(2, descriptionColumnIndex, "Description column not expected");

                // Get column name (unknown)
                Verify.Exception<ColumnNotFoundException>(() => r.ColumnIndex("UnknownColumn"));

                while (r.NextRow())
                {
                    int rowIndex = r.RowCountRead;

                    if (rowIndex % 100 == 99)
                    {
                        // Verify empty rows return no columns, have empty row text, throw on value access
                        Assert.AreEqual(0, r.CurrentRowColumns, "Expected column count 0 in empty rows");
                        Assert.IsTrue(r.Current.Value.IsEmpty());
                        Verify.Exception<ArgumentOutOfRangeException>(() => { var v = r.Current[lineNumberColumnIndex]; });
                    }
                    else if (rowIndex == 5000)
                    {
                        // Read row over 64k [block resizing logic, row values look right]
                        String8 longDescription = r.Current[descriptionColumnIndex];
                        Assert.AreEqual(100000, longDescription.Length);
                    }
                    else
                    {
                        // Get value (valid)
                        String8 lineNumber8 = r.Current[lineNumberColumnIndex];
                        int lineNumber = 0;
                        if (lineNumber8.TryToInteger(out lineNumber))
                        {
                            Assert.AreEqual(rowIndex, lineNumber, "Expected line number to equal row number");
                        }
                        else
                        {
                            Assert.Fail(String.Format("\"{0}\" was not converted to an integer.", lineNumber8));
                        }

                        // Get line number
                        Assert.AreEqual(rowIndex, r.RowCountRead, "Expected lines read to equal row number");

                        // Get row text (valid)
                        String8 fullRow = r.Current.Value;
                    }
                }
            }
        }

        public void Reader_Roundtrip(Func<string, bool, ITabularReader> buildReader, Func<Stream, ITabularWriter> buildWriter)
        {
            string filePath = "ValidSample.xsv";

            // Write a valid file with some values which require CSV escaping
            WriteValidSample(new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite), buildWriter);

            // Direct Copy the file from the reader to the writer - every value unescaped and then escaped
            using (ITabularReader reader = buildReader(filePath, true))
            {
                using (ITabularWriter writer = buildWriter(new FileStream(filePath + ".new", FileMode.Create, FileAccess.ReadWrite)))
                {
                    writer.SetColumns(reader.Columns);

                    while (reader.NextRow())
                    {
                        for (int i = 0; i < reader.CurrentRowColumns; ++i)
                        {
                            writer.Write(reader.Current[i]);
                        }

                        writer.NextRow();
                    }
                }
            }

            // Verify files are identical
            string fileBefore = File.ReadAllText(filePath);
            string fileAfter = File.ReadAllText(filePath + ".new");
            Assert.AreEqual(fileBefore, fileAfter);
        }

        public void Reader_Performance(string sampleFilePath, Func<string, bool, ITabularReader> buildReader)
        {
            long rowCountRead = 0;
            long xsvLengthBytes = new FileInfo(sampleFilePath).Length;

            // Goal: 100MB/sec [Surface Book i7]
            Verify.PerformanceByBytes(50 * LongExtensions.Megabyte, () =>
            {
                int iterations = 100;
                for (int iteration = 0; iteration < iterations; ++iteration)
                {
                    using (ITabularReader r = buildReader(sampleFilePath, true))
                    {
                        int lineNumberIndex = r.ColumnIndex("LineNumber");
                        int countIndex = r.ColumnIndex("Count");
                        int descriptionIndex = r.ColumnIndex("Description");

                        while (r.NextRow())
                        {
                            rowCountRead++;

                            if (r.CurrentRowColumns < 2) continue;

                            String8 lineNumber8 = r.Current[lineNumberIndex];
                            int lineNumber;
                            lineNumber8.TryToInteger(out lineNumber);

                            // TODO: Get ToInteger fast enough to read overall at goal
                            String8 count8 = r.Current[countIndex];
                            //int count = count8.ToInteger();

                            String8 description = r.Current[descriptionIndex];
                        }
                    }
                }

                return iterations * xsvLengthBytes;
            });
        }

        public void Writer_RowValidation(Func<string, ITabularWriter> buildWriter)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (ITabularWriter writer = buildWriter("Writer_RowValidation.xsv"))
                {
                    writer.SetColumns(new string[] { "LineNumber", "Count", "Description", "Source" });

                    writer.Write(1);
                    writer.Write(2);

                    // Verify exception if too few columns are written
                    Verify.Exception<InvalidOperationException>(writer.NextRow);

                    writer.Write(3);
                    writer.Write(4);

                    // Verify exception if too many columns written
                    Verify.Exception<InvalidOperationException>(() => writer.Write(5));

                    // No trailing NextRow()
                }
            }

            // Verify last row terminated (despite no trailing NextRow)
            string content = File.ReadAllText("Writer_RowValidation.xsv");
            Assert.IsTrue(content.EndsWith("\r\n"));
        }

        public void Writer_Performance(Func<Stream, ITabularWriter> buildWriter)
        {
            String8Block block = new String8Block();
            String8 d1 = block.GetCopy("Description 1");
            String8 d2 = block.GetCopy("Description 2");
            String8 s1 = block.GetCopy("Source: Internal");
            String8 s2 = block.GetCopy("Source: External");

            using (MemoryStream s = new MemoryStream())
            {
                int iterations = 50;
                long bytesWritten = 0;
                int rowsWritten = 0;

                // Tsv Write goal: 100MB/sec [Surface Book]
                // NOTE: Tsv Write performance is very sensitive the mix of text and numbers written. Writing integers is slower.
                Verify.PerformanceByBytes(50 * LongExtensions.Megabyte, () =>
                {
                    for (int iteration = 0; iteration < iterations; ++iteration)
                    {
                        s.Seek(0, SeekOrigin.Begin);

                        ITabularWriter writer = buildWriter(s);
                        writer.SetColumns(new string[] { "LineNumber", "Count", "Description", "Source" });

                        int sum = 0;
                        for (int row = 1; row < 10000; ++row)
                        {
                            sum += row;

                            writer.Write(row);
                            writer.Write(sum);

                            if (row % 2 == 0)
                            {
                                writer.Write(d1);
                                writer.Write(s1);
                            }
                            else
                            {
                                writer.Write(d2);
                                writer.Write(s2);
                            }

                            writer.NextRow();
                        }

                        bytesWritten += writer.BytesWritten;
                        rowsWritten += writer.RowCountWritten;
                    }

                    return bytesWritten;
                });
            }
        }

        public void Reader_NewlineVariations(Func<Stream, ITabularWriter> buildWriter, Func<string, bool, ITabularReader> buildReader)
        {
            string xsvPath = "NewlineVariations.xsv";
            Stream stream = new FileStream(xsvPath, FileMode.Create, FileAccess.ReadWrite);
            using (ITabularWriter w = buildWriter(stream))
            {
                w.SetColumns(new string[] { "One", "Two", "Three" });

                for (int row = 0; row < 3; ++row)
                {
                    w.Write(3 * row + 1);
                    w.Write(3 * row + 2);
                    w.Write(3 * row + 3);

                    // Write the end of row but then override it
                    long position = stream.Position;
                    w.NextRow();

                    if (row == 0)
                    {
                        // Row 0 - newline only
                        stream.Seek(position, SeekOrigin.Begin);
                        stream.WriteByte(UTF8.Newline);
                    }
                    else if (row == 2)
                    {
                        // Row 2 - no end of line
                        stream.SetLength(position);
                    }
                }
            }

            using (ITabularReader r = buildReader(xsvPath, true))
            {
                // Verify column heading not clipped even though no '\r'
                Assert.AreEqual("Three", r.Columns[2]);

                Assert.IsTrue(r.NextRow());
                Assert.AreEqual(3, r.CurrentRowColumns);

                // Verify last column doesn't have extra '\r' when terminated with '\r\n'
                Assert.AreEqual("3", r.Current[2].ToString());

                Assert.IsTrue(r.NextRow());
                Assert.AreEqual(3, r.CurrentRowColumns);

                // Verify last column not clipped when terminated with '\n'
                Assert.AreEqual("6", r.Current[2].ToString());

                Assert.IsTrue(r.NextRow());
                Assert.AreEqual(3, r.CurrentRowColumns);

                // Verify last column not clipped when unterminated [EOF]
                Assert.AreEqual("9", r.Current[2].ToString());

                Assert.IsFalse(r.NextRow(), "Reader didn't stop after last line without newline");
            }
        }
    }
}

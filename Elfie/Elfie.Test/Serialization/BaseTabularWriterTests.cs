using Elfie.Test;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Test.Serialization
{
    [TestClass]
    public class BaseTabularWriterTests
    {
        [TestMethod]
        public void CsvWriter_BaseTests()
        {
            BaseTabularWriter_RowValidation((path, columns) => new CsvWriter(path, columns));

#if !DEBUG
            Writer_Performance((stream, columns) => new CsvWriter(stream, columns));
#endif
        }

        [TestMethod]
        public void TsvWriter_BaseTests()
        {
            BaseTabularWriter_RowValidation((path, columns) => new TsvWriter(path, columns));

#if !DEBUG
            Writer_Performance((stream, columns) => new TsvWriter(stream, columns));
#endif
        }

        public void BaseTabularWriter_RowValidation(Func<string, IEnumerable<string>, BaseTabularWriter> buildWriter)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BaseTabularWriter writer = buildWriter("BaseTabularWriter_RowValidation.xsv", new string[] { "LineNumber", "Count", "Description", "Source" }))
                {
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
            string content = File.ReadAllText("BaseTabularWriter_RowValidation.xsv");
            Assert.IsTrue(content.EndsWith("\r\n"));
        }

        public void Writer_Performance(Func<Stream, IEnumerable<string>, BaseTabularWriter> buildWriter)
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

                // Tsv Write goal: 100MB/sec
                // NOTE: Tsv Write performance is very sensitive the mix of text and numbers written. Writing integers is slower.
                Verify.PerformanceByBytes(100 * LongExtensions.Megabyte, () =>
                {
                    for (int iteration = 0; iteration < iterations; ++iteration)
                    {
                        s.Seek(0, SeekOrigin.Begin);

                        BaseTabularWriter writer = buildWriter(s, new string[] { "LineNumber", "Count", "Description", "Source" });

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
                        rowsWritten += writer.LineNumber;
                    }

                    return bytesWritten;
                });
            }
        }
    }
}

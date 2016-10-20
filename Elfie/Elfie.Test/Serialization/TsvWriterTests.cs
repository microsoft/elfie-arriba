// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Serialization
{
    [TestClass]
    public class TsvWriterTests
    {
        [TestMethod]
        public void TsvWriter_Basics()
        {
            String8Block block = new String8Block();
            String8 valueNoEscaping = block.GetCopy("Sample Description");
            String8 valueEscaping = block.GetCopy("Value\tWith\nIssues");

            using (TsvWriter writer = new TsvWriter("TsvWriter.tsv", new string[] { "LineNumber", "Count", "Description", "Source" }))
            {
                Assert.AreEqual(1, writer.RowNumber);

                int sum = 0;
                for (int i = 1; i <= 10; ++i)
                {
                    Assert.AreEqual(i, writer.RowNumber);

                    sum += i;
                    writer.Write(i);
                    writer.Write(sum);
                    writer.Write(valueNoEscaping);
                    writer.Write(valueEscaping);

                    writer.NextRow();
                }
            }

            string tsvContent = File.ReadAllText("TsvWriter.tsv");

            // Verify header is as expected
            Assert.IsTrue(tsvContent.StartsWith("LineNumber\tCount\tDescription\tSource\r\n"));

            // Verify illegal characters are stripped
            Assert.IsTrue(tsvContent.Contains("ValueWithIssues"));

            // Verify the first row fully
            Assert.IsTrue(tsvContent.Contains("1\t1\tSample Description\tValueWithIssues\r\n"));
        }

        [TestMethod]
        public void TsvWriter_RowValidation()
        {
            using (TsvWriter writer = new TsvWriter("TsvWriter_RowValidation.tsv", new string[] { "LineNumber", "Count", "Description", "Source" }))
            {
                writer.Write(1);
                writer.Write(2);

                // Verify exception if too few columns are written
                Verify.Exception<TsvWriterException>(writer.NextRow);

                writer.Write(3);
                writer.Write(4);

                // Verify exception if too many columns written
                Verify.Exception<TsvWriterException>(() => writer.Write(5));

                // No trailing NextRow()
            }

            // Verify last row terminated (despite no trailing NextRow)
            string content = File.ReadAllText("TsvWriter_RowValidation.tsv");
            Assert.IsTrue(content.EndsWith("\r\n"));
        }

#if !DEBUG
        [TestMethod]
#endif
        public void TsvWriter_Performance()
        {
            String8Block block = new String8Block();
            String8 d1 = block.GetCopy("Description 1");
            String8 d2 = block.GetCopy("Description 2");
            String8 s1 = block.GetCopy("Source: Internal");
            String8 s2 = block.GetCopy("Source: External");

            Stopwatch w = Stopwatch.StartNew();
            long bytesWritten = 0;
            int rowsWritten = 0;

            int iterations = 50;
            using (MemoryStream s = new MemoryStream())
            {
                for (int iteration = 0; iteration < iterations; ++iteration)
                {
                    s.Seek(0, SeekOrigin.Begin);

                    TsvWriter writer = new TsvWriter(s, new string[] { "LineNumber", "Count", "Description", "Source" });

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
                    rowsWritten += writer.RowNumber;
                }
            }
            w.Stop();

            // Tsv Write goal: 100MB/sec [100KB/ms]
            // NOTE: Tsv Write performance is very sensitive the mix of text and numbers written. Writing integers is slower.
            long targetMilliseconds = bytesWritten / 100000;
            Trace.WriteLine(String.Format("Elfie TsvWriter wrote {0} ({1:n0} rows) in {2} [goal {3}ms]", bytesWritten.SizeString(), rowsWritten, w.Elapsed.ToFriendlyString(), targetMilliseconds));
            Assert.IsTrue(w.ElapsedMilliseconds < targetMilliseconds);
        }
    }
}

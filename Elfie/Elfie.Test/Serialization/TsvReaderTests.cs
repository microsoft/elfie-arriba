using Elfie.Test;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Test.Serialization
{
    [TestClass]
    public class TsvReaderTests
    {
        private const string SampleTsvPath = @"..\..\Sample.tsv";

        private static void WriteSampleTsv()
        {
            if (File.Exists(SampleTsvPath)) return;

            Random r = new Random();

            using (StreamWriter writer = new StreamWriter(SampleTsvPath))
            {
                writer.Write("LineNumber\tCount\tDescription\n");
                for (int i = 0; i < 10000; ++i)
                {
                    if (i % 100 == 99)
                    {
                        // Write an empty row (1/100)
                        writer.Write("\n");
                    }
                    else if (i == 5000)
                    {
                        // Write a huge row
                        writer.Write(string.Format("{0}\t{1}\t{2}\t{3}\n", 10002, r.Next(100000), new string('Z', 100000), "Extra"));
                    }
                    else
                    {
                        // Write a normal row
                        writer.Write(string.Format("{0}\t{1}\t{2}\n", i + 2, r.Next(100000), "ABCDEF"));
                    }
                }
            }
        }

        [TestMethod]
        public void TsvReader_Basics()
        {
            WriteSampleTsv();

            // File Not Found
            Verify.Exception<FileNotFoundException>(() => new TsvReader("NonExistantFile.tsv", false));

            // Empty File
            File.WriteAllText("Empty.tsv", "");

            // Verify TsvReader throws on construction if trying to read headers
            Verify.Exception<IOException>(() => new TsvReader("Empty.tsv", true));

            // Verify TsvReader returns false immediately if not reading headers
            using (TsvReader r = new TsvReader("Empty.tsv", false))
            {
                Assert.IsFalse(r.NextRow());
            }

            // Verify TsvReader doesn't consume header row if asked not to
            using (TsvReader r = new TsvReader(SampleTsvPath, false))
            {
                Assert.IsTrue(r.NextRow());
                Assert.AreEqual("LineNumber", r.CurrentRow(0).ToString());

                // Get column name (no header row read)
                Verify.Exception<ColumnNotFoundException>(() => r.ColumnIndex("Missing"));
            }

            // Open the sample Tsv the 'expected' way
            using (TsvReader r = new TsvReader(SampleTsvPath, true))
            {
                // Get column name (valid)
                int lineNumberColumnIndex = r.ColumnIndex("LineNumber");
                Assert.AreEqual(0, lineNumberColumnIndex);

                // Get column name (different case, but valid)
                int descriptionColumnIndex = r.ColumnIndex("deSCRiption");
                Assert.AreEqual(2, descriptionColumnIndex);

                // Get column name (unknown)
                Verify.Exception<ColumnNotFoundException>(() => r.ColumnIndex("UnknownColumn"));

                int rowIndex = 0;
                while (r.NextRow())
                {
                    if (rowIndex % 100 == 99)
                    {
                        // Verify empty rows return no columns, have empty row text, throw on value access
                        Assert.AreEqual(0, r.CurrentRowColumns);
                        Assert.IsTrue(r.CurrentRowText.IsEmpty());
                        Verify.Exception<ArgumentOutOfRangeException>(() => r.CurrentRow(lineNumberColumnIndex));
                    }
                    else if (rowIndex == 5000)
                    {
                        // Read row over 64k [block resizing logic, row values look right]
                        String8 longDescription = r.CurrentRow(descriptionColumnIndex);
                        Assert.AreEqual(100000, longDescription.Length);

                        // Read column outside header row range (allowed, not corrupted)
                        String8 extra = r.CurrentRow(3);
                        Assert.AreEqual("Extra", extra.ToString());
                    }
                    else
                    {
                        // Get value (valid)
                        String8 lineNumber8 = r.CurrentRow(lineNumberColumnIndex);
                        int lineNumber = lineNumber8.ToInteger();
                        Assert.AreEqual(rowIndex + 2, lineNumber);

                        // Get line number
                        Assert.AreEqual(rowIndex + 2, r.CurrentRowNumber);

                        // Get row text (valid)
                        String8 fullRow = r.CurrentRowText;

                    }

                    rowIndex++;
                }
            }

        }

#if !DEBUG
        [TestMethod]
#endif
        public void TsvReader_Performance()
        {
            WriteSampleTsv();

            long rowCountRead = 0;
            long tsvLengthBytes = new FileInfo(SampleTsvPath).Length;

            Stopwatch w = Stopwatch.StartNew();
            int iterations = 100;
            for(int iteration = 0; iteration < iterations; ++iteration)
            {
                using (TsvReader r = new TsvReader(SampleTsvPath, true))
                {
                    int lineNumberIndex = r.ColumnIndex("LineNumber");
                    int countIndex = r.ColumnIndex("Count");
                    int descriptionIndex = r.ColumnIndex("Description");

                    while(r.NextRow())
                    {
                        rowCountRead++;

                        if (r.CurrentRowColumns < 2) continue;

                        String8 lineNumber8 = r.CurrentRow(lineNumberIndex);
                        int lineNumber = lineNumber8.ToInteger();

                        // TODO: Get ToInteger fast enough to read overall at goal
                        String8 count8 = r.CurrentRow(countIndex);
                        //int count = count8.ToInteger();

                        String8 description = r.CurrentRow(descriptionIndex);
                    }
                }
            }
            w.Stop();

            // Tsv read goal: 125MB/sec [125KB/ms]
            long targetMilliseconds = tsvLengthBytes * iterations / 125000;
            Trace.WriteLine(String.Format("Elfie TsvReader read {0} ({1:n0} rows) in {2} [goal {3}ms]", (tsvLengthBytes * iterations).SizeString(), rowCountRead, w.Elapsed.ToFriendlyString(), targetMilliseconds));
            Assert.IsTrue(w.ElapsedMilliseconds < targetMilliseconds);
        }
    }
}

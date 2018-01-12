// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Serialization;

using XForm.Data;
using XForm.Extensions;
using XForm.Types;
using XForm.Verbs;

namespace XForm
{
    public class PerformanceComparisons
    {
        private const int DefaultMeasureMilliseconds = 2500;

        private WorkflowContext Context { get; set; }
        private int Count { get; set; }
        private ushort[] Values { get; set; }
        private ushort[] Thresholds { get; set; }

        public PerformanceComparisons()
        {
            Context = new WorkflowContext();
            Random r = new Random();
            Count = 50 * 1000 * 1000;

            // Two 50M item arrays - _values is 0-999, _thresholds is always 50
            Values = new ushort[Count];
            Thresholds = new ushort[Count];

            for (int i = 0; i < Values.Length; ++i)
            {
                Values[i] = (ushort)r.Next(1000);
                Thresholds[i] = 50;
            }
        }

        public void Run()
        {
            //WhereUShortUnderConstant();
            //WhereUShortEqualsUshort();
            //ByteEqualsConstant();
            //DoubleWhere();
            //Join();
            //Dictionary();
            Choose();
            //TsvSplit();
        }

        public void WhereUShortUnderConstant()
        {
            using (Benchmarker b = new Benchmarker($"ushort[{Count:n0}] | where [Value] <= 50 | count", DefaultMeasureMilliseconds))
            {
                b.Measure("For Count", Values.Length, () =>
                {
                    int count = 0;
                    for (int i = 0; i < Values.Length; ++i)
                    {
                        if (Values[i] <= 50) count++;
                    }
                    return count;
                });

                b.Measure("Linq Count", Values.Length, () =>
                {
                    return Values.Where((i) => i <= 50).Count();
                });

                b.Measure("XForm Count", Values.Length, () =>
                {
                    return (int)XFormTable.FromArrays(Values.Length)
                    .WithColumn("Value", Values)
                    .Query("where [Value] <= 50", Context)
                    .Count();
                });

                b.AssertResultsEqual();
            }
        }

        public void WhereUShortEqualsUshort()
        {
            using (Benchmarker b = new Benchmarker($"ushort[{Count:n0}] | where [Value] = [Threshold] | count", DefaultMeasureMilliseconds))
            {
                b.Measure("For Count", Values.Length, () =>
                {
                    int count = 0;
                    for (int i = 0; i < Values.Length; ++i)
                    {
                        if (Values[i] == Thresholds[i]) count++;
                    }
                    return count;
                });

                b.Measure("XForm Count", Values.Length, () =>
                {
                    return (int)XFormTable.FromArrays(Values.Length)
                    .WithColumn("Value", Values)
                    .WithColumn("Threshold", Thresholds)
                    .Query("where [Value] = [Threshold]", Context)
                    .Count();
                });

                b.AssertResultsEqual();
            }
        }

        public void DoubleWhere()
        {
            using (Benchmarker b = new Benchmarker($"ushort[{Count:n0}] | where [Value] < 50 || [Value] > 950 | count", DefaultMeasureMilliseconds))
            {
                b.Measure("For Count", Values.Length, () =>
                {
                    int count = 0;
                    for (int i = 0; i < Values.Length; ++i)
                    {
                        if (Values[i] < 50 || Values[i] > 950) count++;
                    }
                    return count;
                });

                b.Measure("XForm Count", Values.Length, () =>
                {
                    return (int)XFormTable.FromArrays(Values.Length)
                    .WithColumn("Value", Values)
                    .Query("where [Value] < 50 || [Value] > 950", Context)
                    .Count();
                });

                b.AssertResultsEqual();
            }
        }

        public void ByteEqualsConstant()
        {
            byte[] bytes = new byte[1000 * 1000];
            Random r = new Random(8);
            r.NextBytes(bytes);

            using (Benchmarker b = new Benchmarker($"byte[{bytes.Length:n0}] | where [Value] < 16 | count", DefaultMeasureMilliseconds))
            {
                b.Measure("For Count", bytes.Length, () =>
                {
                    int count = 0;
                    for (int i = 0; i < bytes.Length; ++i)
                    {
                        if (bytes[i] < 16) count++;
                    }
                    return count;
                });

                b.Measure("XForm Count", bytes.Length, () =>
                {
                    return (int)XFormTable.FromArrays(bytes.Length)
                    .WithColumn("Value", bytes)
                    .Query("where [Value] < 16", Context)
                    .Count();
                });

                b.AssertResultsEqual();
            }
        }

        public void Join()
        {
            int joinFromLength = Math.Min(1000 * 1000, Values.Length);
            ushort[] joinTo = Enumerable.Range(10, 1000).Select((i) => (ushort)i).ToArray();

            using (Benchmarker b = new Benchmarker($"ushort[{joinFromLength:n0}] | join [Value] | count", DefaultMeasureMilliseconds))
            {
                b.Measure("XForm Join", joinFromLength, () =>
                {
                    IDataBatchEnumerator joinToSource = XFormTable.FromArrays(joinTo.Length).WithColumn("ID", joinTo);

                    IDataBatchEnumerator enumerator = XFormTable.FromArrays(joinFromLength).WithColumn("Value", Values);
                    enumerator = new Join(enumerator, "Value", joinToSource, "ID", "");
                    return (int)enumerator.Count();
                });
            }
        }

        public void Dictionary()
        {
            int count = 1000 * 1000;
            Dictionary<int, int> expected = new Dictionary<int, int>();
            Dictionary5<int, int> actual = new Dictionary5<int, int>(new EqualityComparerAdapter<int>(TypeProviderFactory.Get(typeof(int)).TryGetComparer()));
            Dictionary5O<int, int> actualO = new Dictionary5O<int, int>(new EqualityComparerAdapter<int>(TypeProviderFactory.Get(typeof(int)).TryGetComparer()));

            int[] values = new int[count];
            Random r = new Random(5);
            for (int i = 0; i < count; ++i)
            {
                values[i] = r.Next();
            }

            using (Benchmarker b = new Benchmarker($"Dictionary<int, int> [{count:n0}]", DefaultMeasureMilliseconds))
            {
                b.Measure("System.Collections.Generic.Dictionary", count, () =>
                {
                    for (int i = 0; i < count; ++i)
                    {
                        expected[values[i]] = i;
                    }

                    return expected.Count;
                });

                b.Measure("XForm.Dictionary5", count, () =>
                {
                    for (int i = 0; i < count; ++i)
                    {
                        actual.Add(values[i], i);
                    }

                    return actual.Count;
                });

                b.Measure("XForm.Dictionary5O", count, () =>
                {
                    for (int i = 0; i < count; ++i)
                    {
                        actualO.Add(values[i], i);
                    }

                    return actualO.Count;
                });

                b.AssertResultsEqual();
            }
        }

        public void Choose()
        {
            WorkflowContext context = new WorkflowContext();
            int[] rankPattern = new int[] { 2, 3, 1, 4, 6, 5, 7, 9, 8 };

            // Build three arrays
            int distinctCount = 100000;
            int countPerID = 9;
            int length = countPerID * distinctCount;
            int[] id = new int[length];
            int[] rank = new int[length];
            int[] value = new int[length];

            for (int i = 0; i < length; ++i)
            {
                id[i] = i / countPerID;                  // ID is the same for three rows at a time
                rank[i] = rankPattern[i % countPerID];   // Rank is [2, 3, 1] repeating (so the middle is the biggest)
                value[i] = i;                            // Value is the index of the real row
            }

            using (Benchmarker b = new Benchmarker($"Choose [{length:n0}]", 3 * DefaultMeasureMilliseconds))
            {
                b.Measure("Choose", length, () =>
                {
                    IDataBatchEnumerator actual = XFormTable.FromArrays(length)
                        .WithColumn("ID", id)
                        .WithColumn("Rank", rank)
                        .WithColumn("Value", value)
                        .Query("choose Max [Rank] [ID]", context);

                    return actual.Count();
                });
            }
        }

        public void TsvSplit()
        {
            Stream tsvStream = new MemoryStream();
            //Stream tsvStream = new FileStream("Sample.tsv", FileMode.Create);
            int rowCount = 1000 * 1000;
            WriteSampleTsv(tsvStream, 5, 1000 * 1000);

            byte[] content = new byte[64 * 1024];
            BitVector cells = new BitVector(content.Length);
            BitVector rows = new BitVector(content.Length);
            int[] rowEnds = new int[1024];

            byte[] allContent = new byte[tsvStream.Length];
            tsvStream.Seek(0, SeekOrigin.Begin);
            tsvStream.Read(allContent, 0, allContent.Length);
            BitVector allCells = new BitVector(allContent.Length);
            BitVector allRows = new BitVector(allContent.Length);

            using (Benchmarker b = new Benchmarker($"Tsv Parse [{rowCount:n0}] | count", DefaultMeasureMilliseconds))
            {
                b.Measure("Read only", (int)tsvStream.Length, () =>
                {
                    tsvStream.Seek(0, SeekOrigin.Begin);
                    while (true)
                    {
                        int lengthRead = tsvStream.Read(content, 0, content.Length);
                        if (lengthRead == 0) break;
                    }

                    return rowCount;
                });

                b.Measure("ReadLine | Split", (int)tsvStream.Length, () =>
                {
                    tsvStream.Seek(0, SeekOrigin.Begin);
                    int count = 0;
                    StreamReader reader = new StreamReader(tsvStream);
                    {
                        // Header row
                        reader.ReadLine();

                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            string[] cellSet = line.Split('\t');
                            count++;
                        }
                    }
                    return count;
                });

                b.Measure("Elfie TsvReader", (int)tsvStream.Length, () =>
                {
                    tsvStream.Seek(0, SeekOrigin.Begin);
                    int count = 0;
                    ITabularReader reader = TabularFactory.BuildReader(tsvStream, "Unused.tsv");
                    {
                        while (reader.NextRow()) count++;
                    }
                    return count;
                });


                Func<byte[], int, int, ulong[], ulong[], int> splitTsvN = NativeAccelerator.GetMethod<Func<byte[], int, int, ulong[], ulong[], int>>("XForm.Native.String8N", "SplitTsv");
                b.Measure("XForm Native Split", (int)tsvStream.Length, () =>
                {
                    tsvStream.Seek(0, SeekOrigin.Begin);

                    int count = -1;
                    while (true)
                    {
                        int lengthRead = tsvStream.Read(content, 0, content.Length);
                        if (lengthRead == 0) break;
                        if (lengthRead < content.Length) Array.Clear(content, lengthRead, content.Length - lengthRead);

                        int lineCount = splitTsvN(content, 0, lengthRead, cells.Array, rows.Array);
                        count += lineCount;

                        int fromRow = 0;
                        int countCopy = cells.Page(rowEnds, ref fromRow);
                    }

                    return count;
                });

                b.MeasureParallel("XForm Native Split Parallel", (int)tsvStream.Length, (index, length) =>
                {
                    return splitTsvN(allContent, index, length, allCells.Array, allRows.Array) - 1;
                });
            }
        }

        private static void WriteSampleTsv(Stream stream, int seed, int rowCount)
        {
            DateTime start = new DateTime(2018, 01, 01, 0, 0, 0, DateTimeKind.Utc);

            Random r = new Random(seed);
            ITabularWriter writer = TabularFactory.BuildWriter(stream, "Unused.tsv");
            {
                writer.SetColumns(new string[] { "Zip", "LastScan", "IsArchived" });
                for (int i = 0; i < rowCount; ++i)
                {
                    writer.Write(r.Next(10000, 99999));
                    writer.Write(start.AddDays(-180.0 * r.NextDouble()));
                    writer.Write(r.Next(100) < 50);
                    writer.NextRow();
                }
            }
        }
    }
}

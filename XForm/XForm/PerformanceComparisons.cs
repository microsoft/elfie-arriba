// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using System.IO;
using System.Linq;

using XForm.Data;
using XForm.Extensions;
using XForm.Test;

namespace XForm
{
    public class PerformanceComparisons
    {
        private int _rowCount;
        private ushort[] _values;
        private ushort[] _thresholds;
        private WorkflowContext _context;

        public PerformanceComparisons()
        {
            _rowCount = 10 * 1000 * 1000;
            _values = new ushort[_rowCount];
            _thresholds = new ushort[_rowCount];

            Random r = new Random();
            for (int i = 0; i < _values.Length; ++i)
            {
                _values[i] = (ushort)r.Next(1000);
                _thresholds[i] = 50;
            }

            _context = new WorkflowContext();
        }

        public void Run()
        {
            //NativeAccelerator.Enable();
            //DoubleWhere();
            //WhereIntUnderConstant();
            //WhereIntEqualsInt();
            TsvSplit();
        }

        public void WhereIntUnderConstant()
        {
            using (Benchmarker b = new Benchmarker($"int[{_rowCount:n0}] | where [Value] < 50 | count", 3000))
            {
                b.Measure("For Count", _values.Length, () =>
                {
                    int count = 0;
                    for (int i = 0; i < _values.Length; ++i)
                    {
                        if (_values[i] < 50) count++;
                    }
                    return count;
                });

                b.Measure("Linq Count", _values.Length, () =>
                {
                    return _values.Where((i) => i < 50).Count();
                });

                b.Measure("XForm Count", _values.Length, () =>
                {
                    return (int)XFormTable.FromArrays(_values.Length)
                    .WithColumn("Value", _values)
                    .Query("where [Value] < 50", _context)
                    .Count();
                });

                b.AssertResultsEqual();
            }
        }

        public void WhereIntEqualsInt()
        {
            using (Benchmarker b = new Benchmarker($"int[{_rowCount:n0}] | where [Value] = [Threshold] | count"))
            {
                b.Measure("For Count", _values.Length, () =>
                {
                    int count = 0;
                    for (int i = 0; i < _values.Length; ++i)
                    {
                        if (_values[i] == _thresholds[i]) count++;
                    }
                    return count;
                });

                b.Measure("XForm Count", _values.Length, () =>
                {
                    return (int)XFormTable.FromArrays(_values.Length)
                    .WithColumn("Value", _values)
                    .WithColumn("Threshold", _thresholds)
                    .Query("where [Value] = [Threshold]", _context)
                    .Count();
                });

                b.AssertResultsEqual();
            }
        }

        public void DoubleWhere()
        {
            using (Benchmarker b = new Benchmarker($"int[{_rowCount:n0}] | where [Value] < 50 | where [Value] = 25 | count", 3000))
            {
                b.Measure("For Count", _values.Length, () =>
                {
                    int count = 0;
                    for (int i = 0; i < _values.Length; ++i)
                    {
                        if (_values[i] < 50)
                        {
                            if (_values[i] == 25) count++;
                        }
                    }
                    return count;
                });

                b.Measure("XForm Count", _values.Length, () =>
                {
                    return (int)XFormTable.FromArrays(_values.Length)
                    .WithColumn("Value", _values)
                    .Query("where [Value] < 50", _context)
                    .Query("where [Value] = 25", _context)
                    .Count();
                });

                b.AssertResultsEqual();
            }
        }

        public void TsvSplit()
        {
            Stream tsvStream = new MemoryStream();
            int rowCount = 1000 * 1000;
            WriteSampleTsv(tsvStream, 5, 1000 * 1000);

            // Read once first (try to get cached)

            using (Benchmarker b = new Benchmarker($"Tsv Parse [{rowCount:n0}] | sum(Zip)", 3000))
            {
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
                            string[] cells = line.Split('\t');
                            count++;
                        }
                    }
                    return count;
                });

                b.Measure("Elfie TsvReader ", (int)tsvStream.Length, () =>
                {
                    tsvStream.Seek(0, SeekOrigin.Begin);
                    int count = 0;
                    ITabularReader reader = TabularFactory.BuildReader(tsvStream, "Unused.tsv");
                    {
                        while (reader.NextRow()) count++;
                    }
                    return count;
                });

                b.AssertResultsEqual();
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

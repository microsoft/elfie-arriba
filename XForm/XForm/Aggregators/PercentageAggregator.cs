// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;

namespace XForm.Aggregators
{
    internal class PercentageBuilder : IAggregatorBuilder
    {
        public string Name => "Percentage";
        public string Usage => "Percentage()";

        public IAggregator Build(IXTable source, XDatabaseContext context)
        {
            return new PercentageAggregator();
        }
    }

    public class PercentageAggregator : IAggregator, IFoundIndicesTracker
    {
        private CountAggregator _counter;
        private int _totalRows;

        public PercentageAggregator()
        {
            ColumnDetails = new ColumnDetails("Percentage", typeof(String8));
            _counter = new CountAggregator();
        }

        public ColumnDetails ColumnDetails { get; private set; }
        public ArraySelector FoundIndices => _counter.FoundIndices;

        public XArray Values => ToPercentageStrings(_counter.Values, _totalRows, ThreeSigFigs);

        public static XArray ToPercentageStrings(XArray counts, int total, Func<int, int, string> formatter)
        {
            int[] countArray = (int[])counts.Array;

            String8Block block = new String8Block();
            String8[] percentages = new String8[counts.Count];
            for (int i = 0; i < counts.Count; ++i)
            {
                // Convert to a percentage, string, and then String8
                percentages[i] = block.GetCopy(formatter(countArray[counts.Index(i)], total));
            }

            return XArray.All(percentages, counts.Count);
        }

        public static string ThreeSigFigs(int count, int total)
        {
            if (total == 0) return "-";
            if (count == total) return "100%";

            float percentage = (float)count / (float)total;

            if (percentage < 0.01)
            {
                return percentage.ToString("p2");  // 0.12%
            }
            else if (percentage < 0.10)
            {
                return percentage.ToString("p2");  // 1.23%
            }
            else
            {
                return percentage.ToString("p1");  // 12.3%
            }
        }

        public static string TwoSigFigs(int count, int total)
        {
            if (total == 0) return "-";
            if (count == total) return "100%";

            float percentage = (float)count / (float)total;

            if (percentage < 0.01)
            {
                return percentage.ToString("p1");  // 0.1%
            }
            else if (percentage < 0.10)
            {
                return percentage.ToString("p1");  // 1.2%
            }
            else
            {
                return percentage.ToString("p0");  // 12%
            }
        }

        public static string WholePercentage(int count, int total)
        {
            if (total == 0) return "-";
            if (count == total) return "100%";

            float percentage = (float)count / (float)total;
            return percentage.ToString("p0");
        }

        public void Add(XArray rowIndices, int newDistinctCount)
        {
            _counter.Add(rowIndices, newDistinctCount);
            _totalRows += rowIndices.Count;
        }
    }
}

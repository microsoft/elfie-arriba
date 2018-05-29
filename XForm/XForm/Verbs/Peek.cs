// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Aggregators;
using XForm.Columns;
using XForm.Core;
using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Verbs
{
    internal class PeekBuilder : IVerbBuilder
    {
        public string Verb => "peek";
        public string Usage => "peek {Column}";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            return new Peek(source, context.Parser.NextColumn(source, context));
        }
    }

    /// <summary>
    ///  Peek returns the top 20 distinct values in the column and the approximate
    ///  percentage of rows for each value. It returns only values in more than 0.5%
    ///  of rows, only the top 20, and only accurate to +/- 1% with 95% confidence.
    ///  
    ///  9,604 samples required to see a 50% value within +/- 1% with 95% confidence.
    /// </summary>
    public class Peek : IXTable
    {
        private const int MaximumCountToReturn = 20;                    // Only return the top this many groups
        private const float MinimumPercentageToReport = 0.005f;         // Only return groups with at least this percentage of rows
        private const int RequiredSampleSize = 9604;                   // Use sampled rows when the same has at least this many samples
        private const int SampleCount = 5;                              // Build this many scaled samples

        private IXTable _source;
        private IXColumn _column;
        private DeferredArrayColumn[] _columns;

        private bool _isDictionaryBuilt;
        private int _distinctCount;

        private ArraySelector _currentEnumerateSelector;

        public Peek(IXTable source, IXColumn column)
        {
            if (source == null) throw new ArgumentNullException("source");
            _source = source;
            _column = column;

            // Build a DeferredArrayColumn for each key and for the aggregator
            _columns = new DeferredArrayColumn[]
            {
                new DeferredArrayColumn(_column.ColumnDetails),
                new DeferredArrayColumn(new ColumnDetails("Count", typeof(int))),
                new DeferredArrayColumn(new ColumnDetails("Percentage", typeof(String8)))
            };
        }

        public IReadOnlyList<IXColumn> Columns => _columns;
        public int CurrentRowCount { get; private set; }

        public int Next(int desiredCount, CancellationToken cancellationToken)
        {
            // If this is the first call, walk all rows once to group them
            if (!_isDictionaryBuilt)
            {
                _isDictionaryBuilt = true;
                BuildDictionary(cancellationToken);
                Reset();
            }

            // Once done, page through the distinct group by values found
            _currentEnumerateSelector = _currentEnumerateSelector.NextPage(_distinctCount, desiredCount);
            for (int i = 0; i < _columns.Length; ++i)
            {
                _columns[i].SetSelector(_currentEnumerateSelector);
            }

            CurrentRowCount = _currentEnumerateSelector.Count;
            return CurrentRowCount;
        }

        private void PostSortAndFilter(XArray groups, XArray counts, int totalRowCount, bool wasAllRows)
        {
            int[] finalIndices = new int[groups.Count];
            int[] finalCounts = new int[groups.Count];

            int groupCount = 0;

            // Filter to counts over the minimum percentage threshold
            int[] countsArray = (int[])counts.Array;
            if (countsArray != null)
            {
                int threshold = (int)(totalRowCount * MinimumPercentageToReport);
                for (int i = 0; i < groups.Count; ++i)
                {
                    int count = countsArray[counts.Index(i)];
                    if (count >= threshold)
                    {
                        finalIndices[groupCount] = i;
                        finalCounts[groupCount] = count;
                        groupCount++;
                    }
                }
            }

            // Sort the values by count descending
            Array.Sort<int, int>(finalCounts, finalIndices, 0, groupCount, new ReverseComparer());

            // Limit to the top N if needed
            if (groupCount > MaximumCountToReturn) groupCount = MaximumCountToReturn;

            // Set the distinct count (now that it's known)
            _distinctCount = groupCount;

            // Set the output values
            int[] groupsRemap = null;
            XArray finalCountsX = XArray.All(finalCounts, groupCount);

            _columns[0].SetValues(groups.Select(ArraySelector.Map(finalIndices, groupCount), ref groupsRemap));
            _columns[1].SetValues(finalCountsX);

            if (wasAllRows)
            {
                _columns[2].SetValues(PercentageAggregator.ToPercentageStrings(finalCountsX, totalRowCount, PercentageAggregator.TwoSigFigs));
            }
            else
            {
                _columns[2].SetValues(PercentageAggregator.ToPercentageStrings(finalCountsX, totalRowCount, PercentageAggregator.WholePercentage));
            }
        }

        private class ReverseComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return y - x;
            }
        }

        /// <summary>
        ///  Build a GroupBy Dictionary for Peek.
        /// </summary>
        /// <remarks>
        ///  Peek identifies each distinct common value and the approximate percentage of rows with it.
        ///  If we have many matching rows, we can sample - the sample will have any common values in it.
        ///  However, we don't know how many matches we have in advance.
        ///  Therefore, we build a Dictionary of all rows, 1/8 of rows, 1/64 of rows, and 1/512 of rows.
        ///  As soon as a given sample has enough samples to be statistically valid, we stop collecting the larger subsets.
        ///  This strategy allows us to run the overall query only once, end up with a large enough sample, and avoid building giant Dictionaries.
        /// </remarks>
        /// <param name="cancellationToken">CancellationToken to request early stop</param>
        private void BuildDictionary(CancellationToken cancellationToken)
        {
            // Short-circuit path if there's one key column and it's an EnumColumn
            if (_column.IsEnumColumn())
            {
                BuildSingleEnumColumnDictionary(cancellationToken);
                return;
            }

            // Build a Random instance to sample rows
            Random r = new Random();

            // Build a Dictionary and CountAggregator for each sample
            GroupByDictionary[] dictionaries = new GroupByDictionary[SampleCount];
            CountAggregator[] counts = new CountAggregator[SampleCount];
            int[][] remapArrays = new int[SampleCount][];
            for (int i = 0; i < SampleCount; ++i)
            {
                dictionaries[i] = new GroupByDictionary(new ColumnDetails[] { _column.ColumnDetails });
                counts[i] = new CountAggregator();
            }

            // Retrieve the column getter
            Func<XArray> columnGetter = _column.CurrentGetter();

            // Track which sample we'll currently report
            int currentSample = 0;

            XArray[] arrays = new XArray[1];
            int count;
            while ((count = _source.Next(XTableExtensions.DefaultBatchSize, cancellationToken)) != 0)
            {
                // Get the column values
                arrays[0] = columnGetter();

                // Build the GroupBy count for all rows and successive 1/8 samples
                for (int i = 0; i < SampleCount; ++i)
                {
                    // Add these to the Join Dictionary
                    if (i >= currentSample)
                    {
                        // Choose buckets for each row
                        XArray indicesForRows = dictionaries[i].FindOrAdd(arrays);

                        // Identify the bucket for each row and aggregate them
                        counts[i].Add(indicesForRows, dictionaries[i].Count);

                        // If this sample now has enough values, stop collecting bigger row sets
                        if (currentSample == i - 1 && counts[i].TotalRowCount > RequiredSampleSize)
                        {
                            // If every row was unique, stop early and don't set outputs (zero rows)
                            if (ShouldStopEarly(dictionaries[currentSample], counts[currentSample])) return;

                            dictionaries[currentSample] = null;
                            counts[currentSample] = null;
                            currentSample++;
                        }
                    }

                    // Each successive dictionary has ~1/8 of the rows of the previous one
                    if (i < SampleCount - 1)
                    {
                        ArraySelector sample = Sampler.Eighth(arrays[0].Selector, r, ref remapArrays[i]);
                        arrays[0] = arrays[0].Reselect(sample);
                    }
                }
            }

            // Once the loop is done, get the distinct values and aggregation results
            PostSortAndFilter(dictionaries[currentSample].DistinctKeys()[0], counts[currentSample].Values, counts[currentSample].TotalRowCount, currentSample == 0);
        }

        private bool ShouldStopEarly(GroupByDictionary dictionary, CountAggregator counter)
        {
            // If every value was unique so far, stop
            if (dictionary.Count == counter.TotalRowCount) return true;

            // If any value had enough rows to report, keep going
            XArray counts = counter.Values;
            int[] countsArray = (int[])counts.Array;
            int threshold = (int)(counter.TotalRowCount * MinimumPercentageToReport);
            for (int i = 0; i < counts.Count; ++i)
            {
                int count = countsArray[counts.Index(i)];
                if (count >= threshold) return false;
            }

            // Otherwise, stop (not all unique, but no values in > 0.5% of rows)
            return true;
        }

        private void BuildSingleEnumColumnDictionary(CancellationToken cancellationToken)
        {
            // Build a CountAggregator for the enum GroupBy
            CountAggregator counts = new CountAggregator();

            XArray values = _column.ValuesGetter()();
            Func<XArray> indicesGetter = _column.IndicesCurrentGetter();

            int count;
            while ((count = _source.Next(XTableExtensions.DefaultBatchSize, cancellationToken)) != 0)
            {
                // Aggregate each row directly on the row index (already a small zero-based value)
                XArray indices = indicesGetter();
                counts.Add(indices, values.Count);
            }

            // Once the loop is done, get the distinct values and aggregation results
            PostSortAndFilter(values, counts.Values, counts.TotalRowCount, true);
        }

        public void Reset()
        {
            _currentEnumerateSelector = ArraySelector.All(_distinctCount).Slice(0, 0);

            for (int i = 0; i < _columns.Length; ++i)
            {
                _columns[i].SetSelector(_currentEnumerateSelector);
            }
        }

        public void Dispose()
        {
            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using XForm.Aggregators;
using XForm.Columns;
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

    public class Peek : IXTable
    {
        private const int MaximumCountToReturn = 20;
        private const float MinimumPercentageToReport = 0.005f;

        private IXTable _source;
        private IXColumn _column;
        private CountAggregator _counts;

        private DeferredArrayColumn[] _columns;

        private bool _isDictionaryBuilt;
        private GroupByDictionary _dictionary;
        private int _distinctCount;

        private ArraySelector _currentEnumerateSelector;

        public Peek(IXTable source, IXColumn column)
        {
            if (source == null) throw new ArgumentNullException("source");
            _source = source;
            _column = column;
            _counts = new CountAggregator();

            // Build a typed dictionary to handle the column values
            _dictionary = new GroupByDictionary(new ColumnDetails[] { _column.ColumnDetails });

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

        private void PostSortAndFilter(XArray groups, XArray counts, int totalRowCount)
        {
            int[] finalIndices = new int[groups.Count];
            int[] finalCounts = new int[groups.Count];

            int groupCount = 0;

            // Filter to counts over the minimum percentage threshold
            int[] countsArray = (int[])counts.Array;
            int threshold = (int)(totalRowCount * MinimumPercentageToReport);
            for (int i = 0; i < groups.Count; ++i)
            {
                int count = countsArray[counts.Index(i)];
                if(count >= threshold)
                {
                    finalIndices[groupCount] = i;
                    finalCounts[groupCount] = count;
                    groupCount++;
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
            _columns[2].SetValues(PercentageAggregator.ToPercentageStrings(finalCountsX, totalRowCount));
        }

        private class ReverseComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return y - x;
            }
        }

        private void BuildDictionary(CancellationToken cancellationToken)
        {
            // Short-circuit path if there's one key column and it's an EnumColumn
            if (_column.IsEnumColumn())
            {
                BuildSingleEnumColumnDictionary(cancellationToken);
                return;
            }

            // Retrieve the getters for all columns
            Func<XArray> columnGetter = _column.CurrentGetter();

            XArray[] arrays = new XArray[1];
            int count;
            while ((count = _source.Next(XTableExtensions.DefaultBatchSize, cancellationToken)) != 0)
            {
                // Get the key column arrays
                arrays[0] = columnGetter();

                // Add these to the Join Dictionary
                XArray indicesForRows = _dictionary.FindOrAdd(arrays);

                // Identify the bucket for each row and aggregate them
                _counts.Add(indicesForRows, _dictionary.Count);
            }

            // Once the loop is done, get the distinct values and aggregation results
            PostSortAndFilter(_dictionary.DistinctKeys()[0], _counts.Values, _counts.TotalRowCount);
        }

        private void BuildSingleEnumColumnDictionary(CancellationToken cancellationToken)
        {
            XArray values = _column.ValuesGetter()();
            Func<XArray> indicesGetter = _column.IndicesCurrentGetter();

            int count;
            while ((count = _source.Next(XTableExtensions.DefaultBatchSize, cancellationToken)) != 0)
            {
                // Aggregate each row directly on the row index (already a small zero-based value)
                XArray indices = indicesGetter();
                _counts.Add(indices, values.Count);
            }

            // Once the loop is done, get the distinct values and aggregation results
            PostSortAndFilter(values, _counts.Values, _counts.TotalRowCount);
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

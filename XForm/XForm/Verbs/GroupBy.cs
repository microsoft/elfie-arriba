// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    internal class GroupByBuilder : IVerbBuilder
    {
        public string Verb => "groupBy";
        public string Usage => "groupBy {Column}, ... with {Aggregator}, ...";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            List<IXColumn> groupByColumns = new List<IXColumn>();
            List<IAggregator> aggregators = new List<IAggregator>();

            // Parse GroupBy columns
            do
            {
                IXColumn column = context.Parser.NextColumn(source, context);
                if (column.IsConstantColumn()) throw new ArgumentException("GroupBy can't aggregate across a constant.");

                groupByColumns.Add(column);
            } while (context.Parser.HasAnotherPart && !context.Parser.NextTokenText.Equals("with", StringComparison.OrdinalIgnoreCase));


            // If 'GET', parse Aggregators
            if (context.Parser.HasAnotherPart)
            {
                context.Parser.NextSingleKeyword("with");

                do
                {
                    aggregators.Add(context.Parser.NextAggregator(source, context));
                } while (context.Parser.HasAnotherPart);
            }

            return new GroupBy(source, groupByColumns, aggregators);
        }
    }

    public class GroupBy : IXTable
    {
        private IXTable _source;
        private IXColumn[] _keyColumns;
        private IAggregator[] _aggregators;

        private DeferredArrayColumn[] _columns;

        private bool _isDictionaryBuilt;
        private GroupByDictionary _dictionary;
        private int _distinctCount;

        private ArraySelector _currentEnumerateSelector;

        public GroupBy(IXTable source, IList<IXColumn> keyColumns, IList<IAggregator> aggregators)
        {
            if (source == null) throw new ArgumentNullException("source");
            _source = source;
            _keyColumns = keyColumns.ToArray();
            _aggregators = aggregators.ToArray();

            // Build a typed dictionary to handle the rank and key column types
            _dictionary = new GroupByDictionary(keyColumns.Select((col) => col.ColumnDetails).ToArray());

            // Build a DeferredArrayColumn for each key and for the aggregator
            _columns = new DeferredArrayColumn[keyColumns.Count + aggregators.Count];
            for (int i = 0; i < keyColumns.Count; ++i)
            {
                _columns[i] = new DeferredArrayColumn(keyColumns[i].ColumnDetails);
            }

            for (int i = 0; i < aggregators.Count; ++i)
            {
                _columns[keyColumns.Count + i] = new DeferredArrayColumn(_aggregators[i].ColumnDetails);
            }
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

        private void BuildDictionary(CancellationToken cancellationToken)
        {
            // Short-circuit path if there's one key column and it's an EnumColumn
            if (_keyColumns.Length == 1 && _keyColumns[0].IsEnumColumn())
            {
                BuildSingleEnumColumnDictionary(cancellationToken);
                return;
            }

            // Retrieve the getters for all columns
            Func<XArray>[] keyColumnGetters = _keyColumns.Select((col) => col.CurrentGetter()).ToArray();

            XArray[] keyArrays = new XArray[keyColumnGetters.Length];
            int count;
            while ((count = _source.Next(XTableExtensions.DefaultBatchSize, cancellationToken)) != 0)
            {
                // Get the key column arrays
                for (int i = 0; i < keyArrays.Length; ++i)
                {
                    keyArrays[i] = keyColumnGetters[i]();
                }

                // Add these to the Join Dictionary
                XArray indicesForRows = _dictionary.FindOrAdd(keyArrays);

                // Identify the bucket for each row and aggregate them
                for (int i = 0; i < _aggregators.Length; ++i)
                {
                    _aggregators[i].Add(indicesForRows, _dictionary.Count);
                }
            }

            // Store the distinct count now that we know it
            _distinctCount = _dictionary.Count;

            // Once the loop is done, get the distinct values and aggregation results
            XArray[] keys = _dictionary.DistinctKeys();
            for (int i = 0; i < _keyColumns.Length; ++i)
            {
                _columns[i].SetValues(keys[i]);
            }

            for (int i = 0; i < _aggregators.Length; ++i)
            {
                _columns[_keyColumns.Length + i].SetValues(_aggregators[i].Values);
            }
        }

        private void BuildSingleEnumColumnDictionary(CancellationToken cancellationToken)
        {
            XArray values = _keyColumns[0].ValuesGetter()();
            Func<XArray> indicesGetter = _keyColumns[0].IndicesCurrentGetter();

            // Find or construct an aggregator which can track which enum values ended up with any rows in the result
            IFoundIndicesTracker tracker = (IFoundIndicesTracker)_aggregators.FirstOrDefault((agg) => agg is IFoundIndicesTracker);
            bool trackerFound = (tracker != null);
            if (!trackerFound) tracker = new CountAggregator();

            int count;
            while ((count = _source.Next(XTableExtensions.DefaultBatchSize, cancellationToken)) != 0)
            {
                // Aggregate each row directly on the row index (already a small zero-based value)
                XArray indices = indicesGetter();

                for (int i = 0; i < _aggregators.Length; ++i)
                {
                    _aggregators[i].Add(indices, values.Count);
                }

                if (!trackerFound) tracker.Add(indices, values.Count);
            }

            // Figure out which rows had matches
            ArraySelector foundValuesSelector = tracker.FoundIndices;

            // Store the distinct count now that we know it
            _distinctCount = foundValuesSelector.Count;

            // Once the loop is done, get the distinct values and aggregation results
            _columns[0].SetValues(values.Reselect(foundValuesSelector));
            for (int i = 0; i < _aggregators.Length; ++i)
            {
                _columns[i + 1].SetValues(_aggregators[i].Values.Reselect(foundValuesSelector));
            }
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

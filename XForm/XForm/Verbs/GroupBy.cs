using System;
using System.Collections.Generic;
using System.Linq;
using XForm.Columns;
using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Verbs
{
    internal class GroupByBuilder : IVerbBuilder
    {
        public string Verb => "groupBy";
        public string Usage => "groupBy {Column}, ... GET {Aggregator}, ...";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            List<IXColumn> groupByColumns = new List<IXColumn>();
            do
            {
                groupByColumns.Add(context.Parser.NextColumn(source, context));
            } while (context.Parser.HasAnotherPart);

            return new GroupBy(source, groupByColumns, new CountAggregator());
        }
    }

    public interface IAggregator
    {
        ColumnDetails ColumnDetails { get; }
        XArray Values { get; }

        void Add(XArray rowIndices, int newDistinctCount);
    }

    public class CountAggregator : IAggregator
    {
        private int[] _countPerBucket;
        private int _distinctCount;

        public ColumnDetails ColumnDetails { get; private set; }
        public XArray Values => XArray.All(_countPerBucket, _distinctCount);

        public CountAggregator()
        {
            ColumnDetails = new ColumnDetails("Count", typeof(int));
        }

        public void Add(XArray rowIndices, int newDistinctCount)
        {
            _distinctCount = newDistinctCount;
            Allocator.ExpandToSize(ref _countPerBucket, newDistinctCount);

            if (rowIndices.Array is int[])
            {
                int[] array = (int[])rowIndices.Array;
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _countPerBucket[array[i]]++;
                }
            }
            else if (rowIndices.Array is byte[])
            {
                byte[] array = (byte[])rowIndices.Array;
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _countPerBucket[array[i]]++;
                }
            }
            else
            {
                ushort[] array = (ushort[])rowIndices.Array;
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _countPerBucket[array[i]]++;
                }
            }
        }
    }

    public class GroupBy : IXTable
    {
        private IXTable _source;
        private IXColumn[] _keyColumns;
        private IAggregator _aggregator;

        private DeferredArrayColumn[] _columns;

        private bool _isDictionaryBuilt;
        private GroupByDictionary _dictionary;
        private int _distinctCount;

        private ArraySelector _currentEnumerateSelector;

        public GroupBy(IXTable source, IList<IXColumn> keyColumns, IAggregator aggregator)
        {
            if (source == null) throw new ArgumentNullException("source");
            _source = source;
            _keyColumns = keyColumns.ToArray();
            _aggregator = aggregator;

            // Build a typed dictionary to handle the rank and key column types
            _dictionary = new GroupByDictionary(keyColumns.Select((col) => col.ColumnDetails).ToArray());

            // Build a DeferredArrayColumn for each key and for the aggregator
            _columns = new DeferredArrayColumn[keyColumns.Count + 1];
            for(int i = 0; i < keyColumns.Count; ++i)
            {
                _columns[i] = new DeferredArrayColumn(keyColumns[i].ColumnDetails);
            }

            _columns[_columns.Length - 1] = new DeferredArrayColumn(_aggregator.ColumnDetails);
        }

        public IReadOnlyList<IXColumn> Columns => _columns;
        public int CurrentRowCount { get; private set; }

        public int Next(int desiredCount)
        {
            // If this is the first call, walk all rows once to find best rows
            if (!_isDictionaryBuilt)
            {
                _isDictionaryBuilt = true;
                BuildDictionary();
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

        private void BuildDictionary()
        {
            // Short-circuit path if there's one key column and it's an EnumColumn
            if(_keyColumns.Length == 1 && _keyColumns[0].IsEnumColumn())
            {
                BuildSingleEnumColumnDictionary();
                return;
            }

            // Retrieve the getters for all columns
            Func<XArray>[] keyColumnGetters = _keyColumns.Select((col) => col.CurrentGetter()).ToArray();

            XArray[] keyArrays = new XArray[keyColumnGetters.Length];
            int count;
            while ((count = _source.Next(XTableExtensions.DefaultBatchSize)) != 0)
            {
                // Get the key column arrays
                for (int i = 0; i < keyArrays.Length; ++i)
                {
                    keyArrays[i] = keyColumnGetters[i]();
                }

                // Add these to the Join Dictionary
                XArray indicesForRows = _dictionary.FindOrAdd(keyArrays);

                // Identify the bucket for each row and aggregate them
                _aggregator.Add(indicesForRows, _dictionary.Count);
            }

            // Store the distinct count now that we know it
            _distinctCount = _dictionary.Count;

            // Once the loop is done, get the distinct values and aggregation result
            XArray[] keys = _dictionary.DistinctKeys();
            if (keys.Length != _columns.Length - 1) throw new InvalidOperationException("GroupByDictionary didn't have the right number of key columns.");
            for (int i = 0; i < _columns.Length - 1; ++i)
            {
                _columns[i].SetValues(keys[i]);
            }

            _columns[_columns.Length - 1].SetValues(_aggregator.Values);
        }

        private void BuildSingleEnumColumnDictionary()
        {
            XArray values = _keyColumns[0].ValuesGetter()();
            Func<XArray> indicesGetter = _keyColumns[0].IndicesCurrentGetter();

            int count;
            while ((count = _source.Next(XTableExtensions.DefaultBatchSize)) != 0)
            {
                // Identify the bucket for each row and aggregate them
                XArray indices = indicesGetter();
                _aggregator.Add(indices, values.Count);
            }

            // Store the distinct count now that we know it
            _distinctCount = values.Count;

            // Once the loop is done, get the distinct values and aggregation result
            _columns[0].SetValues(values);
            _columns[1].SetValues(_aggregator.Values);
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

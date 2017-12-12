// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;
using XForm.Transforms;

namespace XForm.Commands
{
    internal class JoinBuilder : IPipelineStageBuilder
    {
        public string Verb => "join";
        public string Usage => "'join' [FromColumnName] [ToBinarySource] [ToColumn] [JoinedInColumnPrefix]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            string sourceColumnName = context.Parser.NextColumnName(source);
            IDataBatchEnumerator joinToSource = context.Parser.NextTableSource();
            string joinToColumn = context.Parser.NextColumnName(joinToSource);

            return new Join(
                source,
                sourceColumnName,
                joinToSource,
                joinToColumn,
                (string)context.Parser.NextLiteralValue());
        }
    }

    public class Join : IDataBatchEnumerator
    {
        private IDataBatchEnumerator _source;
        private MemoryCacher _cachedJoinSource;

        private int _joinFromColumnIndex;
        private Func<DataBatch> _joinFromColumnGetter;
        private Func<DataBatch> _joinToColumnGetter;
        private Dictionary<String8, int> _joinDictionary;

        private List<ColumnDetails> _columns;
        private List<int> _mappedColumnIndices;

        private RowRemapper _sourceJoinedRowsFilter;
        private int[] _currentJoinRowIndices;
        private int _currentJoinCount;

        public Join(IDataBatchEnumerator source, string joinFromColumn, IDataBatchEnumerator joinToSource, string joinToColumn, string joinSidePrefix)
        {
            _source = source;

            IDataBatchList joinSourceList = joinToSource as IDataBatchList;
            _cachedJoinSource = new MemoryCacher(joinSourceList, CacheLevel.Used);

            // Request the JoinFromColumn Getter
            _joinFromColumnIndex = source.Columns.IndexOfColumn(joinFromColumn);
            _joinFromColumnGetter = source.ColumnGetter(_joinFromColumnIndex);

            // Request the JoinToColumn Getter
            _joinToColumnGetter = _cachedJoinSource.ColumnGetter(_cachedJoinSource.Columns.IndexOfColumn(joinToColumn));

            // All of the main source columns are passed through
            _columns = new List<ColumnDetails>(source.Columns);

            // Find and map the columns coming from the join
            _mappedColumnIndices = new List<int>();
            for (int i = 0; i < _cachedJoinSource.Columns.Count; ++i)
            {
                ColumnDetails column = _cachedJoinSource.Columns[i];
                _columns.Add(column.Rename(joinSidePrefix + column.Name));
                _mappedColumnIndices.Add(i);
            }

            _sourceJoinedRowsFilter = new RowRemapper();
        }

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // If this is one of the joined in columns, return the rows which matched from the join
            if (columnIndex >= _source.Columns.Count)
            {
                // Otherwise, return the join column (we'll seek to the matching rows on each Next)
                int joinColumnIndex = _mappedColumnIndices[columnIndex - _source.Columns.Count];
                return _cachedJoinSource.ColumnGetter(joinColumnIndex);
            }

            // Otherwise, get the source getter
            Func<DataBatch> sourceGetter = (columnIndex == _joinFromColumnIndex ? _joinFromColumnGetter : _source.ColumnGetter(columnIndex));

            // Cache an array to remap rows which joined
            int[] remapArray = null;

            return () =>
            {
                // Get the source values for this batch
                DataBatch batch = sourceGetter();

                // Remap to just the rows which joined
                return _sourceJoinedRowsFilter.Remap(batch, ref remapArray);
            };
        }

        public int Next(int desiredCount)
        {
            // If this is the first call, fully cache the JoinToSource and build a lookup Dictionary
            if (_joinDictionary == null) BuildJoinDictionary();

            while (true)
            {
                // Get the next rows from the source
                int count = _source.Next(desiredCount);
                if (count == 0) return 0;

                DataBatch joinFromValues = _joinFromColumnGetter();
                String8[] array = (String8[])joinFromValues.Array;

                // Find the matching row index for each value
                Allocator.AllocateToSize(ref _currentJoinRowIndices, count);
                _sourceJoinedRowsFilter.ClearAndSize(count);

                int joinedCount = 0;
                for (int i = 0; i < count; ++i)
                {
                    String8 joinFromValue = array[joinFromValues.Index(i)];

                    int matchIndex;
                    if (_joinDictionary.TryGetValue(joinFromValue, out matchIndex))
                    {
                        _currentJoinRowIndices[joinedCount] = matchIndex;
                        joinedCount++;
                        _sourceJoinedRowsFilter.Add(i);
                    }
                }

                _currentJoinCount = joinedCount;
                if (_currentJoinCount > 0) break;
            }

            // 'Seek' those particular rows in the JoinToSource
            _cachedJoinSource.Get(ArraySelector.Map(_currentJoinRowIndices, _currentJoinCount));

            return _currentJoinCount;
        }

        private void BuildJoinDictionary()
        {
            _joinDictionary = new Dictionary<String8, int>();

            int joinToTotalCount = _cachedJoinSource.Next(int.MaxValue);
            DataBatch allJoinToValues = _joinToColumnGetter();
            String8[] array = (String8[])allJoinToValues.Array;

            for (int i = 0; i < joinToTotalCount; ++i)
            {
                String8 value = array[allJoinToValues.Index(i)];
                _joinDictionary[value] = i;
            }
        }

        public void Reset()
        {
            _source.Reset();
        }

        public void Dispose()
        {
            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }

            if (_cachedJoinSource != null)
            {
                _cachedJoinSource.Dispose();
                _cachedJoinSource = null;
            }
        }
    }
}

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

namespace XForm.Verbs
{
    internal class JoinBuilder : IVerbBuilder
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

        private IJoinDictionary _joinDictionary;

        private List<ColumnDetails> _columns;
        private List<int> _mappedColumnIndices;

        private RowRemapper _sourceJoinedRowsFilter;

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

        public int CurrentBatchRowCount { get; private set; }

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

            DataBatch joinToRows = default(DataBatch);
            BitVector matchedRows = null;

            while (true)
            {
                // Get the next rows from the source
                int count = _source.Next(desiredCount);
                if (count == 0)
                {
                    CurrentBatchRowCount = 0;
                    return 0;
                }

                // Get values to join from
                DataBatch joinFromValues = _joinFromColumnGetter();

                // Find which rows matched and to what right-side row indices
                matchedRows = _joinDictionary.TryGetValues(joinFromValues, out joinToRows);

                // Filter left-side rows to the matches (inner join)
                _sourceJoinedRowsFilter.SetMatches(matchedRows);

                if (joinToRows.Count > 0) break;
            }

            // 'Seek' the right-side rows which matched
            _cachedJoinSource.Get(ArraySelector.Map((int[])joinToRows.Array, joinToRows.Count));

            CurrentBatchRowCount = joinToRows.Count;
            return joinToRows.Count;
        }

        private void BuildJoinDictionary()
        {
            int joinToTotalCount = _cachedJoinSource.Next(int.MaxValue);
            DataBatch allJoinToValues = _joinToColumnGetter();

            _joinDictionary = new JoinDictionary<String8>(joinToTotalCount, new String8Comparer());
            _joinDictionary.Add(allJoinToValues, 0);
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

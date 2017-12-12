// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Query;

namespace XForm.Aggregators
{
    internal class CountCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "count";
        public string Usage => "'count'";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new CountAggregator(source);
        }
    }

    public class CountAggregator : IDataBatchEnumerator
    {
        private List<ColumnDetails> _column;
        private IDataBatchEnumerator _source;
        private int _count;

        public CountAggregator(IDataBatchEnumerator source)
        {
            _source = source;

            _count = -1;

            _column = new List<ColumnDetails>();
            _column.Add(new ColumnDetails("Count", typeof(int), false));
        }

        public IReadOnlyList<ColumnDetails> Columns => _column;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            if (columnIndex != 0) throw new ArgumentOutOfRangeException("columnIndex");
            int[] result = new int[1];

            return () =>
            {
                result[0] = _count;
                return DataBatch.All(result, 1);
            };
        }

        public void Reset()
        {
            _count = -1;
            _source.Reset();
        }

        public int Next(int desiredCount)
        {
            // Return no more rows if this isn't the first call
            if (_count != -1) return 0;

            // If this is a List, just get the count
            if (_source is IDataBatchList)
            {
                _count = ((IDataBatchList)_source).Count;
            }
            else
            {
                // Accumulate count over all rows from source
                _count = 0;
                while (true)
                {
                    int batchCount = _source.Next(desiredCount);
                    if (batchCount == 0) break;
                    _count += batchCount;
                }
            }

            // Return that there's one row (the count)
            return 1;
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

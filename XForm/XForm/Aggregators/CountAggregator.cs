// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Query;

namespace XForm.Aggregators
{
    internal class CountCommandBuilder : IVerbBuilder
    {
        public string Verb => "count";
        public string Usage => "count";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            return new CountAggregator(source);
        }
    }

    public class CountAggregator : IXTable
    {
        private List<ColumnDetails> _column;
        private IXTable _source;
        private int _count;

        public CountAggregator(IXTable source)
        {
            _source = source;

            _count = -1;

            _column = new List<ColumnDetails>();
            _column.Add(new ColumnDetails("Count", typeof(int)));
        }

        public IReadOnlyList<ColumnDetails> Columns => _column;

        public int CurrentRowCount { get; private set; }

        public Func<XArray> ColumnGetter(int columnIndex)
        {
            if (columnIndex != 0) throw new ArgumentOutOfRangeException("columnIndex");
            int[] result = new int[1];

            return () =>
            {
                result[0] = _count;
                return XArray.All(result, 1);
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
            if (_count != -1)
            {
                CurrentRowCount = 0;
                return CurrentRowCount;
            }

            // If this is a List, just get the count
            if (_source is ISeekableXTable)
            {
                _count = ((ISeekableXTable)_source).Count;
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
            CurrentRowCount = 1;
            return CurrentRowCount;
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

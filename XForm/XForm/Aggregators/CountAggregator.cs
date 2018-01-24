// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using XForm.Columns;
using XForm.Data;
using XForm.Extensions;
using XForm.Functions;
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
        private SingleValueColumn[] _countColumn;
        private IXTable _source;
        private int _count;

        public CountAggregator(IXTable source)
        {
            _source = source;
            _count = -1;
            _countColumn = new SingleValueColumn[] { new SingleValueColumn(this, typeof(int)) };
        }

        public IReadOnlyList<IXColumn> Columns => _countColumn;
        public int CurrentRowCount { get; private set; }

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
                    int batchCount = _source.Next(Math.Max(desiredCount, XTableExtensions.DefaultBatchSize));
                    if (batchCount == 0) break;
                    _count += batchCount;
                }
            }

            // Set the count on the constant
            _countColumn[0].Set(_count);

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

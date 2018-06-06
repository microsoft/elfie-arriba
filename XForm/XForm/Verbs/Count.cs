// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XForm.Columns;
using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;

namespace XForm.Verbs
{
    internal class CountVerbBuilder : IVerbBuilder
    {
        public string Verb => "count";
        public string Usage => "count";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            return new Count(source);
        }
    }

    public class Count : IXTable
    {
        private SingleValueColumn[] _countColumn;
        private IXTable _source;
        private long _count;

        public Count(IXTable source)
        {
            if (source == null) throw new ArgumentNullException("source");

            _source = source;
            _count = -1;
            _countColumn = new SingleValueColumn[] { new SingleValueColumn(this, "Count", typeof(long)) };
        }

        public IReadOnlyList<IXColumn> Columns => _countColumn;
        public int CurrentRowCount { get; private set; }

        public void Reset()
        {
            _count = -1;
            _source.Reset();
        }

        public int Next(int desiredCount, CancellationToken cancellationToken)
        {
            // Return no more rows if this isn't the first call
            if (_count != -1)
            {
                CurrentRowCount = 0;
                return CurrentRowCount;
            }

            _count = CountSource(_source, desiredCount, cancellationToken);

            // Set the count on the constant
            _countColumn[0].Set(_count);

            // Return that there's one row (the count)
            CurrentRowCount = 1;
            return CurrentRowCount;
        }
        
        private static long CountSource(IXTable source, int desiredCount, CancellationToken cancellationToken)
        {
            if (source is ISeekableXTable)
            {
                // If this is a List, just get the count
                return ((ISeekableXTable)source).Count;
            }
            else if(source is ConcatenatedTable)
            {
                // If this is multiple tables, count them in parallel
                ConcatenatedTable cSource = (ConcatenatedTable)source;
                List<IXTable> parts = cSource.Sources.ToList();

                long[] counts = new long[parts.Count];
                Parallel.For(0, parts.Count, (i) => counts[i] = CountSource(parts[i], desiredCount, cancellationToken));
                return counts.Sum();
            }
            else
            {
                // Accumulate count over all rows from source
                long count = 0;
                while (true)
                {
                    int batchCount = source.Next(Math.Max(desiredCount, XTableExtensions.DefaultBatchSize), cancellationToken);
                    if (batchCount == 0) break;
                    count += batchCount;
                }

                return count;
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

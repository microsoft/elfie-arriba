// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Query;
using XForm.Query.Expression;
using XForm.Transforms;

namespace XForm.Verbs
{
    internal class WhereCommandBuilder : IVerbBuilder
    {
        public string Verb => "where";
        public string Usage => "'where' [Expression]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, XDatabaseContext context)
        {
            return new Where(source, context.Parser.NextExpression(source, context));
        }
    }

    public class Where : DataBatchEnumeratorWrapper
    {
        private IExpression _expression;
        private BitVector _vector;
        private RowRemapper _mapper;

        // Keep current filtered DataBatches from the source, to allow requesting more than the desired count
        private DataBatch[] _currentBatches;
        private int _currentMatchesTotal;
        private int _currentMatchesReturned;
        private int _nextCountToReturn;

        // Track the total rows we've gotten and returned
        private int _totalRowsRetrieved;
        private int _totalRowsMatched;

        public Where(IDataBatchEnumerator source, IExpression expression) : base(source)
        {
            _expression = expression;

            // Build a mapper to hold matching rows and remap source batches
            _mapper = new RowRemapper();

            // Allocate room to cache returned DataBatches
            _currentBatches = new DataBatch[source.Columns.Count];
        }

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Keep a column-specific array for remapping indices
            int[] remapArray = null;

            // Retrieve the column getter for this column
            Func<DataBatch> getter = _source.ColumnGetter(columnIndex);

            return () =>
            {
                // If we're done returning from the previous batch, ...
                if (_currentMatchesReturned == 0)
                {
                    // Get the batch from the source for this column
                    DataBatch batch = getter();

                    // Remap the DataBatch indices for this column for the rows which matched the clause
                    _currentBatches[columnIndex] = _mapper.Remap(batch, ref remapArray);
                }

                return _currentBatches[columnIndex].Slice(_currentMatchesReturned, _nextCountToReturn);
            };
        }

        private int CountToRequest(int desiredCount)
        {
            // By default, request the amount to return
            int result = desiredCount;

            // Request enough to get the desired match count, if we know the match percentage so far
            if(_totalRowsMatched > 0)
            {
                double rowsPerMatch = (double)_totalRowsRetrieved / (double)_totalRowsMatched;
                result = (int)(desiredCount * rowsPerMatch);
            }

            // Never request more than 16x the match count
            if (result > desiredCount * 16) result = desiredCount * 16;

            // Always request at least 100 rows
            if (result < 100) result = 100;

            return result;
        }

        public override int Next(int desiredCount)
        {
            _currentMatchesReturned += _nextCountToReturn;

            // If we previously retrieved extra rows, return more of those
            if (_currentMatchesTotal > _currentMatchesReturned)
            {
                _nextCountToReturn = Math.Min(desiredCount, _currentMatchesTotal - _currentMatchesReturned);
                return _nextCountToReturn;
            }

            // Otherwise, retrieve more rows
            int countToRequest = CountToRequest(desiredCount);
            _currentMatchesReturned = 0;

            int outerCount;
            while ((outerCount = _source.Next(countToRequest)) > 0)
            {
                // Track the total retrieved from the source
                _totalRowsRetrieved += outerCount;

                Allocator.AllocateToSize(ref _vector, outerCount);
                _vector.None();

                // Match the query expression and count matches
                _expression.Evaluate(_vector);
                _mapper.SetMatches(_vector);
                _currentMatchesTotal = _mapper.Count;
                _totalRowsMatched += _currentMatchesTotal;

                // If we got matches, return the first set requested, otherwise ask for more
                if (_currentMatchesTotal > 0)
                {
                    _nextCountToReturn = Math.Min(desiredCount, _currentMatchesTotal - _currentMatchesReturned);
                    return _nextCountToReturn;
                }
            }

            return 0;
        }
    }
}

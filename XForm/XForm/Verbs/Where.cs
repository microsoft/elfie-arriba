// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

using XForm.Columns;
using XForm.Data;
using XForm.Extensions;
using XForm.Query;
using XForm.Query.Expression;
using XForm.Transforms;

namespace XForm.Verbs
{
    internal class WhereCommandBuilder : IVerbBuilder
    {
        public string Verb => "where";
        public string Usage => "where {Expression}";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            // Where can be evaluated in parallel, so keep parallel
            return source.WrapParallel(context.Parser, (part) => new Where(part, context.Parser.NextExpression(part, context)));
        }
    }

    public class Where : XTableWrapper
    {
        private IExpression _expression;
        private BitVector _vector;
        private RowRemapper _mapper;

        private RemappedColumn[] _columns;

        // Keep current filtered arrays from the source, to allow requesting more than the desired count
        private int _currentMatchesTotal;
        private int _currentMatchesReturned;
        private int _nextCountToReturn;

        // Track the total rows we've gotten and returned
        private int _totalRowsRetrieved;
        private int _totalRowsMatched;

        public Where(IXTable source, IExpression expression) : base(source)
        {
            _expression = expression;

            // Build a mapper to hold matching rows and remap source arrays
            _mapper = new RowRemapper();

            // Build wrapper columns
            _columns = new RemappedColumn[source.Columns.Count];
            for (int i = 0; i < _columns.Length; ++i)
            {
                _columns[i] = new RemappedColumn(source.Columns[i], _mapper);
            }
        }

        public override IReadOnlyList<IXColumn> Columns => _columns;
        public override int CurrentRowCount => _nextCountToReturn;

        private int CountToRequest(int desiredCount)
        {
            // By default, request the amount to return
            int result = desiredCount;

            // Request enough to get the desired match count, if we know the match percentage so far
            if (_totalRowsMatched > 0)
            {
                double rowsPerMatch = (double)_totalRowsRetrieved / (double)_totalRowsMatched;
                result = (int)(desiredCount * rowsPerMatch);

                // Never request more than 16x the match count
                if (result > desiredCount * 16) result = desiredCount * 16;

                // Never request more than 2x DefaultBatchSize
                if (result > XTableExtensions.DefaultBatchSize) result = XTableExtensions.DefaultBatchSize;
            }
            else if (_totalRowsRetrieved > 1024)
            {
                // If more than 1,024 rows were searched with no matches, revert to full page size
                result = XTableExtensions.DefaultBatchSize;
            }

            // Always request at least 256 rows
            if (result < 256) result = 256;

            return result;
        }

        public override void Reset()
        {
            base.Reset();
            _currentMatchesTotal = 0;
            _currentMatchesReturned = 0;
            _nextCountToReturn = 0;
        }

        public override int Next(int desiredCount, CancellationToken cancellationToken)
        {
            _currentMatchesReturned += _nextCountToReturn;

            // If we previously retrieved extra rows, return more of those
            if (_currentMatchesTotal > _currentMatchesReturned)
            {
                _nextCountToReturn = Math.Min(desiredCount, _currentMatchesTotal - _currentMatchesReturned);
                _mapper.NextMatchPage(_nextCountToReturn);
                return _nextCountToReturn;
            }

            // Otherwise, retrieve more rows
            int countToRequest = CountToRequest(desiredCount);
            _currentMatchesReturned = 0;

            int outerCount;
            while ((outerCount = _source.Next(countToRequest, cancellationToken)) > 0)
            {
                // Track the total retrieved from the source
                _totalRowsRetrieved += outerCount;

                Allocator.AllocateToSize(ref _vector, outerCount);
                _vector.None();

                // Match the query expression and count all matches
                _expression.Evaluate(_vector);

                _currentMatchesTotal = _vector.Count;
                _totalRowsMatched += _currentMatchesTotal;

                // If we got matches, return the first set requested, otherwise ask for more
                if (_currentMatchesTotal > 0)
                {
                    _nextCountToReturn = Math.Min(desiredCount, _currentMatchesTotal - _currentMatchesReturned);
                    _mapper.SetMatches(_vector, _nextCountToReturn);
                    return _nextCountToReturn;
                }

                // Reconsider how many rows to request if no matches
                countToRequest = CountToRequest(desiredCount);
            }

            // Tell the mapper there are no more matches
            Allocator.AllocateToSize(ref _vector, desiredCount);
            _vector.None();
            _mapper.SetMatches(_vector, 0);

            return 0;

            // Where.Next implementation which doesn't request excess results and page through them.
            // Use instead to diagnose "wrong row count" problems which might be due to paging problems.
            //int outerCount;
            //while ((outerCount = _source.Next(desiredCount)) > 0)
            //{
            //    // Track the total retrieved from the source
            //    _totalRowsRetrieved += outerCount;

            //    Allocator.AllocateToSize(ref _vector, outerCount);
            //    _vector.None();

            //    // Match the query expression and count all matches
            //    _expression.Evaluate(_vector);
            //    _currentMatchesTotal = _vector.Count;
            //    _totalRowsMatched += _currentMatchesTotal;

            //    _mapper.SetMatches(_vector);
            //    if (_currentMatchesTotal > 0) return _currentMatchesTotal;
            //}

            //return 0;
        }
    }
}

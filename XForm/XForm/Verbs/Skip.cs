// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

using XForm.Data;
using XForm.Query;

namespace XForm.Verbs
{
    internal class SkipCommandBuilder : IVerbBuilder
    {
        public string Verb => "skip";
        public string Usage => "skip {RowCount}";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            int countToSkip = context.Parser.NextInteger();
            return new Skip(source, countToSkip);
        }
    }

    /// <summary>
    ///  'Skip' skips the first [count] rows from the source.
    ///  Skip and Limit can be used to do paging, but the service must re-run the query each time.
    /// </summary>
    /// <remarks>
    ///  Ideal paging would be by identifying the source table RowId which was last returned and seeking past it to start.
    ///  When 'Order By' is available, a Where clause to skip already returned rows would be more efficient.
    /// </remarks>
    public class Skip : XTableWrapper
    {
        private int _countToSkip;
        private int _rowCountSkipped;

        public Skip(IXTable source, int countToSkip) : base(source)
        {
            _countToSkip = countToSkip;
        }

        public override void Reset()
        {
            base.Reset();
            _rowCountSkipped = 0;
        }

        public override int Next(int desiredCount, CancellationToken cancellationToken)
        {
            // Skip the desired number of rows
            while(_countToSkip > _rowCountSkipped)
            {
                int rowsToRequest = Math.Min(desiredCount, _countToSkip - _rowCountSkipped);
                
                int rowsReturned = _source.Next(rowsToRequest, cancellationToken);
                if (rowsReturned == 0) return 0;

                _rowCountSkipped += rowsReturned;
            }

            // Then, just pass through
            return _source.Next(desiredCount, cancellationToken);
        }
    }
}

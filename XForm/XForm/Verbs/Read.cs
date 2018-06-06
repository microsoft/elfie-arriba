// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using XForm.Data;
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Query;

namespace XForm.Verbs
{
    internal class ReadCommandBuilder : IVerbBuilder
    {
        public string Verb => "read";
        public string Usage => "read {Table|Query}";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            if (source != null) throw new ArgumentException($"'read' must be the first stage in a pipeline.");
            return context.Parser.NextTableSource();
        }
    }

    internal class ReadRangeCommandBuilder : IVerbBuilder
    {
        public string Verb => "readrange";
        public string Usage => "readRange {TimeSpanUpToAsOfDate} {TableName}";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            if (source != null) throw new ArgumentException($"'read' must be the first stage in a pipeline.");

            List<IXTable> sources = new List<IXTable>();

            // Identify the interval and table name requested
            TimeSpan interval = context.Parser.NextTimeSpan();
            if (interval < TimeSpan.Zero) throw new ArgumentException($"'interval' must be positive. (For last 7 days, use '7d')");
            string tableName = (string)context.Parser.NextLiteralValue();

            // Determine the range of versions to include (from the as of date or now if not provided)
            DateTime rangeEnd = (context.RequestedAsOfDateTime == DateTime.MaxValue ? DateTime.UtcNow : context.RequestedAsOfDateTime);
            DateTime rangeStart = rangeEnd.Subtract(interval);

            // Find versions available
            ItemVersions versions = context.StreamProvider.ItemVersions(LocationType.Source, tableName);
            if (versions.Versions.Count == 0) versions = context.StreamProvider.ItemVersions(LocationType.Table, tableName);
            if (versions.Versions.Count == 0) throw new ArgumentException($"'{tableName}' was not found as a Source or Table.");

            // Find the first version to include (if any) - the last full version before the start was 'current' at the start moment
            ItemVersion previous = versions.LatestBeforeCutoff(CrawlType.Full, rangeStart);

            XDatabaseContext historicalContext;
            foreach (ItemVersion version in versions.VersionsInRange(CrawlType.Full, rangeStart, rangeEnd))
            {
                // Add the version before this one, if any (including any incremental pieces)
                if (previous != null)
                {
                    historicalContext = new XDatabaseContext(context);
                    historicalContext.RequestedAsOfDateTime = version.AsOfDate.AddSeconds(-1);
                    sources.Add(context.Runner.Build(tableName, historicalContext));
                }

                previous = version;
            }

            // Add 'last' up to the requested moment
            historicalContext = new XDatabaseContext(context);
            historicalContext.RequestedAsOfDateTime = rangeEnd;
            sources.Add(context.Runner.Build(tableName, historicalContext));

            // Communicate the latest component as of date back to the builder
            historicalContext.Pop(context);

            // Return the source(s) found
            return ConcatenatedTable.Build(sources);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

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
            DateTime rangeStart = context.RequestedAsOfDateTime.Subtract(interval);
            string tableName = (string)context.Parser.NextLiteralValue();

            // Add rows *just before* each full source in range (the previous full crawl and all incremental ones)
            foreach (ItemVersion fullSource in context.StreamProvider.ItemVersions(LocationType.Source, tableName).VersionsInRange(CrawlType.Full, rangeStart, context.RequestedAsOfDateTime))
            {
                // Ask for the state just before this source
                XDatabaseContext historicalContext = new XDatabaseContext(context);
                historicalContext.RequestedAsOfDateTime = fullSource.AsOfDate.AddSeconds(-1);
                sources.Add(context.Runner.Build(tableName, historicalContext));
            }

            // Add the last full source and incremental ones up to the RequestedAsOfDateTime
            sources.Add(context.Runner.Build(tableName, context));

            // Return the source(s) found
            if (sources.Count == 1) return sources[0];

            throw new NotImplementedException("ConcatenatingReader");
            //return new ConcatenatingReader(sources);
        }
    }
}

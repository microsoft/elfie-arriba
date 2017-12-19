// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Query;

namespace XForm.Commands
{
    internal class ReadCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "read";
        public string Usage => "'read' [tableNameOrFilePath]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            if (source != null) throw new ArgumentException($"'read' must be the first stage in a pipeline.");
            return context.Parser.NextTableSource();
        }
    }

    internal class ReadRangeCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "readrange";
        public string Usage => "'readRange' [relativeTime] [tableNameOrFilePath]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            if (source != null) throw new ArgumentException($"'read' must be the first stage in a pipeline.");

            List<IDataBatchEnumerator> sources = new List<IDataBatchEnumerator>();

            // Identify the interval and table name requested
            TimeSpan interval = context.Parser.NextTimeSpan();
            DateTime rangeStart = context.RequestedAsOfDateTime.Subtract(interval);
            string tableName = context.Parser.NextString();

            // Add rows *just before* each full source in range (the previous full crawl and all incremental ones)
            foreach (StreamAttributes fullSource in context.StreamProvider.VersionsInRange(LocationType.Source, tableName, CrawlType.Full, rangeStart, context.RequestedAsOfDateTime))
            {
                // Ask for the state just before this source
                WorkflowContext historicalContext = new WorkflowContext(context);
                historicalContext.RequestedAsOfDateTime = fullSource.WhenModifiedUtc.AddSeconds(-1);
                sources.Add(context.Runner.Build(tableName, historicalContext));
            }

            // Add the last full source and incremental ones up to the RequestedAsOfDateTime
            sources.Add(context.Runner.Build(tableName, context));

            // Return the source(s) found
            if (sources.Count == 1) return sources[0];
            return new ConcatenatingReader(sources);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Extensions;

using XForm.Context;
using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Query;

namespace XForm
{
    public class WorkflowRunner : IWorkflowRunner
    {
        private WorkflowContext WorkflowContext { get; set; }
        private HashSet<string> Sources { get; set; }

        public WorkflowRunner(WorkflowContext context)
        {
            this.WorkflowContext = context;
        }

        public IEnumerable<string> SourceNames
        {
            get
            {
                if (Sources == null)
                {
                    Sources = new HashSet<string>(WorkflowContext.StreamProvider.Tables(), StringComparer.OrdinalIgnoreCase);
                    Sources.UnionWith(WorkflowContext.StreamProvider.Queries());
                }

                return Sources;
            }
        }

        public IDataBatchEnumerator Build(string tableName, WorkflowContext outerContext)
        {
            return Build(tableName, outerContext, false);
        }

        public IDataBatchEnumerator Build(string tableName, WorkflowContext outerContext, bool deferred)
        {
            // Create a context to track what we're building now
            WorkflowContext innerContext = WorkflowContext.Push(outerContext);
            innerContext.Runner = this;
            innerContext.CurrentTable = tableName;

            // If this is a query, there won't be a cached table - just build a pipeline to make it
            StreamAttributes queryAttributes = innerContext.StreamProvider.Attributes(innerContext.StreamProvider.Path(LocationType.Query, tableName, ".xql"));
            if (queryAttributes.Exists)
            {
                IDataBatchEnumerator queryPipeline = XqlParser.Parse(innerContext.StreamProvider.ReadAllText(queryAttributes.Path), null, innerContext);
                innerContext.Pop(outerContext);
                return queryPipeline;
            }

            // Find the latest already built result, and associated query
            StreamAttributes latestTableAttributes = innerContext.StreamProvider.LatestBeforeCutoff(LocationType.Table, tableName, CrawlType.Full, outerContext.RequestedAsOfDateTime);
            string latestTableQuery = "";
            if (latestTableAttributes.Exists)
            {
                using (BinaryTableReader reader = new BinaryTableReader(outerContext.StreamProvider, latestTableAttributes.Path))
                {
                    latestTableQuery = reader.Query;
                }
            }

            // Set the dependency date to the latest table we've already built (if any)
            innerContext.NewestDependency = latestTableAttributes.WhenModifiedUtc;

            // Determine the XQL to build the table and construct a builder which can do so
            string xql;
            IDataBatchEnumerator builder;

            // Find the config to build the table
            StreamAttributes configAttributes = innerContext.StreamProvider.Attributes(innerContext.StreamProvider.Path(LocationType.Config, tableName, ".xql"));
            if (!configAttributes.Exists)
            {
                // If this is a simple source, just reading it is how to build it
                xql = $"read {XqlScanner.Escape(tableName, TokenType.Value)}";

                // Build a reader concatenating all needed pieces
                builder = ReadSource(tableName, innerContext);
            }
            else
            {
                // If there is a config, the config is how to build it
                xql = innerContext.StreamProvider.ReadAllText(configAttributes.Path);

                // Build a pipeline for the query, recursively creating dependencies
                builder = XqlParser.Parse(xql, null, innerContext);
            }

            // Get the path we're either reading or building
            string tablePath = innerContext.StreamProvider.Path(LocationType.Table, tableName, CrawlType.Full, innerContext.NewestDependency);

            // If sources rebuilt, the query changed, or the latest output isn't up-to-date, rebuild it
            if (innerContext.RebuiltSomething || xql != latestTableQuery || IsOutOfDate(latestTableAttributes.WhenModifiedUtc, innerContext.NewestDependency))
            {
                // If we're not running now, just return how to build it
                if (deferred) return builder;

                // Otherwise, build it now; we'll return the query to read the output
                innerContext.CurrentQuery = xql;
                Trace.WriteLine($"COMPUTE: [{innerContext.NewestDependency.ToString(StreamProviderExtensions.DateTimeFolderFormat)}] {tableName}");
                new BinaryTableWriter(builder, innerContext, tablePath).RunAndDispose();
                innerContext.RebuiltSomething = true;
            }

            // Report the newest dependency in this chain to the components above
            innerContext.Pop(outerContext);

            return new BinaryTableReader(innerContext.StreamProvider, tablePath);
        }

        public IDataBatchEnumerator ReadSource(string tableName, WorkflowContext context)
        {
            List<IDataBatchEnumerator> sources = new List<IDataBatchEnumerator>();

            // Find the latest source of this type
            StreamAttributes latestFullSourceAttributes = context.StreamProvider.LatestBeforeCutoff(LocationType.Source, tableName, CrawlType.Full, context.RequestedAsOfDateTime);
            if (!latestFullSourceAttributes.Exists) throw new UsageException(tableName, "tableName", context.StreamProvider.SourceNames());

            // Find the latest already converted table
            StreamAttributes latestBuiltTableAttributes = context.StreamProvider.LatestBeforeCutoff(LocationType.Table, tableName, CrawlType.Full, context.RequestedAsOfDateTime);

            // Read the Table or the Full Crawl Source, whichever is newer
            DateTime incrementalNeededAfterCutoff;
            if (latestBuiltTableAttributes.Exists && !IsOutOfDate(latestBuiltTableAttributes.WhenModifiedUtc, latestFullSourceAttributes.WhenModifiedUtc))
            {
                // If the table is current, reuse it
                sources.Add(new BinaryTableReader(context.StreamProvider, latestBuiltTableAttributes.Path));
                incrementalNeededAfterCutoff = latestBuiltTableAttributes.WhenModifiedUtc;
            }
            else
            {
                // Otherwise, build a new table from the latest source full crawl
                sources.AddRange(context.StreamProvider.Enumerate(latestFullSourceAttributes.Path, EnumerateTypes.File, true).Select((sa) => new TabularFileReader(context.StreamProvider, sa.Path)));
                incrementalNeededAfterCutoff = latestFullSourceAttributes.WhenModifiedUtc;
            }

            // Add incremental crawls between the full source and the reporting date
            DateTime latestComponent = incrementalNeededAfterCutoff;

            foreach (StreamAttributes incrementalCrawl in context.StreamProvider.VersionsInRange(LocationType.Source, tableName, CrawlType.Inc, incrementalNeededAfterCutoff, context.RequestedAsOfDateTime))
            {
                sources.AddRange(context.StreamProvider.Enumerate(incrementalCrawl.Path, EnumerateTypes.File, true).Select((sa) => new TabularFileReader(context.StreamProvider, sa.Path)));
                latestComponent = latestComponent.BiggestOf(incrementalCrawl.WhenModifiedUtc);
            }

            // Report the latest incorporated source back
            context.NewestDependency = latestComponent;

            // Return the source (if a single) or concatenated group (if multiple parts)
            if (sources.Count == 1) return sources[0];
            return new ConcatenatingReader(sources);
        }

        private bool IsOutOfDate(DateTime outputWhenModifiedUtc, DateTime inputsWhenModifiedUtc)
        {
            // An output is out of date when the inputs are at least one second newer.
            // This "fuzzyness" is because input files can have partial second freshness but written tables don't.
            return inputsWhenModifiedUtc - outputWhenModifiedUtc > TimeSpan.FromSeconds(1);
        }
    }

    public class DeferredRunner : IWorkflowRunner
    {
        private WorkflowRunner _inner;

        public DeferredRunner(WorkflowRunner inner)
        {
            _inner = inner;
        }

        public IEnumerable<string> SourceNames => _inner.SourceNames;

        public IDataBatchEnumerator Build(string tableName, WorkflowContext context)
        {
            // Ask the workflow runner to defer computing dependencies now
            return _inner.Build(tableName, context, true);
        }
    }

    public static class ReportWriter
    {
        public static string Build(string tableName, WorkflowContext context, string outputFormat)
        {
            IDataBatchEnumerator builder = null;

            try
            {
                Stopwatch w = Stopwatch.StartNew();

                // Reset the dependency DateTime check
                context.NewestDependency = DateTime.MinValue;

                // Recursively build dependencies and return a reader for the result table
                builder = context.Runner.Build(tableName, context);

                string outputPath;

                if ((String.IsNullOrEmpty(outputFormat) || outputFormat.Equals("xform", StringComparison.OrdinalIgnoreCase)) && builder is BinaryTableReader)
                {
                    // If the binary format was requested and we already built it, return the path written
                    outputPath = ((BinaryTableReader)builder).TablePath;
                }
                else
                {
                    StreamAttributes attributes = context.StreamProvider.LatestBeforeCutoff(LocationType.Report, tableName, CrawlType.Full, context.RequestedAsOfDateTime);
                    outputPath = Path.Combine(context.StreamProvider.Path(LocationType.Report, tableName, CrawlType.Full, attributes.WhenModifiedUtc), $"Report.{outputFormat}");

                    if (attributes.WhenModifiedUtc < context.NewestDependency || context.RebuiltSomething || !context.StreamProvider.Attributes(outputPath).Exists)
                    {
                        // If the report needs to be rebuilt, make it and return the path
                        outputPath = Path.Combine(context.StreamProvider.Path(LocationType.Report, tableName, CrawlType.Full, context.NewestDependency), $"Report.{outputFormat}");
                        if (outputFormat.Equals("xform", StringComparison.OrdinalIgnoreCase))
                        {
                            new BinaryTableWriter(builder, context, outputPath).RunAndDispose();
                        }
                        else
                        {
                            new TabularFileWriter(builder, context.StreamProvider, outputPath).RunAndDispose();
                        }

                        context.RebuiltSomething = true;
                    }
                }

                w.Stop();
                Trace.WriteLine($"Done. \"{outputPath}\" {(context.RebuiltSomething ? "written" : "up-to-date")} in {w.Elapsed.ToFriendlyString()}.");
                return outputPath;
            }
            finally
            {
                if (builder != null)
                {
                    builder.Dispose();
                    builder = null;
                }
            }
        }
    }
}

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
                IDataBatchEnumerator queryPipeline = PipelineParser.BuildPipeline(innerContext.StreamProvider.ReadAllText(queryAttributes.Path), null, innerContext);
                innerContext.Pop(outerContext);
                return queryPipeline;
            }

            // Find the config to build this, the latest source, and the latest output
            StreamAttributes configAttributes = innerContext.StreamProvider.Attributes(innerContext.StreamProvider.Path(LocationType.Config, tableName, ".xql"));
            StreamAttributes latestSourceAttributes = innerContext.StreamProvider.LatestBeforeCutoff(LocationType.Source, tableName, outerContext.RequestedAsOfDateTime);
            StreamAttributes latestTableAttributes = innerContext.StreamProvider.LatestBeforeCutoff(LocationType.Table, tableName, outerContext.RequestedAsOfDateTime);
            string latestTableQuery = "";
            if (latestTableAttributes.Exists)
            {
                using (BinaryTableReader reader = new BinaryTableReader(outerContext.StreamProvider, latestTableAttributes.Path))
                {
                    latestTableQuery = reader.Query;
                }
            }

            // If the Config doesn't exist and there's no source, throw
            if (!configAttributes.Exists && !latestSourceAttributes.Exists) throw new UsageException(tableName, "tableName", outerContext.StreamProvider.SourceNames());

            // Set the dependency date to the latest table we've already built (if any)
            innerContext.NewestDependency = latestTableAttributes.WhenModifiedUtc;

            // Determine the XQL to build the table and construct a builder which can do so
            string xql;
            IDataBatchEnumerator builder;

            if (!configAttributes.Exists)
            {
                // If there's no config, it's a simple conversion. Is the table up-to-date?
                innerContext.NewestDependency = innerContext.NewestDependency.BiggestOf(latestSourceAttributes.WhenModifiedUtc);

                // Find the input file itself
                IEnumerable<StreamAttributes> sourceFiles = innerContext.StreamProvider.Enumerate(latestSourceAttributes.Path, EnumerateTypes.File, true);
                if (sourceFiles.Count() > 1) throw new NotImplementedException("Need concatenating reader");

                // Construct a pipeline to read the raw file only
                xql = $"read {PipelineScanner.Escape(tableName)}";
                builder = new TabularFileReader(innerContext.StreamProvider, sourceFiles.First().Path);
            }
            else
            {
                // If there is a config, the config is how to build it
                xql = innerContext.StreamProvider.ReadAllText(configAttributes.Path);

                // Build a pipeline for the query, recursively creating dependencies
                builder = PipelineParser.BuildPipeline(xql, null, innerContext);
            }

            // Get the path we're either reading or building
            string tablePath = innerContext.StreamProvider.Path(LocationType.Table, tableName, CrawlType.Full, innerContext.NewestDependency);

            // If sources rebuilt, the query changed, or the latest output isn't up-to-date, rebuild it
            if (innerContext.RebuiltSomething || xql != latestTableQuery || innerContext.NewestDependency - latestTableAttributes.WhenModifiedUtc > TimeSpan.FromSeconds(1))
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
                    return ((BinaryTableReader)builder).TablePath;
                }
                else
                {
                    StreamAttributes attributes = context.StreamProvider.LatestBeforeCutoff(LocationType.Report, tableName, context.RequestedAsOfDateTime);
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

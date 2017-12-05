// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Extensions;

using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;

namespace XForm
{


    public class WorkflowRunner : IWorkflowRunner
    {
        public IStreamProvider StreamProvider { get; private set; }
        private DateTime AsOfDateTime { get; set; }

        private HashSet<string> Tables { get; set; }
        private HashSet<string> Queries { get; set; }

        public WorkflowRunner(IStreamProvider streamProvider, DateTime asOfDateTime)
        {
            this.StreamProvider = streamProvider;
            this.AsOfDateTime = asOfDateTime;

            IdentifySources();
        }

        private void IdentifySources()
        {
            // TODO: Don't cache this full list in advance. The enumeration could be expensive for external locations.
            Tables = new HashSet<string>(this.StreamProvider.Tables(), StringComparer.OrdinalIgnoreCase);
            Queries = new HashSet<string>(this.StreamProvider.Queries(), StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> SourceNames => Tables.Concat(Queries);

        public IDataBatchEnumerator Build(string tableName, WorkflowContext outerContext)
        {
            return Build(tableName, outerContext, false);
        }

        public IDataBatchEnumerator Build(string tableName, WorkflowContext outerContext, bool deferred)
        {
            // If this is a query, there won't be a cached table - just build a pipeline to make it
            StreamAttributes queryAttributes = StreamProvider.Attributes(StreamProvider.Path(LocationType.Query, tableName, ".xql"));
            if (queryAttributes.Exists) return PipelineParser.BuildPipeline(StreamProvider.ReadAllText(queryAttributes.Path), null, outerContext);

            // Find the config to build this, the latest source, and the latest output
            StreamAttributes configAttributes = StreamProvider.Attributes(StreamProvider.Path(LocationType.Config, tableName, ".xql"));
            StreamAttributes latestSourceAttributes = StreamProvider.LatestBeforeCutoff(LocationType.Source, tableName, AsOfDateTime);
            StreamAttributes latestTableAttributes = StreamProvider.LatestBeforeCutoff(LocationType.Table, tableName, AsOfDateTime);
            string latestTableQuery = (latestTableAttributes.Exists ? StreamProvider.ReadAllText(Path.Combine(latestTableAttributes.Path, "Config.xql")) : "");
            
            // If the Config doesn't exist and there's no source, throw
            if (!configAttributes.Exists && !latestSourceAttributes.Exists) throw new UsageException(tableName, "tableName", SourceNames);

            // Create a context to track the version of this table we need (the latest dependency recursively found)
            WorkflowContext innerContext = WorkflowContext.Push(outerContext);
            innerContext.Runner = this;
            innerContext.StreamProvider = StreamProvider;
            innerContext.NewestDependency = latestTableAttributes.WhenModifiedUtc;

            // Determine the XQL to build the table and construct a builder which can do so
            string xql;
            IDataBatchEnumerator builder;

            if (!configAttributes.Exists)
            {
                // If there's no config, it's a simple conversion. Is the table up-to-date?
                innerContext.NewestDependency = innerContext.NewestDependency.BiggestOf(latestSourceAttributes.WhenModifiedUtc);

                // Find the input file itself
                IEnumerable<StreamAttributes> sourceFiles = StreamProvider.Enumerate(latestSourceAttributes.Path, true);
                if (sourceFiles.Count() > 1) throw new NotImplementedException("Need concatenating reader");

                // Construct a pipeline to read the raw file only
                xql = $"read \"{sourceFiles.First().Path}\"";
                builder = PipelineParser.BuildPipeline(xql, null, null);
            }
            else
            { 
                // If there is a config, the config is how to build it
                xql = StreamProvider.ReadAllText(configAttributes.Path);

                // Build a pipeline for the query, recursively creating dependencies
                builder = PipelineParser.BuildPipeline(xql, null, innerContext);
            }

            // Get the path we're either reading or building
            string tablePath = StreamProvider.Path(LocationType.Table, tableName, CrawlType.Full, innerContext.NewestDependency);

            // If sources rebuilt, the query changed, or the latest output isn't up-to-date, rebuild it
            if (innerContext.RebuiltSomething || xql != latestTableQuery || innerContext.NewestDependency - latestTableAttributes.WhenModifiedUtc > TimeSpan.FromSeconds(1))
            {
                // If we're not running now, just return how to build it
                if (deferred) return builder;

                // Otherwise, build it now; we'll return the query to read the output
                Trace.WriteLine($"COMPUTE: [{innerContext.NewestDependency.ToString(StreamProviderExtensions.DateTimeFolderFormat)}] {tableName}");
                new BinaryTableWriter(builder, tablePath).RunAndDispose();
                StreamProvider.WriteAllText(Path.Combine(tablePath, "Config.xql"), xql);
                innerContext.RebuiltSomething = true;
            }

            // Report the newest dependency in this chain to the components above
            innerContext.Pop(outerContext);

            return new BinaryTableReader(tablePath);
        }

        public string Build(string tableName, string outputFormat)
        {
            WorkflowContext context = new WorkflowContext(this, StreamProvider);
            IDataBatchEnumerator builder = null;

            try
            {
                Stopwatch w = Stopwatch.StartNew();

                // Recursively build dependencies and return a reader for the result table
                builder = Build(tableName, context);

                string outputPath;

                if (String.IsNullOrEmpty(outputFormat) || outputFormat.Equals("xform", StringComparison.OrdinalIgnoreCase))
                {
                    // If the binary format was requested, we've already created it
                    outputPath = ((BinaryTableReader)builder).TablePath;
                }
                else
                {
                    StreamAttributes attributes = StreamProvider.LatestBeforeCutoff(LocationType.Report, tableName, AsOfDateTime);
                    outputPath = Path.Combine(StreamProvider.Path(LocationType.Report, tableName, CrawlType.Full, attributes.WhenModifiedUtc), $"Report.{outputFormat}");

                    if (attributes.WhenModifiedUtc < context.NewestDependency || context.RebuiltSomething || !StreamProvider.Attributes(outputPath).Exists)
                    {
                        // If the report needs to be rebuilt, make it and return the path
                        outputPath = Path.Combine(StreamProvider.Path(LocationType.Report, tableName, CrawlType.Full, context.NewestDependency), $"Report.{outputFormat}");
                        new TabularFileWriter(builder, outputPath).RunAndDispose();
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

        public int Add(string sourceFileOrFolderPath, string tableName, CrawlType crawlType, DateTime asOfDateTime = default(DateTime))
        {
            // If the 'asOfDateTime' wasn't passed, use the File Write Time
            if (asOfDateTime == default(DateTime)) asOfDateTime = File.GetLastWriteTimeUtc(sourceFileOrFolderPath);

            string desiredFolderPath = StreamProvider.Path(LocationType.Source, tableName, crawlType, asOfDateTime);

            if (Directory.Exists(sourceFileOrFolderPath))
            {
                foreach (string filePath in Directory.GetFiles(sourceFileOrFolderPath, "*.*", SearchOption.AllDirectories))
                {
                    StreamProvider.Copy(File.OpenRead(filePath), Path.Combine(desiredFolderPath, Path.GetFileName(filePath)));
                }
            }
            else
            {
                StreamProvider.Copy(File.OpenRead(sourceFileOrFolderPath), Path.Combine(desiredFolderPath, Path.GetFileName(sourceFileOrFolderPath)));
            }

            // TODO: Delete old copies if there are too many

            Trace.WriteLine($"Done. Added \"{desiredFolderPath}\" to database.");
            return 0;
        }

        public void SaveXql(LocationType type, string tableName, string xql)
        {
            string queryPath = StreamProvider.Path(type, tableName, ".xql");
            StreamProvider.WriteAllText(queryPath, xql);
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
}

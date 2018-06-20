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
        private XDatabaseContext XDatabaseContext { get; set; }

        private HashSet<string> _sources;
        private DateTime _sourcesCacheExpires;
        private Cache<ItemVersions> _versionCache;
        private Cache<LatestTableForCutoff> _currentTableVersions;

        public WorkflowRunner(XDatabaseContext context)
        {
            this.XDatabaseContext = context;
            _versionCache = new Cache<ItemVersions>();
            _currentTableVersions = new Cache<LatestTableForCutoff>();
            _sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> SourceNames => Sources;
        public HashSet<string> Sources
        {
            get
            {
                if (DateTime.UtcNow > _sourcesCacheExpires) UpdateSources();
                return _sources;
            }
        }

        private void UpdateSources()
        {
            lock (_sources)
            {
                _sources.Clear();
                _sources.UnionWith(XDatabaseContext.StreamProvider.Tables());
                _sources.UnionWith(XDatabaseContext.StreamProvider.Queries());
                _sourcesCacheExpires = DateTime.UtcNow.Add(Cache<ItemVersions>.DefaultCacheDuration);
            }
        }

        public IXTable Build(string tableName, XDatabaseContext outerContext)
        {
            return Build(tableName, outerContext, false);
        }

        public IXTable Build(string tableName, XDatabaseContext outerContext, bool deferred)
        {
            // Validate the source name is recognized
            if (!Sources.Contains(tableName))
            {
                // If it wasn't in cache, check individually for it live
                if (!XDatabaseContext.StreamProvider.ContainsTable(tableName)) throw new UsageException(tableName, "Table", Sources);

                // If found, update the cache
                UpdateSources();
            }

            // If only a Date was passed for AsOfDate, look for the last version as of that day
            if(outerContext.RequestedAsOfDateTime.TimeOfDay == TimeSpan.Zero)
            {
                outerContext.RequestedAsOfDateTime = outerContext.RequestedAsOfDateTime.AddDays(1).AddSeconds(-1);
            }

            // If we previously found the latest for this table, just return it again
            LatestTableForCutoff previousLatest;
            if (_currentTableVersions.TryGet(tableName, out previousLatest)
                && previousLatest.Cutoff >= outerContext.RequestedAsOfDateTime
                && previousLatest.TableVersion.AsOfDate <= outerContext.RequestedAsOfDateTime
                && TableMetadataSerializer.UncachedExists(outerContext.StreamProvider, previousLatest.TableVersion.Path))
            {
                outerContext.NewestDependency = previousLatest.TableVersion.AsOfDate;
                return BinaryTableReader.Build(outerContext.StreamProvider, previousLatest.TableVersion.Path);
            }

            // Create a context to track what we're building now
            XDatabaseContext innerContext = XDatabaseContext.Push(outerContext);
            innerContext.CurrentTable = tableName;

            // If this is a query, there won't be a cached table - just build a pipeline to make it
            StreamAttributes queryAttributes = innerContext.StreamProvider.Attributes(innerContext.StreamProvider.Path(LocationType.Query, tableName, ".xql"));
            if (queryAttributes.Exists)
            {
                IXTable queryPipeline = innerContext.Query(innerContext.StreamProvider.ReadAllText(queryAttributes.Path));
                innerContext.Pop(outerContext);
                return queryPipeline;
            }

            // Find the latest already built result, and associated query
            ItemVersions tableVersions = innerContext.StreamProvider.ItemVersions(LocationType.Table, tableName);
            ItemVersion latestTable = tableVersions.LatestBeforeCutoff(CrawlType.Full, outerContext.RequestedAsOfDateTime);
            string latestTableQuery = null;
            if (latestTable != null)
            {
                latestTableQuery = TableMetadataSerializer.Read(outerContext.StreamProvider, latestTable.Path).Query;
            }

            // Set the dependency date to the latest table we've already built (if any)
            innerContext.NewestDependency = (latestTable == null ? DateTime.MinValue : latestTable.AsOfDate);

            // Determine the XQL to build the table and construct a builder which can do so
            string xql;
            IXTable builder;

            // Find the config to build the table and scan dependency versions to determine whether table is out-of-date
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
                builder = innerContext.Query(xql);
            }

            // If we don't have the table or the source, we have to throw
            if (latestTable == null && builder == null) throw new UsageException(tableName, "Table", innerContext.StreamProvider.Tables());

            // Get the path we're either reading or building
            string tablePath = innerContext.StreamProvider.Path(LocationType.Table, tableName, CrawlType.Full, innerContext.NewestDependency);

            // If we can rebuild this table and we need to (sources rebuilt, query changed, out-of-date, deleted), rebuild it
            if (builder != null)
            {
                if (latestTable == null 
                    || innerContext.RebuiltSomething 
                    || (latestTableQuery != null && xql != latestTableQuery) 
                    || IsOutOfDate(latestTable.AsOfDate, innerContext.NewestDependency) 
                    || !TableMetadataSerializer.UncachedExists(outerContext.StreamProvider, latestTable.Path))
                {
                    // If we're not running now, just return how to build it
                    if (deferred) return builder;

                    // Otherwise, build it now; we'll return the query to read the output
                    innerContext.CurrentQuery = xql;
                    Trace.WriteLine($"COMPUTE: [{innerContext.NewestDependency.ToString(StreamProviderExtensions.DateTimeFolderFormat)}] {tableName}");
                    BinaryTableWriter.Build(builder, innerContext, tablePath).RunAndDispose();
                    innerContext.RebuiltSomething = true;
                }
            }

            // Report the newest dependency in this chain to the components above
            innerContext.Pop(outerContext);

            _currentTableVersions.Add(tableName, new LatestTableForCutoff(outerContext.RequestedAsOfDateTime, new ItemVersion(LocationType.Table, tableName, CrawlType.Full, innerContext.NewestDependency)));
            return BinaryTableReader.Build(innerContext.StreamProvider, tablePath);
        }

        public IXTable ReadSource(string tableName, XDatabaseContext context)
        {
            List<IXTable> sources = new List<IXTable>();

            // Find the latest source of this type
            ItemVersions sourceVersions = context.StreamProvider.ItemVersions(LocationType.Source, tableName);
            ItemVersion latestFullSource = sourceVersions.LatestBeforeCutoff(CrawlType.Full, context.RequestedAsOfDateTime);

            // If there are no sources, there's nothing to rebuild from
            if (sourceVersions.Versions == null || sourceVersions.Versions.Count == 0) return null;

            // Find the latest already converted table
            ItemVersions tableVersions = context.StreamProvider.ItemVersions(LocationType.Table, tableName);
            ItemVersion latestBuiltTable = tableVersions.LatestBeforeCutoff(CrawlType.Full, context.RequestedAsOfDateTime);

            // If no source or table was found, throw
            if (latestFullSource == null && latestBuiltTable == null) throw new UsageException(tableName, "[Table]", context.StreamProvider.SourceNames());

            // Read the Table or the Full Crawl Source, whichever is newer
            DateTime incrementalNeededAfterCutoff;
            if (latestBuiltTable != null 
                && (latestFullSource == null || !IsOutOfDate(latestBuiltTable.AsOfDate, latestFullSource.AsOfDate)) 
                && TableMetadataSerializer.UncachedExists(context.StreamProvider, latestBuiltTable.Path))
            {
                // If the table is current, reuse it
                sources.Add(BinaryTableReader.Build(context.StreamProvider, latestBuiltTable.Path));
                incrementalNeededAfterCutoff = latestBuiltTable.AsOfDate;
            }
            else
            {
                // Otherwise, build a new table from the latest source full crawl
                sources.AddRange(context.StreamProvider.Enumerate(latestFullSource.Path, EnumerateTypes.File, true).Select((sa) => new TabularFileReader(context.StreamProvider, sa.Path)));
                incrementalNeededAfterCutoff = latestFullSource.AsOfDate;
            }

            // Add incremental crawls between the full source and the reporting date
            DateTime latestComponent = incrementalNeededAfterCutoff;

            foreach (ItemVersion incrementalCrawl in sourceVersions.VersionsInRange(CrawlType.Inc, incrementalNeededAfterCutoff, context.RequestedAsOfDateTime))
            {
                sources.AddRange(context.StreamProvider.Enumerate(incrementalCrawl.Path, EnumerateTypes.File, true).Select((sa) => new TabularFileReader(context.StreamProvider, sa.Path)));
                latestComponent = latestComponent.BiggestOf(incrementalCrawl.AsOfDate);
            }

            // Report the latest incorporated source back
            context.NewestDependency = latestComponent;

            // Return the source (if a single) or concatenated group (if multiple parts)
            return ConcatenatedTable.Build(sources);
        }

        private bool IsOutOfDate(DateTime outputWhenModifiedUtc, DateTime inputsWhenModifiedUtc)
        {
            // An output is out of date when the inputs are at least one second newer.
            // This "fuzzyness" is because input files can have partial second freshness but written tables don't.
            return inputsWhenModifiedUtc - outputWhenModifiedUtc > TimeSpan.FromSeconds(1);
        }

        public void Save(string query, string tableName)
        {
            // Ensure the table name is only a table name, not a path
            if (tableName.EndsWith(".xql", StringComparison.OrdinalIgnoreCase)) tableName = tableName.Substring(0, tableName.Length - 4);

            string fullPath = tableName;

            using (StreamWriter writer = new StreamWriter(XDatabaseContext.StreamProvider.OpenWrite($"Query\\{tableName}.xql")))
            {
                writer.Write(query);
            }

            if (this.Sources != null) this.Sources.Add(tableName);
        }

        private class LatestTableForCutoff
        {
            public ItemVersion TableVersion;
            public DateTime Cutoff;

            public LatestTableForCutoff(DateTime cutoff, ItemVersion tableVersion)
            {
                TableVersion = tableVersion;
                Cutoff = cutoff;
            }
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

        public IXTable Build(string tableName, XDatabaseContext context)
        {
            // Ask the workflow runner to defer computing dependencies now
            return _inner.Build(tableName, context, true);
        }

        public void Save(string query, string saveToPath)
        {
            _inner.Save(query, saveToPath);
        }
    }

    public static class ReportWriter
    {
        public static string Build(string tableName, XDatabaseContext context, string outputFormat)
        {
            IXTable builder = null;

            try
            {
                Stopwatch w = Stopwatch.StartNew();

                // Reset the dependency DateTime check
                context.NewestDependency = DateTime.MinValue;

                // Recursively build dependencies and return a reader for the result table
                builder = context.Runner.Build(tableName, context);

                string outputPath = null;

                if ((String.IsNullOrEmpty(outputFormat) || outputFormat.Equals("xform", StringComparison.OrdinalIgnoreCase)) && builder is BinaryTableReader)
                {
                    // If the binary format was requested and we already built it, return the path written
                    outputPath = ((BinaryTableReader)builder).TablePath;
                }
                else
                {
                    ItemVersion latestReportVersion = context.StreamProvider.ItemVersions(LocationType.Report, tableName).LatestBeforeCutoff(CrawlType.Full, context.RequestedAsOfDateTime);
                    if (latestReportVersion != null)
                    {
                        outputPath = Path.Combine(context.StreamProvider.Path(LocationType.Report, tableName, CrawlType.Full, latestReportVersion.AsOfDate), $"Report.{outputFormat}");
                    }

                    if (latestReportVersion == null || latestReportVersion.AsOfDate < context.NewestDependency || context.RebuiltSomething || !context.StreamProvider.Attributes(outputPath).Exists)
                    {
                        // If the report needs to be rebuilt, make it and return the path
                        outputPath = Path.Combine(context.StreamProvider.Path(LocationType.Report, tableName, CrawlType.Full, context.NewestDependency), $"Report.{outputFormat}");
                        if (outputFormat.Equals("xform", StringComparison.OrdinalIgnoreCase))
                        {
                            BinaryTableWriter.Build(builder, context, outputPath).RunAndDispose();
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

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
    public enum CrawlType
    {
        Full,
        Incremental
    }

    public enum LocationType
    {
        Source,
        Config,
        Table,
        Query,
        Report
    }

    public class WorkflowRunner : IWorkflowRunner
    {
        public const string DateTimeFolderFormat = "yyyy.MM.dd HH.mm.ssZ";

        private DateTime AsOfDateTime { get; set; }
        private string RootPath { get; set; }

        private string SourcePath { get; set; }
        private string ConfigPath { get; set; }
        private string TablePath { get; set; }
        private string QueryPath { get; set; }
        private string ReportPath { get; set; }

        private Dictionary<string, string> Tables { get; set; }
        private Dictionary<string, string> Queries { get; set; }

        public WorkflowRunner(string rootPath, DateTime asOfDateTime)
        {
            this.AsOfDateTime = asOfDateTime;

            this.RootPath = Path.GetFullPath(rootPath);
            this.SourcePath = Path.Combine(rootPath, "Source");
            this.ConfigPath = Path.Combine(rootPath, "Config");
            this.TablePath = Path.Combine(rootPath, "Table");
            this.QueryPath = Path.Combine(rootPath, "Query");
            this.ReportPath = Path.Combine(rootPath, "Report");

            IdentifySources();
        }

        private void IdentifySources()
        {
            Tables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Queries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Add each source with a full crawl
            if (Directory.Exists(SourcePath))
            {
                foreach (string sourceFolderPath in Directory.GetDirectories(SourcePath, "Full", SearchOption.AllDirectories))
                {
                    string pathUnderSources = Path.GetDirectoryName(sourceFolderPath).Substring(this.SourcePath.Length + 1);
                    Tables[pathUnderSources] = sourceFolderPath;
                }
            }

            // Add each known config query
            if (Directory.Exists(ConfigPath))
            {
                foreach (string configQueryPath in Directory.GetFiles(ConfigPath, "*.xql", SearchOption.AllDirectories))
                {
                    string pathUnderConfig = Path.ChangeExtension(configQueryPath, null).Substring(this.ConfigPath.Length + 1);
                    Tables[pathUnderConfig] = configQueryPath;
                }
            }

            // Add each known output query
            if (Directory.Exists(QueryPath))
            {
                foreach (string queryPath in Directory.GetFiles(QueryPath, "*.xql", SearchOption.AllDirectories))
                {
                    string pathUnderConfig = Path.ChangeExtension(queryPath, null).Substring(this.QueryPath.Length + 1);
                    Queries[pathUnderConfig] = queryPath;
                }
            }
        }

        public IEnumerable<string> SourceNames => Tables.Keys.Concat(Queries.Keys);

        public IDataBatchEnumerator Build(string tableName, WorkflowContext outerContext)
        {
            return Build(tableName, outerContext, false);
        }

        public IDataBatchEnumerator Build(string tableName, WorkflowContext outerContext, bool deferred)
        {
            // If this is a query, there won't be a cached table - just build a pipeline to make it
            string source;
            if (Queries.TryGetValue(tableName, out source)) return PipelineParser.BuildPipeline(File.ReadAllText(source), null, outerContext);

            // If this isn't a config or table, throw
            if (!Tables.TryGetValue(tableName, out source)) throw new UsageException(tableName, "tableName", SourceNames);

            // Create a context to track the newest dependency (query or input file) under this table
            WorkflowContext innerContext = new WorkflowContext(this);

            // Look for the latest output already created (if any)
            DateTime lastTableVersionBeforeCutoff = LatestVersionBeforeCutoff(LocationType.Table, tableName, AsOfDateTime, null);
            innerContext.NewestDependency = innerContext.NewestDependency.BiggestOf(lastTableVersionBeforeCutoff);

            string previousXql = "";
            if (lastTableVersionBeforeCutoff > DateTime.MinValue)
            {
                string previousTablePath = FullPath(LocationType.Table, tableName, CrawlType.Full, lastTableVersionBeforeCutoff);
                previousXql = File.ReadAllText(Path.Combine(previousTablePath, "Config.xql"));
            }

            string tablePath;
            string xql;
            IDataBatchEnumerator builder;

            if (source.EndsWith(".xql", StringComparison.OrdinalIgnoreCase))
            {
                // If there's a query to build it, record the lastWriteTime of the query
                xql = File.ReadAllText(source);

                // Compute how to build this output (recursively checking the AsOfDateTimes of referenced sources)
                builder = PipelineParser.BuildPipeline(xql, null, innerContext);
            }
            else
            {
                // If not, the query is a simple conversion. Find the latest raw source file of this name before the cutoff
                DateTime lastSourceVersionBeforeCutoff = LatestVersionBeforeCutoff(LocationType.Source, tableName, AsOfDateTime, null);
                innerContext.NewestDependency = innerContext.NewestDependency.BiggestOf(lastSourceVersionBeforeCutoff);

                // Find the input file itself
                tablePath = FullPath(LocationType.Table, tableName, CrawlType.Full, innerContext.NewestDependency);
                string sourceVersionPath = FullPath(LocationType.Source, tableName, CrawlType.Full, lastSourceVersionBeforeCutoff);
                string[] files = Directory.GetFiles(sourceVersionPath);
                if (files.Length > 1) throw new NotImplementedException("Need concatenating reader");

                // Construct a pipeline to read the raw file only
                xql = $"read \"{files[0]}\"";
                builder = PipelineParser.BuildPipeline(xql, null, null);
            }

            tablePath = FullPath(LocationType.Table, tableName, CrawlType.Full, innerContext.NewestDependency);

            // If the output isn't up-to-date, ...
            if (innerContext.NewestDependency - lastTableVersionBeforeCutoff > TimeSpan.FromSeconds(1) || xql != previousXql || innerContext.RebuiltSomething)
            {
                // If we're not running now, just return how to build it
                if (deferred) return builder;

                // Otherwise, build it now; we'll return the query to read the output
                Trace.WriteLine($"COMPUTE: [{innerContext.NewestDependency.ToString(DateTimeFolderFormat)}] {tableName}");
                new BinaryTableWriter(builder, tablePath).RunAndDispose();
                File.WriteAllText(Path.Combine(tablePath, "Config.xql"), xql);
                innerContext.RebuiltSomething = true;
            }

            // Report the newest dependency in this chain to the components above
            outerContext.NewestDependency = outerContext.NewestDependency.BiggestOf(innerContext.NewestDependency);
            outerContext.RebuiltSomething |= innerContext.RebuiltSomething;

            return new BinaryTableReader(tablePath);
        }

        public string Build(string tableName, string outputFormat)
        {
            WorkflowContext context = new WorkflowContext(this);
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
                    DateTime newestReport = LatestVersionBeforeCutoff(LocationType.Report, tableName, AsOfDateTime, null);
                    outputPath = Path.Combine(FullPath(LocationType.Report, tableName, CrawlType.Full, newestReport), $"Report.{outputFormat}");

                    if (newestReport < context.NewestDependency || context.RebuiltSomething || !File.Exists(outputPath))
                    {
                        // If the report needs to be rebuilt, make it and return the path
                        outputPath = Path.Combine(FullPath(LocationType.Report, tableName, CrawlType.Full, context.NewestDependency), $"Report.{outputFormat}");
                        new TabularFileWriter(builder, outputPath).RunAndDispose();
                        context.RebuiltSomething = true;
                    }
                    else
                    {
                        // If the last report is already up-to-date, return the existing path
                        outputPath = Path.Combine(FullPath(LocationType.Report, tableName, CrawlType.Full, newestReport), $"Report.{outputFormat}");
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

        public DateTime LatestVersionBeforeCutoff(LocationType location, string tableName, DateTime asOfDateTime, Logger logger)
        {
            // TODO: Add incremental merging

            // Find the last Full crawl which isn't after the cutoff
            DateTime latestBeforeCutoff = DateTime.MinValue;

            string sourceFolder = Path.Combine(PathForType(location), tableName, "Full");
            if (!Directory.Exists(sourceFolder)) return latestBeforeCutoff;

            foreach (string versionFolder in Directory.GetDirectories(sourceFolder))
            {
                DateTime versionAsOf;
                if (!DateTime.TryParseExact(Path.GetFileName(versionFolder), DateTimeFolderFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out versionAsOf))
                {
                    if (logger != null) logger.Write(MessageType.Warning, "WorkFlowRunner.LatestVersionBeforeCutoff", $"{versionFolder} is not a properly named DateTime version folder.");
                    continue;
                }

                if (versionAsOf > latestBeforeCutoff && versionAsOf <= asOfDateTime)
                {
                    latestBeforeCutoff = versionAsOf;
                }
            }

            if (logger != null) logger.Write(MessageType.Source, "WorkFlowRunner.LatestVersionBeforeCutoff", $"{location}.{tableName} => {latestBeforeCutoff}");
            return latestBeforeCutoff;
        }

        private string FullPath(LocationType location, string tableName, CrawlType crawl, DateTime asOfDateTime)
        {
            return Path.Combine(PathForType(location), tableName, crawl.ToString(), asOfDateTime.ToString(DateTimeFolderFormat));
        }

        private string PathForType(LocationType type)
        {
            switch (type)
            {
                case LocationType.Source:
                    return SourcePath;
                case LocationType.Table:
                    return TablePath;
                case LocationType.Config:
                    return ConfigPath;
                case LocationType.Query:
                    return QueryPath;
                case LocationType.Report:
                    return ReportPath;
                default:
                    throw new NotImplementedException(type.ToString());
            }
        }

        public DateTime GetAsOfDate(string sourceFileOrFolderPath)
        {
            if (Directory.Exists(sourceFileOrFolderPath))
            {
                return Directory.GetFiles(sourceFileOrFolderPath, "*.*", SearchOption.AllDirectories).Max((path) => File.GetLastWriteTimeUtc(path));
            }
            else
            {
                return File.GetLastWriteTimeUtc(sourceFileOrFolderPath);
            }
        }

        public int Add(string sourceFileOrFolderPath, string tableName, CrawlType type, DateTime asOfDateTime = default(DateTime))
        {
            // If the 'asOfDateTime' wasn't passed, use the File Write Time
            if (asOfDateTime == default(DateTime)) asOfDateTime = GetAsOfDate(sourceFileOrFolderPath);

            string desiredFolderPath = Path.Combine(SourcePath, tableName, type.ToString(), asOfDateTime.ToUniversalTime().ToString(DateTimeFolderFormat));

            Directory.CreateDirectory(desiredFolderPath);
            if (Directory.Exists(sourceFileOrFolderPath))
            {
                foreach (string filePath in Directory.GetFiles(sourceFileOrFolderPath, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(filePath, Path.Combine(desiredFolderPath, Path.GetFileName(filePath)));
                }
            }
            else
            {
                File.Copy(sourceFileOrFolderPath, Path.Combine(desiredFolderPath, Path.GetFileName(sourceFileOrFolderPath)));
            }

            // TODO: Delete old copies if there are too many

            Trace.WriteLine($"Done. Added \"{desiredFolderPath}\" to database.");
            return 0;
        }

        public void SaveXql(LocationType type, string tableName, string xql)
        {
            string outputPath = Path.Combine(PathForType(type), $"{tableName}.xql");
            string directory = Path.GetDirectoryName(outputPath);
            if (!String.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(outputPath, xql);
            IdentifySources();
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

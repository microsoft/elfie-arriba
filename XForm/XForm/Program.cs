// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;

using XForm.Data;
using XForm.Extensions;
using XForm.IO.StreamProvider;
using XForm.Query;

namespace XForm
{
    public class Program
    {
        public static int Main(string[] args)
        {
            WorkflowContext context = new WorkflowContext();
            context.RequestedAsOfDateTime = DateTime.UtcNow;
            context.StreamProvider = new LocalFileStreamProvider(Environment.CurrentDirectory);
            context.Runner = new WorkflowRunner(context);

            return Run(args, context);
        }

        public static int Run(string[] args, WorkflowContext context)
        {
            try
            {
                if (args == null || args.Length == 0)
                {
                    return (int)new InteractiveRunner(context).Run();
                }

                string command = args[0].ToLowerInvariant();
                switch (command)
                {
                    case "run":
                        if (args.Length < 2) throw new UsageException("'run' [QueryFilePath]");
                        return (int)RunFileQuery(args[1], context);
                    case "add":
                        if (args.Length < 3) throw new UsageException("'add' [SourceFileOrDirectory] [AsSourceName] [Full|Incremental?] [AsOfDateTimeUtc?]");

                        context.StreamProvider.Add(
                            args[1],
                            args[2],
                            ParseCrawlTypeOrDefault(args, 3, CrawlType.Full),
                            ParseDateTimeOrDefault(args, 4, DateTime.MinValue));

                        return 0;
                    case "build":
                        if (args.Length < 2) throw new UsageException("'build' [DesiredOutputName] [DesiredOutputFormat?] [AsOfDateTimeUtc?]");
                        context.RequestedAsOfDateTime = ParseDateTimeOrDefault(args, 3, context.RequestedAsOfDateTime);
                        string outputPath = ReportWriter.Build(
                            args[1],
                            context,
                            (args.Length > 2 ? args[2] : "xform"));

                        return 0;
                    case "http":
                        new HttpService(context).Run();
                        return 0;
                    case "perf":
                        new PerformanceComparisons().Run();
                        return 0;
                    default:
                        throw new UsageException($"Unknown XForm mode '{command}'.");
                }
            }
            catch (UsageException ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine(ex.Message);
                return -2;
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine($"Error: {ex.ToString()}");
                return -1;
            }
        }

        private static CrawlType ParseCrawlTypeOrDefault(string[] args, int index, CrawlType defaultValue)
        {
            if (args.Length <= index) return defaultValue;

            CrawlType type;
            if (!Enum.TryParse<CrawlType>(args[index], out type))
            {
                throw new UsageException($"Unknown SourceType '{args[index]}'; valid types: {String.Join(", ", Enum.GetValues(typeof(CrawlType)))}.");
            }

            return type;
        }

        private static DateTime ParseDateTimeOrDefault(string[] args, int index, DateTime defaultValue)
        {
            if (args.Length <= index) return defaultValue;

            DateTime result;
            if (!DateTime.TryParse(args[index], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result))
            {
                throw new UsageException($"'{args[index]} was not a valid DateTime.");
            }

            return result;
        }

        private static long RunFileQuery(string queryFilePath, WorkflowContext context)
        {
            string query = File.ReadAllText(queryFilePath);

            long rowsWritten = 0;
            using (new TraceWatch(query))
            {
                using (IDataBatchEnumerator source = XqlParser.Parse(query, null, context))
                {
                    rowsWritten = source.RunWithoutDispose();
                }
            }

            Console.WriteLine($"Done. {rowsWritten:n0} rows written.");
            return rowsWritten;
        }
    }
}

﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args == null || args.Length == 0)
                {
                    InteractiveRunner runner = new InteractiveRunner();
                    return runner.Run();
                }

                string command = args[0].ToLowerInvariant();
                switch (command)
                {
                    case "run":
                        if (args.Length < 2) throw new UsageException("'run' [QueryFilePath]");
                        return RunFileQuery(args[1]);
                    case "add":
                        if (args.Length < 3) throw new UsageException("'add' [SourceFileOrDirectory] [AsSourceName] [Full|Incremental?] [AsOfDateTimeUtc?]");

                        return new WorkflowRunner(Environment.CurrentDirectory, ParseDateTimeOrDefault(args, 4, DateTime.MinValue)).Add(
                            args[1],
                            args[2],
                            ParseCrawlTypeOrDefault(args, 3, CrawlType.Full));
                    case "build":
                        if (args.Length < 2) throw new UsageException("'build' [DesiredOutputName] [DesiredOutputFormat?] [AsOfDateTimeUtc?]");

                        string outputPath = new WorkflowRunner(Environment.CurrentDirectory, ParseDateTimeOrDefault(args, 3, DateTime.UtcNow)).Build(
                            args[1],
                            (args.Length > 2 ? args[2] : "xform"));

                        return 0;
                    default:
                        throw new UsageException($"Unknown XForm mode '{command}'.");
                }
            }
            catch (UsageException ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine($"Usage: {ex.Message}");
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

        private static int RunFileQuery(string queryFilePath)
        {
            string query = File.ReadAllText(queryFilePath);

            int rowsWritten = 0;
            using (new TraceWatch(query))
            {
                using (IDataBatchEnumerator source = PipelineParser.BuildPipeline(query))
                {
                    rowsWritten = source.Run();
                }
            }

            Console.WriteLine($"Done. {rowsWritten:n0} rows written.");
            return rowsWritten;
        }
    }
}
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
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
                if (args.Length > 0)
                {
                    return RunFileQuery(args[0]);
                }
                else
                {
                    InteractiveRunner runner = new InteractiveRunner();
                    return runner.Run();
                }
            }
            catch (UsageException ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine($"Usage: {ex.ToString()}");
                return -2;
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine($"Error: {ex.ToString()}");
                return -1;
            }
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

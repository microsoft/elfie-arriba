// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using CommandLine;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.PDB;

namespace Microsoft.CodeAnalysis.Elfie.Indexer
{
    public enum IndexerExitCode
    {
        Success = 0,
        ArgumentsInvalid = -1,
        UnhandledException = -2,
        PathToMergeNotFound = -3
    }

    public class Program
    {
        private static int Main(string[] args)
        {
            Logger logger = new Logger();

            try
            {
                return Parser.Default.ParseArguments<
                    IndexOptions,
                    DumpOptions>(args)
                  .MapResult(
                    (IndexOptions indexOptions) => new IndexCommand().Run(indexOptions),
                    (DumpOptions dumpOptions) => new DumpCommand().Run(dumpOptions),
                    errs => 1);
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                logger.LogException(ex);
                return (int)IndexerExitCode.UnhandledException;
            }
        }
    }
}

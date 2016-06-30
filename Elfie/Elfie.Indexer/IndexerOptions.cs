// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using CommandLine;

using Microsoft.CodeAnalysis.Elfie.Model;

namespace Microsoft.CodeAnalysis.Elfie.Indexer
{
    [Verb("index", HelpText = "Index one or more solutions, projects, binaries, folders, or text file lists.")]
    public class IndexOptions
    {
        public const string Usage = @"
ex: Elfie.Indexer -p ""C:\Code\Elfie-Private"" -o ..\Index
ex: Elfie.Indexer -p ""C:\Code\ArribaPaths.txt"" -o ..\Index --dl 19000 --pn Arriba --rn 1.0.0.stable --url http://github.com/Arriba --ift --full
ex: Elfie.Indexer -p C:\Code -o ..\Index --full";

        [Option('p', Required = true, HelpText = "Path to Solution, Project, Binary, Folder Path containing 'Just My Code' binaries, or Text File list of binaries to index.")]
        public string PathToIndex { get; set; }

        [Option('o', HelpText = "Output Directory within which to write index file or folder.")]
        public string OutputPath { get; set; }

        [Option("ln", HelpText = "Alternate Use/Error log folder name. Logs are in %LocalAppData%\\LogName.")]
        public string LogName { get; set; }

        [Option("full", HelpText = "Full to index non-public members and other details.")]
        public bool IsFull { get; set; }

        [Option("diag", HelpText = "Write Diagnostics with index [to debug index internals].")]
        public bool WriteDiagnostics { get; set; }

        [Option("dl", HelpText = "Download Count of item to index.")]
        public int DownloadCount { get; set; }

        [Option("pn", HelpText = "Package Name of NuGet Package being indexed.")]
        public string PackageName { get; set; }

        [Option("rn", HelpText = "Release Name of NuGet Package being indexed.")]
        public string ReleaseName { get; set; }

        [Option("url", HelpText = "Project URL of NuGet Package being indexed.")]
        public string ProjectUrl { get; set; }

        [Option('c', HelpText = "The local path to cache symbols and sources to. App.Config setting is used if this is not set.")]
        public string SymbolCachePath { get; set; }

        [Option("id", HelpText = "Whether to look for and include indices built for dependencies in the SymbolCache.")]
        public bool IncludeSymbolCacheIndices { get; set; }

        [Option("ift", HelpText = "Whether to include the Framework Targets for binaries in a NuGet Package folder structure.")]
        public bool IncludeFrameworkTargets { get; set; }

        // Previous Version of Database to use for incremental crawling.
        public IMemberDatabase PreviousDatabase { get; set; }

        // Previous Version write time to use for incremental crawling.
        public DateTime PreviousDatabaseWriteTimeUtc { get; set; }

        public IndexOptions()
        { }
    }
}

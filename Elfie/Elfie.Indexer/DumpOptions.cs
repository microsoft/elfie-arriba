// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.CodeAnalysis.Elfie.Indexer
{
    [Verb("dump", HelpText = "Dump IDX file to console.")]
    public class DumpOptions
    {
        [Value(0,
               HelpText = "A path to an ELFIE IDX file to dump to the console.")]
        public string IndexFilePath { get; internal set; }

        public DumpOptions()
        { }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using CommandLine;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;

namespace SourceIndex
{
    public class SourceIndexOptions
    {
        public const string Usage = @"SourceIndex is used to update a PDB to point to source files via a URL [ex: GitHub URL].
ex: SourceIndex -p Newtonsoft.Json.pdb -u https://raw.githubusercontent.com/JamesNK/Newtonsoft.Json/8.0.2 -r C:\Development\Releases\Json\Working\Newtonsoft.Json\Working-Signed";

        [Option('p', Required = true, HelpText = "Path to PDB to Source Index")]
        public string PdbPath { get; set; }

        [Option('u', Required = true, HelpText = "URL to map source files to [ex: https://raw.githubusercontent.com/<User>/<Repo>/<TagOrCommitHash>]")]
        public string TargetUrl { get; set; }

        [Option('r', Required = true, HelpText = @"Original Build Path corresponding to TargetURL [ex: C:\Code\<repo>]")]
        public string BuildPathRoot { get; set; }
    }

    public enum SourceIndexExitCode
    {
        Success = 0,
        ArgumentsInvalid = -1,
        UnhandledException = -2,
        PdbNotFound = -3,
        SourcePathDidNotStartWithRoot = -4,
    }

    public class Program
    {
        private const string SrcToolFormatString = @"DebuggingTools\srctool.exe -r ""{0}""";
        private const string PdbStrFormatString = @"DebuggingTools\pdbstr.exe -w -p:""{0}"" -s:srcsrv -i:""{0}.srcsrv""";

        private const string SrcSrvMapping =
@"SRCSRV: ini ------------------------------------------------
VERSION=2
INDEXVERSION=2
VERCTRL=http
SRCSRV: variables ------------------------------------------
SRCSRVTRG={0}/%var2%
SRCSRVCMD=
SRCSRVVERCTRL=http
SRCSRV: source files ---------------------------------------";

        private const string SrcSrvSuffix = "SRCSRV: end ------------------------------------------------";

        private static int Main(string[] args)
        {
            try
            {
                var result = Parser.Default.ParseArguments<SourceIndexOptions>(args);

                return (int)result.MapResult(
                    (options) => Run(options),
                    (error) =>
                    {
                        Console.WriteLine(SourceIndexOptions.Usage);
                        return SourceIndexExitCode.ArgumentsInvalid;
                    }
                );
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine("Error: " + ex.Message);
                return (int)SourceIndexExitCode.UnhandledException;
            }
        }

        public static SourceIndexExitCode Run(SourceIndexOptions arguments)
        {
            if (!File.Exists(arguments.PdbPath))
            {
                Console.WriteLine("Error: Unable to find \"{0}\". Stopping.", arguments.PdbPath);
                return SourceIndexExitCode.PdbNotFound;
            }

            // Extract the file list from the PDB
            Cmd.Quiet(String.Format(SrcToolFormatString, arguments.PdbPath), TimeSpan.FromSeconds(10), arguments.PdbPath + ".txt");

            // Write the new SRCSRV mapping
            int unmappedLines = 0;
            string[] lines = File.ReadAllLines(arguments.PdbPath + ".txt");
            using (StreamWriter writer = new StreamWriter(arguments.PdbPath + ".srcsrv"))
            {
                writer.WriteLine(String.Format(SrcSrvMapping, arguments.TargetUrl.TrimEnd('/')));

                for (int i = 0; i < lines.Length - 1; ++i)
                {
                    string originalPath = lines[i];
                    if (!originalPath.StartsWith(arguments.BuildPathRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        unmappedLines++;
                        Console.WriteLine("Warning: Source File in PDB didn't start with expected BuildPathRoot.\r\nPath: {0}\r\nRoot: {1}", originalPath, arguments.BuildPathRoot);
                    }

                    string pathUnderRoot = lines[i].Replace(arguments.BuildPathRoot, "").TrimStart('\\').Replace('\\', '/');

                    writer.Write(originalPath);
                    writer.Write("*");
                    writer.Write(pathUnderRoot);
                    writer.WriteLine();
                }

                writer.WriteLine(SrcSrvSuffix);
            }

            if (unmappedLines == lines.Length - 1)
            {
                Console.WriteLine("Error: No source files in PDB had the right BuildPathRoot to remap. Aborting.");
                return SourceIndexExitCode.SourcePathDidNotStartWithRoot;
            }

            // Import the new SRCSRV mapping into the PDB
            Cmd.Quiet(String.Format(PdbStrFormatString, arguments.PdbPath), TimeSpan.FromSeconds(10));

            // TODO: With dependency free code to read DLL signature, we could write the corrected PDB into the local SymbolCache

            Console.WriteLine("Done. Source Paths in {0} remapped to {1}.", arguments.PdbPath, arguments.TargetUrl);
            return SourceIndexExitCode.Success;
        }
    }
}

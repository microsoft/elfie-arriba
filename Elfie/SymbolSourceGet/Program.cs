// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using CommandLine;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Indexer;
using Microsoft.CodeAnalysis.Elfie.PDB;

namespace SymbolSourceGet
{
    public class Options
    {
        public const string Usage = @"
ex: SymbolSourceGet -b Elfie.dll
SymbolSourceGet tries to find and download debugging symbols [PDB] and source 
code from configured symbol server paths. Set the symbol server paths in
app.config. Symbols and sources are written in the local symbol cache in the
normal folder structure used by debuggers. Source files are cached in a 'src'
folder next to the downloaded symbols.";

        public Options()
        {
            this.SymbolServerUrls = ConfigurationManager.AppSettings["SymbolServerUrls"];
        }

        [Option('b', Required = true, HelpText = "Path to binary to find symbols and sources for.")]
        public string BinaryPath { get; set; }

        [Option('f', HelpText = "Force re-retrieve everything, even if a previous attempt was made.")]
        public bool Force { get; set; }

        [Option("so", HelpText = "True to get symbols only [no sources].")]
        public bool SymbolsOnly { get; set; }

        [Option('c', HelpText = "The local path to cache symbols and sources to. App.Config setting is used if this is not set.")]
        public string SymbolCachePath { get; set; }

        [Option('u', HelpText = "Symbol Server URLs to query, pipe delimited. App.Config paths used if this is not set.")]
        public string SymbolServerUrls { get; set; }
    }

    public class ExitCodes
    {
        public const int Success = 0;
        public const int ArgumentsInvalid = -1;
        public const int UnhandledException = -2;
        public const int BinaryNotFound = -3;
        public const int BinarySignatureNotFound = -4;
        public const int SymbolsNotFoundAtAnySymbolServer = -5;
        public const int SourceUrlsNotFoundInPdb = -6;
        public const int FailedDownloadingSomeSourceFiles = -7;
    }

    public class Program
    {
        private const string SymChkExe = @"DebuggingTools\symchk.exe";
        private const string SymChkFormatString = " \"{0}\" /s \"SRV*{1}*{2}\"";

        private static int Main(string[] args)
        {
            try
            {
                var result = Parser.Default.ParseArguments<Options>(args);

                return result.MapResult(
                    (arguments) => Run(arguments),
                    (errors) => ExitCodes.ArgumentsInvalid
                );
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return (int)ExitCodes.UnhandledException;
            }
        }

        private static int Run(Options arguments)
        {
            if (!String.IsNullOrEmpty(arguments.SymbolCachePath)) SymbolCache.Path = arguments.SymbolCachePath;

            if (!File.Exists(arguments.BinaryPath))
            {
                Console.WriteLine("Error: Unable to find \"{0}\". Stopping.", arguments.BinaryPath);
                return ExitCodes.BinaryNotFound;
            }

            // Get Hash of Binary
            RsDsSignature signature = Assembly.ReadRsDsSignature(arguments.BinaryPath);
            if (signature == null)
            {
                Console.WriteLine("Unable to read binary signature. It may not be a managed binary.");
                return ExitCodes.BinarySignatureNotFound;
            }

            Console.WriteLine("Signature: {0}", signature);

            // File Structure Map:
            //  C:\SymbolCache\Elfie.pdb									=> All known Elfie.pdb copies
            //			\671497fb11a04a8f9fc2c64bd82b4a201\Elfie.pdb		=> All content related to Elfie.dll with debug signature 671...201
            //				Elfie.pdb										=> Elfie.pdb matching Elfie.dll with debug signature 671...201
            //				FilePaths.map									=> Tab delimited file with source file path in PDB, download URL, cached path under \SymbolCache\Elfie.pdb
            //			\src\fc2c64bd\Program.cs							=> Source Code 'Program.cs' with remote URL hash fc2c64bd
            string pdbFileName = Path.ChangeExtension(Path.GetFileName(arguments.BinaryPath), ".pdb");
            string pdbNameDirectory = Path.Combine(Environment.ExpandEnvironmentVariables(SymbolCache.Path), pdbFileName);
            string pdbCacheDirectory = Path.Combine(pdbNameDirectory, signature.ToString());
            string pdbPath = Path.Combine(pdbCacheDirectory, pdbFileName);
            string sourceCacheDirectory = Path.Combine(pdbNameDirectory, "src");

            if (File.Exists(pdbPath))
            {
                Console.WriteLine("PDB: {0}", pdbPath);
            }
            else if (File.Exists(Path.ChangeExtension(arguments.BinaryPath, ".pdb")))
            {
                pdbPath = Path.GetFullPath(Path.ChangeExtension(arguments.BinaryPath, ".pdb"));
                Console.WriteLine("PDB: {0}", pdbPath);
            }
            else if (Directory.Exists(pdbCacheDirectory) && !arguments.Force)
            {
                Console.WriteLine("PDB download previously failed. Stopping. Use '-f' to force retry.");
                return ExitCodes.SymbolsNotFoundAtAnySymbolServer;
            }
            else
            {
                // Get symbols, if we haven't previously tried
                DownloadSymbols(arguments, pdbPath);

                if (!File.Exists(pdbPath))
                {
                    // TODO: Ping the symbol servers; don't write the "don't try again" if we couldn't reach the server at all

                    // Create a stub indicating we tried but could not download the PDB
                    if (!String.IsNullOrEmpty(pdbCacheDirectory)) Directory.CreateDirectory(pdbCacheDirectory);

                    Console.WriteLine("No configured symbol server had symbols. Stopping. Use '-f' to force retry.");
                    return ExitCodes.SymbolsNotFoundAtAnySymbolServer;
                }
            }

            // Extract or reload the PDB path to source URL map
            SourceFileMap map = SourceFileMap.BuildOrLoad(pdbPath);

            // Write map next to binary as well as PDB [so either can be indexed safely]
            map.Save(Path.ChangeExtension(arguments.BinaryPath, ".map"));

            if (map.Files.Count == 0)
            {
                Console.WriteLine("No Source File URLs found in PDB. It may not be source server registered.");
                return ExitCodes.SourceUrlsNotFoundInPdb;
            }
            else
            {
                Console.WriteLine("PDB references {0:n0} source file URLs.", map.Files.Count);
            }

            // Download source files not already downloaded (or failed)
            bool allFilesDownloaded = DownloadSourceFiles(map, sourceCacheDirectory, arguments.Force).Result;

            return allFilesDownloaded ? ExitCodes.Success : ExitCodes.FailedDownloadingSomeSourceFiles;
        }

        private static void DownloadSymbols(Options arguments, string pdbPath)
        {
            string[] symbolServerUrls = arguments.SymbolServerUrls.Split('|');
            foreach (string symbolServerUrl in symbolServerUrls)
            {
                Console.WriteLine("Downloading Symbols from {0}...", symbolServerUrl);

                string symchkArguments = String.Format(SymChkFormatString, arguments.BinaryPath, arguments.SymbolCachePath, symbolServerUrl);
                Cmd.Quiet(SymChkExe + " " + symchkArguments, TimeSpan.FromSeconds(30));

                if (File.Exists(pdbPath))
                {
                    Console.WriteLine("Found.");
                    break;
                }
            }
        }

        private static async Task<bool> DownloadSourceFiles(SourceFileMap map, string pdbCacheDirectory, bool force)
        {
            int countDownloaded = 0;
            int countPreviouslyDownloaded = 0;
            int countFailed = 0;
            int countPreviouslyFailed = 0;

            foreach (SourceFileDetails sfd in map.Files)
            {
                string fileCachePath = Path.Combine(pdbCacheDirectory, sfd.CachedPathSuffix);
                string fileCacheFolderPath = Path.GetDirectoryName(fileCachePath);

                // If the folder where the file would be downloaded exists, stop (that's the marker we tried already)
                if (!force)
                {
                    if (File.Exists(fileCachePath))
                    {
                        countPreviouslyDownloaded++;
                        continue;
                    }

                    if (Directory.Exists(fileCacheFolderPath))
                    {
                        countPreviouslyFailed++;
                        continue;
                    }
                }

                // Try to download the file
                Console.WriteLine("Downloading {0}...", sfd.SourceUrl);

                try
                {
                    await SourceFileMap.Download(map.PdbPath, sfd.SourceUrl, TimeSpan.FromSeconds(5));
                    countDownloaded++;
                }
                catch (WebException ex)
                {
                    countFailed++;

                    // On download failure, create a marker so we know we tried previously
                    Console.WriteLine("Download failed. Writing do not retry marker. Error: {0}", ex.Message);
                    if (!String.IsNullOrEmpty(fileCacheFolderPath)) Directory.CreateDirectory(fileCacheFolderPath);
                }
            }

            // Log download result
            if (map.Files.Count > 0)
            {
                Console.WriteLine("Source Download completed. {1:n0} newly downloaded, {2:n0} previously downloaded, {3:n0} failed, {4:n0} previously failed.", map.Files.Count, countDownloaded, countPreviouslyDownloaded, countFailed, countPreviouslyFailed);
            }

            return countFailed == 0;
        }
    }
}

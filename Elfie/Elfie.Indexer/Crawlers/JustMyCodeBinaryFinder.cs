// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers
{
    /// <summary>
    ///  JustMyCodeFinder finds user code by looking for all PDBs which reference a first source file
    ///  which is found on the local machine. This is a fast way to identify code the user built for
    ///  binary-based indexing.
    /// </summary>
    public class JustMyCodeBinaryFinder
    {
        /// <summary>
        ///  Return 'Just My Code' assemblies under rootPath. This is:
        ///   - Assemblies with a PDB
        ///   - PDB's first source code file is found locally
        ///   - Most recently written assembly per unique local code file
        ///   - Returned in most recently written first order
        /// </summary>
        /// <param name="rootPath">Path to crawl</param>
        /// <returns>List of assembly paths which are 'Just My Code'</returns>
        public static List<string> FindJustMyCodeUnder(string rootPath)
        {
            Dictionary<string, JustMyCodeEntry> newestBinaryForSourceFile = new Dictionary<string, JustMyCodeEntry>(StringComparer.OrdinalIgnoreCase);

            // Look for all PDBs under the root path
            foreach (string pdbPath in Directory.EnumerateFiles(rootPath, "*.pdb", SearchOption.AllDirectories))
            {
                // If we can't find the binary, stop
                string binaryPath = GetFoundBinaryPath(pdbPath);
                if (binaryPath == null) continue;

                // If we can't load it or find the first source file, stop
                string firstSourceFilePath = GetFirstSourceFileInPDB(binaryPath);
                if (firstSourceFilePath == null) continue;

                // Record the most recently written PDB for each unique first source file
                if (File.Exists(firstSourceFilePath))
                {
                    DateTime pdbLastWriteUtc = File.GetLastWriteTimeUtc(pdbPath);

                    JustMyCodeEntry otherBinaryPath;
                    if (!newestBinaryForSourceFile.TryGetValue(firstSourceFilePath, out otherBinaryPath) || otherBinaryPath.PdbLastWriteTimeUtc < pdbLastWriteUtc)
                    {
                        newestBinaryForSourceFile[firstSourceFilePath] = new JustMyCodeEntry() { BinaryPath = binaryPath, PdbPath = pdbPath, PdbFirstSourceFilePath = firstSourceFilePath, PdbLastWriteTimeUtc = pdbLastWriteUtc };
                    }
                }
            }

            // Return binary paths found in most-recently-written-first order
            return new List<string>(newestBinaryForSourceFile.Values.OrderByDescending((e) => e.PdbLastWriteTimeUtc).Select((e) => e.BinaryPath));
        }

        /// <summary>
        ///  Returns whether any 'Just My Code' assemblies are found under the given path
        ///  built after a certain DateTime. This is used to determine if a recrawl of
        ///  'Just My Code' is needed.
        /// </summary>
        /// <param name="rootPath">Path to crawl</param>
        /// <param name="cutoffUtc">Cutoff; return true only if newer 'Just My Code' PDBs are found</param>
        /// <returns></returns>
        public static bool AreJustMyCodeBinariesNewerThan(string rootPath, DateTime cutoffUtc)
        {
            foreach (string pdbPath in Directory.EnumerateFiles(rootPath, "*.pdb", SearchOption.AllDirectories))
            {
                DateTime pdbLastWriteUtc = File.GetLastWriteTimeUtc(pdbPath);
                if (cutoffUtc > pdbLastWriteUtc) continue;

                string binaryPath = GetFoundBinaryPath(pdbPath);
                if (binaryPath == null) continue;

                string firstSourceFilePath = GetFirstSourceFileInPDB(binaryPath);
                if (firstSourceFilePath != null && File.Exists(firstSourceFilePath)) return true;
            }

            return false;
        }

        private static string GetFoundBinaryPath(string pdbPath)
        {
            string binaryPath = Path.ChangeExtension(pdbPath, ".dll");
            if (File.Exists(binaryPath)) return binaryPath;

            binaryPath = Path.ChangeExtension(pdbPath, ".exe");
            if (File.Exists(binaryPath)) return binaryPath;

            binaryPath = Path.ChangeExtension(pdbPath, ".winmd");
            if (File.Exists(binaryPath)) return binaryPath;

            return null;
        }

        private static string GetFirstSourceFileInPDB(string binaryPath)
        {
            try
            {
                using (PdbSymbolProvider provider = PdbSymbolProvider.TryBuildProvider(binaryPath))
                {
                    if (provider != null)
                    {
                        string firstDocumentUrl = provider.FirstDocumentUrl();

                        if (firstDocumentUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            firstDocumentUrl = provider.CacheLocation(firstDocumentUrl);
                        }

                        return firstDocumentUrl;
                    }
                }
            }
            catch (ArgumentException)
            {
                // ... Couldn't load PDB - different platform target?
            }

            return null;
        }
    }

    public class JustMyCodeEntry
    {
        public string PdbPath;
        public DateTime PdbLastWriteTimeUtc;
        public string PdbFirstSourceFilePath;
        public string BinaryPath;
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers
{
    public class IncrementalCrawler
    {
        private ICrawler InnerCrawler { get; set; }
        private IMemberDatabase Previous { get; set; }
        private DateTime PreviousWriteUtc { get; set; }
        public bool IncludeSymbolCacheIndices { get; set; }

        public IncrementalCrawler(ICrawler innerCrawler)
        {
            this.InnerCrawler = innerCrawler;
        }

        public IncrementalCrawler(ICrawler innerCrawler, IMemberDatabase previousDatabase, DateTime previousWriteUtc)
        {
            this.InnerCrawler = innerCrawler;
            this.Previous = previousDatabase;
            this.PreviousWriteUtc = previousWriteUtc;
        }

        public PackageDatabase Walk(string walkPath, PackageIdentity identity = null)
        {
            // Normalize WalkPath to ensure we can get a name for it
            walkPath = Path.GetFullPath(walkPath);

            if (identity == null) identity = new PackageIdentity(Path.GetFileName(walkPath));
            if (String.IsNullOrEmpty(identity.IndexFileName)) throw new ArgumentException(String.Format("ERROR: Unable to compute database name for path \"{0}\"", walkPath));

            PackageDatabase db = new PackageDatabase(identity);
            MutableSymbol packageRoot = db.MutableRoot.AddChild(new MutableSymbol(identity.PackageName, SymbolType.Package));

            // Index the directory|file list|solution|project|binary
            string extension = Path.GetExtension(walkPath).ToLowerInvariant();
            if (Directory.Exists(walkPath))
            {
                if (this.IncludeSymbolCacheIndices)
                {
                    WalkEverythingAndSymbolCache(walkPath, packageRoot);
                }
                else
                {
                    WalkJustMyCode(walkPath, packageRoot);
                }
            }
            else if (extension.Equals(".txt"))
            {
                foreach (string itemPath in File.ReadAllLines(walkPath))
                {
                    WalkIncremental(itemPath, packageRoot);
                }
            }
            else
            {
                InnerCrawler.Walk(walkPath, packageRoot);
            }

            return db;
        }

        private void WalkIncremental(string filePath, MutableSymbol parent)
        {
            // Copy the old database for this binary if it is older than the old index and we find it
            if (this.Previous != null)
            {
                DateTime binaryModifiedUtc = File.GetLastWriteTimeUtc(filePath);
                if (binaryModifiedUtc <= this.PreviousWriteUtc)
                {
                    Symbol oldAssembly;
                    if (TryFindAssembly(Path.GetFileName(filePath), out oldAssembly))
                    {
                        parent.AddTree(oldAssembly);
                        return;
                    }
                    else if (TryFindAssembly(Path.GetFileNameWithoutExtension(filePath), out oldAssembly))
                    {
                        parent.AddTree(oldAssembly);
                        return;
                    }
                }
            }

            // Otherwise, recrawl
            InnerCrawler.Walk(filePath, parent);
        }

        private bool TryFindAssembly(string name, out Symbol assembly)
        {
            assembly = default(Symbol);

            String8 name8 = String8.Convert(name, new byte[String8.GetLength(name)]);

            Range nameRange;
            if (!this.Previous.StringStore.TryFindString(name8, out nameRange)) return false;

            int matchIndex;
            if (!this.Previous.DeclaredMembers.TryFindChildByName(1, nameRange, out matchIndex)) return false;

            assembly = new Symbol(this.Previous, matchIndex);
            return true;
        }

        private void WalkJustMyCode(string rootPath, MutableSymbol parent)
        {
            List<string> justMyCodeBinaries = new List<string>();

            // Find each PDB for which we can find the binary and find the first referenced source file
            using (new TraceWatch("Finding 'Just My Code' binaries..."))
            {
                justMyCodeBinaries = JustMyCodeBinaryFinder.FindJustMyCodeUnder(rootPath);
            }

            // Index each binary
            ProgressWriter p = new ProgressWriter(justMyCodeBinaries.Count);
            using (new TraceWatch("Indexing {0:n0} binaries...", justMyCodeBinaries.Count))
            {
                foreach (string binaryPath in justMyCodeBinaries)
                {
                    WalkIncremental(binaryPath, parent);
                    p.IncrementProgress();
                }
            }
        }

        private void WalkEverythingAndSymbolCache(string rootPath, MutableSymbol parent)
        {
            List<string> binariesToIndex = new List<string>();
            List<string> indicesToMerge = new List<string>();

            using (new TraceWatch("Finding Binaries to Index..."))
            {
                foreach (string filePath in Directory.GetFiles(rootPath))
                {
                    // Skip VsHost.exe binary
                    if (filePath.Contains(".vshost.")) continue;

                    if (FileIO.IsManagedBinary(filePath))
                    {
                        // If the binary has symbols next to it, index it
                        string pdbPath = Path.ChangeExtension(filePath, ".pdb");
                        if (File.Exists(pdbPath))
                        {
                            binariesToIndex.Add(filePath);
                        }

                        // If the binary has symbols and an IDX in the SymbolCache, merge it
                        string pdbPathInSymbolCache = Assembly.GetSymbolCachePdbPath(filePath);
                        if (!String.IsNullOrEmpty(pdbPathInSymbolCache) && File.Exists(pdbPathInSymbolCache))
                        {
                            string indexInSymbolCache = Path.ChangeExtension(pdbPathInSymbolCache, ".dll.idx");

                            if (File.Exists(indexInSymbolCache))
                            {
                                indicesToMerge.Add(indexInSymbolCache);
                            }
                        }
                    }
                }
            }

            // Index each binary
            ProgressWriter p = new ProgressWriter(binariesToIndex.Count);
            using (new TraceWatch("Indexing {0:n0} binaries...", binariesToIndex.Count))
            {
                foreach (string binaryPath in binariesToIndex)
                {
                    WalkIncremental(binaryPath, parent);
                    p.IncrementProgress();
                }
            }

            p = new ProgressWriter(indicesToMerge.Count);
            using (new TraceWatch("Merging {0:n0} dependency indices...", indicesToMerge.Count))
            {
                foreach (string indexPath in indicesToMerge)
                {
                    PackageDatabase dependency = new PackageDatabase();
                    dependency.FileRead(indexPath);
                    parent.AddTree(dependency.QueryRoot.FirstChild());
                    p.IncrementProgress();
                }
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.PDB;

namespace Microsoft.CodeAnalysis.Elfie.Indexer
{
    public class IndexCommand
    {
        public IndexCommand()
        { }

        public int Run(IndexOptions arguments)
        {
            if (!String.IsNullOrEmpty(arguments.SymbolCachePath)) SymbolCache.Path = arguments.SymbolCachePath;

            if (!File.Exists(arguments.PathToIndex) && !Directory.Exists(arguments.PathToIndex))
            {
                Console.WriteLine("Unable to find PathToIndex \"{0}\". Stopping.", arguments.PathToIndex);
                return (int)IndexerExitCode.PathToMergeNotFound;
            }

            // Index the database
            PackageDatabase database = Index(arguments);

            // Save the database
            if (String.IsNullOrEmpty(arguments.OutputPath)) arguments.OutputPath = Path.GetDirectoryName(arguments.PathToIndex);
            string savedToPath = Save(database, arguments.OutputPath);
            if (arguments.WriteDiagnostics) SaveDiagnostics(database, arguments.OutputPath);

            // Format Verification: Test reload to ensure no exceptions at least
            if (!String.IsNullOrEmpty(savedToPath))
            {
                PackageDatabase loaded = new PackageDatabase();
                loaded.FileRead(savedToPath);
            }

            return (int)IndexerExitCode.Success;
        }


        public static PackageDatabase Index(string pathToIndex, bool isFull = false, bool includeFrameworkTargets = false)
        {
            return Index(new IndexOptions() { PathToIndex = pathToIndex, IsFull = isFull, IncludeFrameworkTargets = includeFrameworkTargets });
        }

        public static PackageDatabase Index(IndexOptions arguments)
        {
            PackageIdentity identity = new PackageIdentity(arguments.PackageName ?? Path.GetFileName(arguments.PathToIndex)) { ReleaseName = arguments.ReleaseName, DownloadCount = arguments.DownloadCount, ProjectUrl = arguments.ProjectUrl };

            PackageDatabase db;
            using (new TraceWatch("{0} Indexing '{1}'...", (arguments.IsFull ? "Full" : ""), arguments.PathToIndex))
            {
                // TODO: Make this configurable until SrmCrawler is all we want
                //SrmCrawler crawler = new SrmCrawler();
                RoslynCompilationCrawler crawler = new RoslynCompilationCrawler();

                if (arguments.IsFull == false)
                {
                    crawler.IncludeNonPublicMembers = false;
                    //crawler.IncludeCodeLocations = false;
                    //crawler.IncludeSignatures = false;
                    //crawler.IncludeMembers = false;
                }

                crawler.IncludeFrameworkTargets = arguments.IncludeFrameworkTargets;

                IncrementalCrawler ic = new IncrementalCrawler(crawler, arguments.PreviousDatabase, arguments.PreviousDatabaseWriteTimeUtc);
                ic.IncludeSymbolCacheIndices = arguments.IncludeSymbolCacheIndices;
                db = ic.Walk(arguments.PathToIndex, identity);
            }

            int locatedMemberCount = db.LocatedMemberCount;
            int totalMemberCount = db.MemberCount;

            Trace.WriteLine(String.Format(
                "Done. Database is {0:n0} types, {1:n0} members, {2:p0} located [{3}]\r\n\tsymbols: {4},\r\n\tfiles: {5},\r\n\tstrings: {6},\r\n\tindex: {7}.\r\n",
                db.TypeCount,
                db.MemberCount,
                locatedMemberCount / (double)totalMemberCount,
                db.Bytes.SizeString(),
                db.SymbolStatistics.ToStatisticsString(),
                db.FileStatistics.ToStatisticsString(),
                db.StringStatistics.ToStatisticsString(),
                db.IndexStatistics.ToStatisticsString()
            ));

            if (locatedMemberCount < totalMemberCount)
            {
                using (new TraceWatch("Improving locations with syntax parse..."))
                {
                    RoslynSyntaxLocationExpander expander = new RoslynSyntaxLocationExpander();
                    expander.AddLocations(db);
                }

                Trace.WriteLine(String.Format("Done. {0:p0} located.\r\n", db.LocatedMemberCount / (double)totalMemberCount));
            }

            return db;
        }

        public static string Save(PackageDatabase db, string outputPath)
        {
            if (!String.IsNullOrEmpty(outputPath) && !Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

            // Write binary format
            string binaryModelPath = Path.Combine(outputPath, db.Identity.IndexFileName);

            // Don't write empty databases
            if (db.IsEmpty)
            {
                Trace.WriteLine(String.Format("Index '{0}' is empty. Not writing.", binaryModelPath));
                return String.Empty;
            }

            using (new TraceWatch("Writing Index to '{0}'...", binaryModelPath))
            {
                db.FileWrite(binaryModelPath);
                return binaryModelPath;
            }
        }

        public static void SaveDiagnostics(PackageDatabase database, string outputPath)
        {
            if (database.IsEmpty) return;

            // Write text model
            string binaryModelPath = Path.Combine(outputPath, database.Identity.IndexFileName);
            using (new TraceWatch("Writing Text Model to '{0}'...", binaryModelPath + ".log"))
            {
                using (StreamWriter writer = new StreamWriter(binaryModelPath + ".log", false))
                {
                    database.QueryRoot.WriteTree(writer, 0);
                }
            }

            // Write string content only
            using (new TraceWatch("Writing Strings to '{0}'...", binaryModelPath + ".str"))
            {
                database.WriteStringsOnly(binaryModelPath + ".str");
            }
        }
    }
}
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.EndToEnd
{
    internal class Program
    {
        public const string IndexPath = @"..\Index";

        private static void Main(string[] args)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                if (!new IndexAndSearchReferenceAssemblies().Run(writer))
                {
                    Console.WriteLine(sb.ToString());
                }
                else
                {
                    Console.WriteLine("No bad test results seen!");
                }
            }

            //foreach (string targetFrameworkPath in Directory.GetDirectories(@"C:\Download\Newtonsoft.Json Flavors\8.0.2\lib"))
            //{
            //    string targetFramework = Path.GetFileName(targetFrameworkPath);
            //    string binaryPath = Path.Combine(targetFrameworkPath, "Newtonsoft.Json.dll");

            //    PackageDatabase db = Indexer.IndexCommand.Index(binaryPath, true);
            //    AddReferenceDatabase ardb = new AddReferenceDatabase();
            //    ardb.AddReferenceAssemblyTypes(db);
            //    Write.ToFile(ardb.WriteText, binaryPath + ".ardb.txt");
            //}

            ////WriteArdbVariations(@"C:\Download\ARDBs\FormatVersion\20160207");

            ////return;

            //string target = @"C:\Code\Arriba";
            ////string target = @"C:\Code\elfie-private\src\Elfie.sln";
            ////string target = @"C:\Code\Arriba\Arriba\Arriba.csproj";
            ////string target = @"C:\Code\Arriba\Arriba\bin\x86\Debug\Arriba.dll";
            ////string target = @"C:\Code\Roslyn\Roslyn.sln";
            ////string target = @"C:\Code\roslyn\Binaries\Debug";
            ////string target = @"C:\Code\roslyn\Binaries\Debug\Microsoft.CodeAnalysis.CSharp.dll";
            ////string target = @"C:\Code";
            ////string target = @"C:\Download\Newtonsoft.Json 7.0.1\Newtonsoft.Json.dll";

            //PackageIdentity identity = new PackageIdentity(Path.GetFileName(target));
            //PackageDatabase database;

            //// Index the database [Full]
            //database = Elfie.Indexer.Program.Index(target, true, false, identity);

            //// Save the database
            //Elfie.Indexer.Program.Save(database, IndexPath);
            //Elfie.Indexer.Program.SaveDiagnostics(database, IndexPath);

            //// Re-load the database
            //database = new PackageDatabase();
            //database.FileRead(Path.Combine(IndexPath, identity.IndexFileName));

            //// Test Go To Definition on loaded copy
            //GoToDefinitionPerformance(database, 100000);

            //// Test Search on loaded copy
            //SearchPerformance(database, 100000);

            //// Convert to AddReferenceDatabase
            //AddReferenceDatabase ardb = new AddReferenceDatabase();
            //using (new TraceWatch("Converting to AddReferenceDatabase..."))
            //{
            //    ardb.Add(database);
            //    ardb.ConvertToImmutable();
            //}
            //Trace.WriteLine(ardb.ToStatisticsString("public types"));

            //// Test Go To Definition on converted copy
            //GoToDefinitionPerformance(database, 100000);

            //// Test Search on converted copy
            //SearchPerformance(database, 100000);
        }

        private static void ComputeAndTestDeltas(string folderPath, string latestVersionPath)
        {
            using (new TraceWatch("Verifying latest file '{0}' loads...", latestVersionPath, folderPath))
            {
                AddReferenceDatabase latest = new AddReferenceDatabase(ArdbVersion.Current);
                Read.FromFile(latest.ReadText, latestVersionPath);
            }

            byte[] latestBytes = File.ReadAllBytes(latestVersionPath);

            using (new TraceWatch("Computing Deltas from '{0}' for all files in '{1}'...", latestVersionPath, folderPath))
            {
                foreach (string filePath in Directory.GetFiles(folderPath, "*.txt", SearchOption.AllDirectories))
                {
                    if (String.Equals(filePath, latestVersionPath, StringComparison.OrdinalIgnoreCase)) continue;

                    Trace.WriteLine(filePath);
                    byte[] currentBytes = File.ReadAllBytes(filePath);

                    byte[] delta = null;
                    using (new TraceWatch("Computing ARDB text delta..."))
                    {
                        byte[] latestCopy = new byte[latestBytes.Length];
                        latestBytes.CopyTo(latestCopy, 0);

                        byte[] currentCopy = new byte[currentBytes.Length];
                        currentBytes.CopyTo(currentCopy, 0);

                        delta = Delta.CreatePatch(currentCopy, latestCopy);
                        Trace.WriteLine(String.Format("{0:n0}", ((long)delta.Length).SizeString()));
                    }

                    using (new TraceWatch("Validating Delta..."))
                    {
                        byte[] currentWithDelta = Delta.ApplyPatch(currentBytes, delta);

                        if (latestBytes.Length != currentWithDelta.Length) throw new IOException(String.Format("Delta from '{0}' to '{1}' didn't reproduce latest. Wrong length. Aborting.", filePath, latestVersionPath));

                        for (int i = 0; i < latestBytes.Length; ++i)
                        {
                            if (latestBytes[i] != currentWithDelta[i])
                            {
                                throw new IOException(String.Format("Delta from '{0}' to '{1}' didn't reproduce latest; Aborting. Byte {2} wrong.\r\nExpect: {3}\r\nActual: {4}", filePath, latestVersionPath, i, latestBytes[i], currentWithDelta[i]));
                            }
                        }
                    }

                    using (new TraceWatch("Writing Delta..."))
                    {
                        File.WriteAllBytes(Path.ChangeExtension(filePath, ".Delta.bin"), delta);
                    }
                }
            }
        }

        private static void ReportEmptyPackages(AddReferenceDatabase ardb)
        {
            Console.WriteLine("Packages with few Types:");

            Symbol root = ardb.QueryRoot;

            for (Symbol package = root.FirstChild(); package.IsValid; package = package.NextSibling())
            {
                int typeCount = 0;
                package.Walk((s) => { if (s.Type.IsType()) typeCount++; });

                if (typeCount < 5)
                {
                    package.Name.WriteTo(Console.Out);
                    Console.WriteLine("\t" + typeCount.ToString());
                }
            }

            Console.WriteLine("\r\nDone.");
        }

        private static void WriteArdbVariations(string folderPath)
        {
            foreach (string filePath in Directory.GetFiles(folderPath, "*.txt", SearchOption.AllDirectories))
            {
                AddReferenceDatabase ardb = new AddReferenceDatabase();
                Read.FromFile(ardb.ReadText, filePath);

                // Write only the Package Names [easy diff of which packages included]
                using (StreamWriter writer = new StreamWriter(Path.ChangeExtension(filePath, "PackageOnly.txt"), false))
                {
                    ardb.WriteText(writer, (s) => s.Type == SymbolType.Package);
                }

                // Write without extension methods [compare sizes]
                using (StreamWriter writer = new StreamWriter(Path.ChangeExtension(filePath, "NoExtensionMethods.txt"), false))
                {
                    ardb.WriteText(writer, (s) => s.Type != SymbolType.ExtensionMethod);
                }

                // Write without long names
                using (StreamWriter writer = new StreamWriter(Path.ChangeExtension(filePath, "NoExtensionsOrLongNames.txt"), false))
                {
                    ardb.WriteText(writer, (s) => s.Type != SymbolType.ExtensionMethod && (s.Type.IsAboveType() || s.Name.Length <= 40));
                }
            }
        }

        private static void LoadSpeedTest(string sourcePath)
        {
            AddReferenceDatabase ardb = new AddReferenceDatabase();

            int iterations = 100;

            using (new TraceWatch("Loading '{0}' {1} times...", sourcePath, iterations))
            {
                for (int i = 0; i < iterations; ++i)
                {
                    ardb.FileRead(sourcePath);
                }
            }
        }

        private static void ConvertSerializationFormat(string sourcePath, string destinationPath)
        {
            using (new TraceWatch("Converting Serialization format from '{0}' to '{1}'...", sourcePath, destinationPath))
            {
                if (!String.IsNullOrEmpty(destinationPath) && !Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);

                foreach (string sourceFilePath in Directory.GetFiles(sourcePath))
                {
                    PackageDatabase db = new PackageDatabase();
                    db.FileRead(sourceFilePath);
                    db.FileWrite(Path.Combine(destinationPath, Path.GetFileName(sourceFilePath)));
                }
            }
        }

        private static void CountNonAsciiStrings(string databasePath)
        {
            int nonAsciiPackageDownloadCount = 0;

            int packageCount = 0;
            int stringCount = 0;

            int packagesWithNonAscii = 0;
            int stringsWithNonAscii = 0;

            using (new TraceWatch("Counting Non-Ascii in '{0}'...", databasePath))
            {
                // Load individual package databases
                foreach (PackageDatabase db in BinarySerializableExtensions.LoadEach<PackageDatabase>(Directory.GetFiles(databasePath), true))
                {
                    bool isPackageAllAscii = true;

                    StringStore strings = ((IMemberDatabase)db).StringStore;
                    for (int i = 0; i < strings.Count; ++i)
                    {
                        if (strings[i].IsAscii() == false)
                        {
                            stringsWithNonAscii++;
                            isPackageAllAscii = false;
                        }
                    }

                    stringCount += strings.Count;
                    packageCount++;
                    if (!isPackageAllAscii)
                    {
                        packagesWithNonAscii++;
                        nonAsciiPackageDownloadCount += db.Identity.DownloadCount;
                        Trace.WriteLine(String.Format("Non-ASCII: {0} [{1} downloads]", db.Identity.PackageName, db.Identity.DownloadCount));
                    }
                }
            }

            Console.WriteLine("{0:n0} / {1:n0} packages contain non-Ascii; {2:n0} / {3:n0} strings.", packagesWithNonAscii, packageCount, stringsWithNonAscii, stringCount);
            Console.WriteLine("Total Non-Ascii Package Downloads: {0:n0}", nonAsciiPackageDownloadCount);
        }

        private static void CalculatePopularityAndSize(string databasePath)
        {
            using (new TraceWatch("Calculating statistics on '{0}'...", databasePath))
            {
                PopularityAndSizeReport report = new PopularityAndSizeReport();

                // Load individual package databases
                foreach (PackageDatabase db in BinarySerializableExtensions.LoadEach<PackageDatabase>(Directory.GetFiles(databasePath), true))
                {
                    report.Add(db);
                }

                report.Write(databasePath + ".PopularityReport.csv");
            }
        }

        private static void GoToDefinitionPerformance(PackageDatabase db, int iterations = 1)
        {
            MemberQuery[] searches =
            {
                new MemberQuery("Arriba.Model.FastAddSortedColumn.SetSize", "ushort", true, true),
                new MemberQuery("Arriba.Model.FastAddSortedColumn.SetSize", "wrong", true, true),
                new MemberQuery("Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions", true, true),
                new MemberQuery("Newtonsoft.Json.JsonConvert", true, true),
                new MemberQuery("Elfie.Model.Index.MemberIndex.ConvertToImmutable", true, true)
            };

            GoToDefinition(db, searches, iterations);
        }

        private static void GoToDefinition(PackageDatabase db, MemberQuery[] searches, int iterations = 1)
        {
            PartialArray<Symbol> lastSuccessfulResults = new PartialArray<Symbol>();
            PartialArray<Symbol> results = new PartialArray<Symbol>(10);

            using (new TraceWatch("Go To Definition {0:n0} times across {1:n0} values...", iterations, searches.Length))
            {
                for (int iteration = 0; iteration < iterations; ++iteration)
                {
                    MemberQuery query = searches[iteration % searches.Length];
                    query.TryFindMembers(db, ref results);
                    if (results.Count > 0) results.CopyTo(ref lastSuccessfulResults);
                }
            }

            if (lastSuccessfulResults.Count > 0)
            {
                lastSuccessfulResults[0].WriteLocation(Console.Out);
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("NOT FOUND.");
            }

            Console.WriteLine();
        }

        public static PartialArray<Symbol> SearchPerformance(IMemberDatabase db, int iterations = 1)
        {
            MemberQuery[] searches = {
                new MemberQuery("St", false, false),                           // Small prefix in Elfie
                new MemberQuery("String8", false, false),                      // Short Type Name
                new MemberQuery("String8.", false, false),                     // Get Members
                new MemberQuery("String8.Com", false, false),                  // Get Members with filter
                new MemberQuery("Sorted", false, false),                       // Prefix in Arriba
                new MemberQuery("SortedColumn.Set", false, false),             // Longer Name and member
                new MemberQuery("Newtonsoft.Json.JsonC", false, false),        // Multi-part prefix in Json.NET
                new MemberQuery("JsonSerializerSettings.MaxD", false, false),  // Long type name in JSON.NET
                new MemberQuery("PENam", false, false),                        // Short name in Roslyn
                new MemberQuery("PENamedTypeSymbol.", false, false)            // Members in Roslyn
            };

            return SearchPerformance(db, searches, iterations);
        }

        public static PartialArray<Symbol> SearchPerformance(IMemberDatabase db, MemberQuery[] searches, int iterations = 1)
        {
            PartialArray<Symbol> lastSuccessfulResults = new PartialArray<Symbol>();
            PartialArray<Symbol> results = new PartialArray<Symbol>(20);

            using (new TraceWatch("Searching {0:n0} times across {1:n0} values...", iterations, searches.Length))
            {
                for (int iteration = 0; iteration < iterations; ++iteration)
                {
                    MemberQuery query = searches[iteration % searches.Length];
                    query.TryFindMembers(db, ref results);

                    if (results.Count > 0) results.CopyTo(ref lastSuccessfulResults);
                }
            }

            if (lastSuccessfulResults.Count > 0)
            {
                for (int i = 0; i < Math.Min(lastSuccessfulResults.Count, 5); ++i)
                {
                    Symbol result = lastSuccessfulResults[i];

                    int lengthWritten = 0;
                    lengthWritten += result.FullName.WriteTo(Console.Out);
                    Console.Write(new string(' ', Math.Max(1, 60 - lengthWritten)));
                    result.PackageName.WriteTo(Console.Out);
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("NOT FOUND.");
            }

            Console.WriteLine();
            return results;
        }
    }
}

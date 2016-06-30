// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

using CommandLine;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Merger
{
    public enum MergerExitCode : int
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
                var result = Parser.Default.ParseArguments<MergerOptions>(args);

                return (int)result.MapResult(
                    (arguments) =>
                    {
                        logger = new Logger(arguments.LogName);
                        logger.LogUse();
                        return Run(arguments);
                    },
                    (errors) =>
                    {
                        Console.WriteLine(MergerOptions.Usage);
                        return MergerExitCode.ArgumentsInvalid;
                    });
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                logger.LogException(ex);
                return (int)MergerExitCode.UnhandledException;
            }
        }

        public static MergerExitCode Run(MergerOptions options)
        {
            if (!File.Exists(options.PathToMerge) && !Directory.Exists(options.PathToMerge))
            {
                Console.WriteLine("Unable to find PathToMerge \"{0}\". Stopping.", options.PathToMerge);
                return MergerExitCode.PathToMergeNotFound;
            }

            Merge(options);

            return MergerExitCode.Success;
        }

        public static void Merge(MergerOptions options)
        {
            string individualLogs = options.PathToMerge + "Logs";
            if (Directory.Exists(individualLogs)) Directory.Delete(individualLogs, true);
            if (options.WithPackageOutcomes) Directory.CreateDirectory(individualLogs);

            using (new TraceWatch("Filtering '{0}' covering {1:p0} of downloads to Public Types Only...", options.PathToMerge, options.DownloadPercentage))
            {
                AddReferenceDatabase ardb = new AddReferenceDatabase(options.Version);
                ardb.DatabaseVersion = options.DatabaseVersion;

                // Determine indexes to include (up to popularity cutoff)
                List<string> indexesToInclude = null;

                if (Directory.Exists(options.PathToMerge))
                {
                    indexesToInclude = IndexesByPopularityToCutoff(Directory.EnumerateFiles(options.PathToMerge, "*.idx", SearchOption.AllDirectories), options.DownloadPercentage);

                    if (indexesToInclude.Count == 0)
                    {
                        Trace.WriteLine("No indexes found to include. Stopping.");
                        return;
                    }
                }
                else if (File.Exists(options.PathToMerge))
                {
                    // Text file listing IDS files passed
                    indexesToInclude = new List<string>(File.ReadAllLines(options.PathToMerge));
                }
                else
                {
                    throw new ArgumentException(String.Format("Merge doesn't know how to crawl passed path, '{0}'.", options.PathToMerge));
                }

                HashSet<string> excludedPackageNames = ParsePackageNames(options.ExcludedPackageNames);
                HashSet<string> filteringDisabledPackageNames = ParsePackageNames(options.DisableDuplicateFilteringPackageNames);

                // Load individual package databases in approximate download count order (prefix of name is scale of download count)
                ProgressWriter p = new ProgressWriter(indexesToInclude.Count);
                foreach (PackageDatabase db in BinarySerializableExtensions.LoadEach<PackageDatabase>(indexesToInclude, true))
                {
                    if (!String.IsNullOrEmpty(db.Identity.PackageName) && excludedPackageNames.Contains(db.Identity.PackageName))
                    {
                        Trace.WriteLine(String.Format("Excluded Package {0}", db.Identity.PackageName));
                    }
                    else
                    {
                        if (filteringDisabledPackageNames.Contains(db.Identity.PackageName))
                        {
                            ardb.AddReferenceAssemblyTypes(db);
                        }
                        else
                        {
                            DatabaseAddResult result = null;

                            ardb.AddUniqueMembers(db);

                            if (options.WithPackageOutcomes)
                            {
                                string log = Path.Combine(individualLogs, db.Identity.IndexFileName + ".log");
                                using (StreamWriter writer = new StreamWriter(log, false))
                                {
                                    result.WriteMemberResults(writer);
                                }
                            }
                        }
                    }

                    p.IncrementProgress();
                }

                // Include the percentage included in the name
                string outputFilePath = Path.Combine(options.OutputPath, Path.GetFileName(Path.GetFullPath(options.PathToMerge)) + "." + (options.DownloadPercentage * 100).ToString("g0"));

                // Write the merged tree, if requested (debuggability)
                if (options.WithMergedTreeLog)
                {
                    string uniqueTreePath = options.PathToMerge + ".MergedTree.log";
                    using (new TraceWatch("Writing Unique Global Namespace tree to '{0}'...", uniqueTreePath))
                    {
                        using (StreamWriter writer = new StreamWriter(uniqueTreePath, false))
                        {
                            ardb.GetMergedMembers().WriteMergedTree(writer);
                        }
                    }
                }

                // Write the binary and text forms of the ARDB
                using (new TraceWatch("Writing AddReferenceDatabase '{0}'...", outputFilePath))
                {
                    ardb.FileWrite(outputFilePath + ".ardb");
                    CreateZip(outputFilePath + ".ardb");

                    Write.ToFile(ardb.WriteText, outputFilePath + ".ardb.txt");
                    CreateZip(outputFilePath + ".ardb.txt");
                }
            }
        }

        private static HashSet<string> ParsePackageNames(string delimitedPackageNames)
        {
            // Filter out packages in the exclude list, if any
            HashSet<string> packageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!String.IsNullOrEmpty(delimitedPackageNames))
            {
                foreach (string packageName in delimitedPackageNames.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    packageNames.Add(packageName.Trim());
                }
            }
            return packageNames;
        }

        private static void CreateZip(string filePath)
        {
            string zipFilePath = filePath + ".zip";
            if (File.Exists(zipFilePath)) File.Delete(zipFilePath);

            using (ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
            }
        }

        private class PackageIndexPopularity
        {
            public string PackageFileName { get; set; }
            public int DownloadCount { get; set; }
            public List<string> IndexPaths { get; set; }

            public PackageIndexPopularity(string packageFileName)
            {
                this.PackageFileName = packageFileName;
                this.IndexPaths = new List<string>();
            }

            public void Add(int downloadCount, string indexPath)
            {
                if (downloadCount > this.DownloadCount) this.DownloadCount = downloadCount;
                this.IndexPaths.Add(indexPath);
            }
        }

        public static List<string> IndexesByPopularityToCutoff(IEnumerable<string> indexFilePaths, double downloadPercentageIncluded)
        {
            Dictionary<string, PackageIndexPopularity> indexesByPackageName = new Dictionary<string, PackageIndexPopularity>();

            // Find the download count for each package and the path of every copy [multiple framework target copies may be included]
            foreach (string filePath in indexFilePaths)
            {
                string packageFileName = Path.GetFileName(filePath);

                using (FileStream s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    BinaryReader reader = new BinaryReader(s);
                    int downloadCount = PackageDatabase.ReadDownloadsOnly(reader);

                    PackageIndexPopularity pip = null;
                    if (!indexesByPackageName.TryGetValue(packageFileName, out pip))
                    {
                        pip = new PackageIndexPopularity(packageFileName);
                        indexesByPackageName[packageFileName] = pip;
                    }

                    pip.Add(downloadCount, filePath);
                }
            }

            // Compute the total download count across unique packages
            long totalDownloadCount = indexesByPackageName.Select((kvp) => (long)kvp.Value.DownloadCount).Sum();

            // Include all copies of each package until we've included more than the target percentage of downloads
            PackagePopularityOrderSorter sorter = new PackagePopularityOrderSorter();
            long downloadCountToCover = (long)(totalDownloadCount * downloadPercentageIncluded);
            long downloadCountSoFar = 0;

            List<string> indexesToInclude = new List<string>();

            foreach (PackageIndexPopularity pip in indexesByPackageName.Values.OrderBy((pip) => pip, sorter))
            {
                if (downloadPercentageIncluded < 1 && downloadCountSoFar >= downloadCountToCover) break;
                downloadCountSoFar += pip.DownloadCount;
                indexesToInclude.AddRange(pip.IndexPaths);
            }

            return indexesToInclude;
        }

        private static void ConvertAddReferenceDatabaseFromText(string textFilePath)
        {
            AddReferenceDatabase ardb = new AddReferenceDatabase();

            using (new TraceWatch("Converting '{0}' from text to binary model...", textFilePath))
            {
                Read.FromFile(ardb.ReadText, textFilePath);
                ardb.FileWrite(textFilePath.Substring(0, textFilePath.Length - 4));
            }

            int count = ardb.Count;
            long bytes = ardb.Bytes;
            Trace.WriteLine(String.Format("{0:n0} items in {1}, {2}/item.\r\n", count, bytes.SizeString(), (bytes / count).SizeString()));
        }

        /// <summary>
        ///  IComparer which sorts by log base two popularity descending and then by name.
        ///  Used to sort Packages so that more popular packages are first but the order
        ///  doesn't change too much day to day [so that ARDB diffs are small to send to clients].
        /// </summary>
        private class PackagePopularityOrderSorter : IComparer<PackageIndexPopularity>
        {
            public int Compare(PackageIndexPopularity x, PackageIndexPopularity y)
            {
                int xPopularity = (int)Math.Log(x.DownloadCount, 2);
                int yPopularity = (int)Math.Log(y.DownloadCount, 2);

                int popularityCompareDesc = yPopularity.CompareTo(xPopularity);
                if (popularityCompareDesc != 0) return popularityCompareDesc;

                return x.PackageFileName.CompareTo(y.PackageFileName);
            }
        }
    }
}

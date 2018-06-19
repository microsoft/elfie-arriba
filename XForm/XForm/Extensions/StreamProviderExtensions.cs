// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using XForm.IO;
using XForm.IO.StreamProvider;

namespace XForm.Extensions
{
    /// <summary>
    ///  StreamProviderExtensions provides higher level functions on the IStreamProvider in terms
    ///  of IStreamProvider primitives.
    /// </summary>
    public static class StreamProviderExtensions
    {
        public const string DateTimeFolderFormat = "yyyy.MM.dd HH.mm.ssZ";

        public static bool Exists(this IStreamProvider streamProvider, string logicalPath)
        {
            return streamProvider.Attributes(logicalPath).Exists;
        }

        public static bool UncachedExists(this IStreamProvider streamProvider, string logicalPath)
        {
            try
            {
                using (Stream stream = streamProvider.OpenRead(logicalPath))
                {
                    return true;
                }
            }
            catch(IOException)
            { }

            return false;
        }

        public static string Path(this IStreamProvider streamProvider, LocationType type, string tableName, string extension)
        {
            return System.IO.Path.Combine(type.ToString(), tableName + extension);
        }

        public static string Path(this IStreamProvider streamProvider, LocationType type, string tableName, CrawlType crawlType)
        {
            return System.IO.Path.Combine(type.ToString(), tableName, crawlType.ToString());
        }

        public static string Path(this IStreamProvider streamProvider, LocationType type, string tableName, CrawlType crawlType, DateTime version)
        {
            return System.IO.Path.Combine(type.ToString(), tableName, crawlType.ToString(), version.ToUniversalTime().ToString(DateTimeFolderFormat));
        }

        public static void Add(this IStreamProvider streamProvider, string sourceFileOrFolderPath, string tableName, CrawlType crawlType, DateTime asOfDateTime = default(DateTime))
        {
            // If the 'asOfDateTime' wasn't passed, use the File Write Time
            if (asOfDateTime == default(DateTime)) asOfDateTime = File.GetLastWriteTimeUtc(sourceFileOrFolderPath);

            string desiredFolderPath = streamProvider.Path(LocationType.Source, tableName, crawlType, asOfDateTime);

            if (Directory.Exists(sourceFileOrFolderPath))
            {
                foreach (string filePath in Directory.GetFiles(sourceFileOrFolderPath, "*.*", SearchOption.AllDirectories))
                {
                    streamProvider.Copy(File.OpenRead(filePath), System.IO.Path.Combine(desiredFolderPath, System.IO.Path.GetFileName(filePath)));
                }
            }
            else
            {
                streamProvider.Copy(File.OpenRead(sourceFileOrFolderPath), System.IO.Path.Combine(desiredFolderPath, System.IO.Path.GetFileName(sourceFileOrFolderPath)));
            }
        }

        public static void Clean(this IStreamProvider streamProvider, bool reallyDelete, DateTime cutoff = default(DateTime))
        {
            // By default, keep data from the last week (so Now, Yesterday, and Last Week 'as of' data is all available)
            if (cutoff == default(DateTime)) cutoff = DateTime.UtcNow.AddDays(-8);

            // Remove Sources and Tables older than the cutoff
            ItemVersions currentVersions;
            int countLeft;

            foreach (string tableName in streamProvider.Tables())
            {
                // Find all Source versions
                currentVersions = streamProvider.ItemVersions(LocationType.Source, tableName);
                countLeft = currentVersions.Versions.Count;

                // Delete ones older than the cutoff, keeping the last three
                foreach (var version in currentVersions.Versions)
                {
                    if (version.AsOfDate < cutoff)
                    {
                        if (countLeft <= 3) break;
                        countLeft--;

                        Trace.WriteLine($"DELETE {version.Path}");
                        if (reallyDelete)
                        {
                            // Delete the source
                            streamProvider.DeleteWithRetries(version.Path);
                            version.LocationType = LocationType.Table;

                            // Delete the matching table, if found
                            streamProvider.DeleteWithRetries(version.Path);
                        }
                    }
                }

                // Find all Table versions
                currentVersions = streamProvider.ItemVersions(LocationType.Table, tableName);
                countLeft = currentVersions.Versions.Count;

                // Delete ones older than the cutoff, keeping the last three
                foreach (var version in currentVersions.Versions)
                {
                    if (version.AsOfDate < cutoff)
                    {
                        if (countLeft <= 3) break;
                        countLeft--;

                        Trace.WriteLine($"DELETE {version.Path}");
                        if (reallyDelete) streamProvider.DeleteWithRetries(version.Path);
                    }
                }
            }
        }

        public static void DeleteWithRetries(this IStreamProvider streamProvider, string logicalPath, int tryCount = 3)
        {
            Exception lastException = null;

            for(int i = 0; i < tryCount; ++i)
            {
                try
                {
                    streamProvider.Delete(logicalPath);
                    return;
                }
                catch(Exception ex)
                {
                    lastException = ex;
                    Thread.Sleep(2500);
                }
            }

            throw new TimeoutException($"Timed out trying to delete '{logicalPath}' after {tryCount:n0} tries.", lastException);
        }

        public static bool ContainsTable(this IStreamProvider streamProvider, string tableName)
        {
            // Don't allow trailing characters on table name which File I/O removes
            if (tableName.EndsWith(".") || tableName.EndsWith("\\") || tableName.EndsWith("/")) return false;

            if (streamProvider.Attributes(Path(streamProvider, LocationType.Source, tableName, CrawlType.Full)).Exists) return true;
            if (streamProvider.Attributes(Path(streamProvider, LocationType.Table, tableName, CrawlType.Full)).Exists) return true;
            if (streamProvider.Attributes(Path(streamProvider, LocationType.Config, tableName, ".xql")).Exists) return true;
            if (streamProvider.Attributes(Path(streamProvider, LocationType.Query, tableName, ".xql")).Exists) return true;

            return false;
        }

        public static IEnumerable<string> Tables(this IStreamProvider streamProvider)
        {
            HashSet<string> tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Return all Configs as tables
            foreach (StreamAttributes item in streamProvider.Enumerate("Config", EnumerateTypes.File, true))
            {
                if (item.Path.EndsWith(".xql", StringComparison.OrdinalIgnoreCase))
                {
                    tables.Add(item.Path.RelativePath("Config\\", ".xql"));
                }
            }

            // Return raw Sources as tables
            AddFullFolderContainers(streamProvider, "Source", tables);

            // Return built Tables as tables
            AddFullFolderContainers(streamProvider, "Table", tables);

            return tables;
        }

        private static void AddFullFolderContainers(this IStreamProvider streamProvider, string underPath, HashSet<string> results)
        {
            AddFullFolderContainers(streamProvider, underPath + "\\", underPath, results);
        }

        private static void AddFullFolderContainers(this IStreamProvider streamProvider, string rootToRemove, string underPath, HashSet<string> results)
        {
            foreach (StreamAttributes item in streamProvider.Enumerate(underPath, EnumerateTypes.Folder, false))
            {
                if (item.Path.EndsWith("\\Full"))
                {
                    // If this has a 'Full' folder in it, add it and stop recursing
                    results.Add(underPath.RelativePath(rootToRemove));
                    return;
                }

                // Otherwise look under this folder
                AddFullFolderContainers(streamProvider, rootToRemove, item.Path, results);
            }
        }

        public static IEnumerable<string> Queries(this IStreamProvider streamProvider)
        {
            // Return all Queries
            foreach (StreamAttributes item in streamProvider.Enumerate("Query", EnumerateTypes.File, true))
            {
                if (item.Path.EndsWith(".xql", StringComparison.OrdinalIgnoreCase))
                {
                    yield return item.Path.RelativePath("Query\\", ".xql");
                }
            }
        }

        public static IEnumerable<string> SourceNames(this IStreamProvider streamProvider)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            set.UnionWith(streamProvider.Tables());
            set.UnionWith(streamProvider.Queries());
            return set;
        }

        public static string ReadAllText(this IStreamProvider streamProvider, string path)
        {
            using (StreamReader reader = new StreamReader(streamProvider.OpenRead(path)))
            {
                return reader.ReadToEnd();
            }
        }

        public static void WriteAllText(this IStreamProvider streamProvider, string path, string content)
        {
            using (StreamWriter writer = new StreamWriter(streamProvider.OpenWrite(path)))
            {
                writer.Write(content);
            }
        }

        public static void Copy(this IStreamProvider streamProvider, Stream source, string targetPath)
        {
            using (source)
            {
                using (Stream output = streamProvider.OpenWrite(targetPath))
                {
                    source.CopyTo(output);
                }
            }
        }

        public static void Copy(this IStreamProvider streamProvider, string sourceFilePath, string targetPath)
        {
            Copy(streamProvider, File.OpenRead(sourceFilePath), targetPath);
        }
    }
}

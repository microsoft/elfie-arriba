// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

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

        public static StreamAttributes LatestBeforeCutoff(this IStreamProvider streamProvider, LocationType type, string tableName, CrawlType crawlType, DateTime asOfDateTime)
        {
            // Find the last crawl which isn't after the cutoff
            StreamAttributes latestStream = StreamAttributes.NotExists;
            DateTime latestStreamVersion = DateTime.MinValue;

            string sourceFullPath = streamProvider.Path(type, tableName, crawlType);
            foreach (StreamAttributes version in streamProvider.Enumerate(sourceFullPath, EnumerateTypes.Folder, false))
            {
                DateTime versionAsOf;
                if (!DateTime.TryParseExact(System.IO.Path.GetFileName(version.Path), DateTimeFolderFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out versionAsOf))
                {
                    continue;
                }

                // Track the latest version, modifying the WhenModifiedUtc to be the folder stamp and not the actual file time
                if (versionAsOf > latestStreamVersion && versionAsOf <= asOfDateTime)
                {
                    latestStream = version;
                    latestStream.WhenModifiedUtc = versionAsOf;
                }
            }

            return latestStream;
        }

        public static IEnumerable<StreamAttributes> VersionsInRange(this IStreamProvider streamProvider, LocationType type, string tableName, CrawlType crawlType, DateTime startDateTime, DateTime asOfDateTime)
        {
            string sourceFullPath = streamProvider.Path(type, tableName, crawlType);
            foreach (StreamAttributes version in streamProvider.Enumerate(sourceFullPath, EnumerateTypes.Folder, false))
            {
                DateTime versionAsOf;
                if (!DateTime.TryParseExact(System.IO.Path.GetFileName(version.Path), DateTimeFolderFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out versionAsOf))
                {
                    continue;
                }

                // Track the latest version, modifying the WhenModifiedUtc to be the folder stamp and not the actual file time
                if (versionAsOf > startDateTime && versionAsOf <= asOfDateTime)
                {
                    version.WhenModifiedUtc = versionAsOf;
                    yield return version;
                }
            }
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

            return tables;
        }

        private static void AddFullFolderContainers(this IStreamProvider streamProvider, string underPath, HashSet<string> results)
        {
            foreach (StreamAttributes item in streamProvider.Enumerate(underPath, EnumerateTypes.Folder, false))
            {
                if (item.Path.EndsWith("\\Full"))
                {
                    // If this has a 'Full' folder in it, add it and stop recursing
                    results.Add(underPath.RelativePath("Source\\"));
                    return;
                }

                // Otherwise look under this folder
                AddFullFolderContainers(streamProvider, item.Path, results);
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
    }
}

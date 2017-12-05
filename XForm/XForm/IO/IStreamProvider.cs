using Microsoft.CodeAnalysis.Elfie.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace XForm.IO
{
    public enum CrawlType
    {
        Full,
        Incremental
    }

    public enum LocationType
    {
        Source,
        Config,
        Table,
        Query,
        Report
    }

    public class StreamAttributes
    {
        public string Path { get; set; }
        public bool Exists { get; set; }
        public long Length { get; set; }
        public DateTime WhenModifiedUtc { get; set; }

        public override string ToString()
        {
            return Exists ? $"{Path} {Length.SizeString()} {WhenModifiedUtc:u}" : $"{Path} [NOT EXISTS]";
        }

        public static StreamAttributes NotExists = new StreamAttributes() { Exists = false, WhenModifiedUtc = DateTime.MinValue };
    }

    public interface IStreamProvider
    {
        Stream OpenRead(string logicalPath);
        Stream OpenWrite(string logicalPath);
        Stream OpenAppend(string logicalPath);
        StreamAttributes Attributes(string logicalPath);

        IEnumerable<StreamAttributes> Enumerate(string underLogicalPath, bool recursive);
        void Publish(string logicalTablePath);
    }

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

        public static StreamAttributes LatestBeforeCutoff(this IStreamProvider streamProvider, LocationType type, string tableName, DateTime asOfDateTime)
        {
            // Find the last Full crawl which isn't after the cutoff
            StreamAttributes latestStream = StreamAttributes.NotExists;
            DateTime latestStreamVersion = DateTime.MinValue;

            string sourceFullPath = streamProvider.Path(type, tableName, CrawlType.Full);
            foreach(StreamAttributes version in streamProvider.Enumerate(sourceFullPath, false))
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

        public static IEnumerable<string> Tables(this IStreamProvider streamProvider)
        {
            HashSet<string> tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Return all Configs as tables
            foreach(StreamAttributes item in streamProvider.Enumerate("Config", true))
            {
                if(item.Path.EndsWith(".xql", StringComparison.OrdinalIgnoreCase))
                {
                    tables.Add(item.Path.Substring(0, item.Path.Length - 4));
                }
            }

            // Return raw Sources as tables
            AddFullFolderContainers(streamProvider, "Source", tables);

            return tables;
        }

        private static void AddFullFolderContainers(this IStreamProvider streamProvider, string underPath, HashSet<string> results)
        {
            foreach (StreamAttributes item in streamProvider.Enumerate(underPath, false))
            {
                if (item.Path.EndsWith("\\Full"))
                {
                    // If this has a 'Full' folder in it, add it and stop recursing
                    results.Add(underPath);
                    return;
                }

                // Otherwise look under this folder
                AddFullFolderContainers(streamProvider, item.Path, results);
            }
        }

        public static IEnumerable<string> Queries(this IStreamProvider streamProvider)
        {
            // Return all Queries
            foreach (StreamAttributes item in streamProvider.Enumerate("Config", true))
            {
                if (item.Path.EndsWith(".xql", StringComparison.OrdinalIgnoreCase))
                {
                    yield return item.Path.Substring(0, item.Path.Length - 4);
                }
            }
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

    public class LocalFileStreamProvider : IStreamProvider
    {
        private string RootPath { get; set; }

        public LocalFileStreamProvider(string rootPath)
        {
            this.RootPath = Path.GetFullPath(rootPath);
        }

        public StreamAttributes Attributes(string logicalPath)
        {
            string realPath = PathCombineSandbox(logicalPath);
            if (!File.Exists(realPath)) return new StreamAttributes() { Exists = false, Path = logicalPath };
            return Convert(new FileInfo(realPath));
        }

        public IEnumerable<StreamAttributes> Enumerate(string underLogicalPath, bool recursive)
        {
            SearchOption searchOption = (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            string underPath = PathCombineSandbox(underLogicalPath);
            if (Directory.Exists(underPath))
            {
                DirectoryInfo underDir = new DirectoryInfo(underPath);

                foreach (FileInfo file in underDir.EnumerateFiles("*.*", searchOption))
                {
                    yield return Convert(file);
                }

                foreach (DirectoryInfo folder in underDir.EnumerateDirectories("*.*", searchOption))
                {
                    yield return Convert(folder);
                }
            }
        }

        public Stream OpenAppend(string logicalPath)
        {
            string physicalPath = PathCombineSandbox(logicalPath);
            EnsureFolderExists(physicalPath);
            return new FileStream(physicalPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read | FileShare.Delete);
        }

        public Stream OpenRead(string logicalPath)
        {
            return new FileStream(PathCombineSandbox(logicalPath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Stream OpenWrite(string logicalPath)
        {
            string physicalPath = PathCombineSandbox(logicalPath);
            EnsureFolderExists(physicalPath);
            return new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete);
        }

        public void Publish(string logicalTablePath)
        {
            // Nothing: Files already written
        }

        private StreamAttributes Convert(FileInfo info)
        {
            return new StreamAttributes() { Exists = true, Path = info.FullName.Substring(RootPath.Length + 1), Length = info.Length, WhenModifiedUtc = info.LastWriteTimeUtc };
        }


        private StreamAttributes Convert(DirectoryInfo info)
        {
            return new StreamAttributes() { Exists = true, Path = info.FullName.Substring(RootPath.Length + 1), Length = 0, WhenModifiedUtc = info.LastWriteTimeUtc };
        }

        private string PathCombineSandbox(string logicalPath)
        {
            string result = Path.Combine(RootPath, logicalPath);
            if (!result.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException($"LocalFileStreamProvider won't open \"{logicalPath}\" because it isn't under \"{RootPath}\".");
            return result;
        }

        private void EnsureFolderExists(string physicalPath)
        {
            if (String.IsNullOrEmpty(physicalPath)) return;
            string folderPath = Path.GetDirectoryName(physicalPath);
            if (!String.IsNullOrEmpty(folderPath)) Directory.CreateDirectory(folderPath);
        }
    }
}

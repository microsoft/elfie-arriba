// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using XForm.Extensions;

namespace XForm.IO.StreamProvider
{
    /// <summary>
    ///  LocalFileStreamProvider is an IStreamProvider which reads a Database folder
    ///  in the local file system. It requires all inputs and outputs to be under the
    ///  root path.
    /// </summary>
    public class LocalFileStreamProvider : IStreamProvider
    {
        private string RootPath { get; set; }

        public LocalFileStreamProvider(string rootPath)
        {
            this.RootPath = Path.GetFullPath(rootPath);
        }

        public string Description => RootPath;

        public StreamAttributes Attributes(string logicalPath)
        {
            string realPath = PathCombineSandbox(logicalPath);
            if (File.Exists(realPath)) return Convert(new FileInfo(realPath));
            if (Directory.Exists(realPath)) return Convert(new DirectoryInfo(realPath));
            return new StreamAttributes() { Exists = false, Path = logicalPath };
        }

        public IEnumerable<StreamAttributes> Enumerate(string underLogicalPath, EnumerateTypes types, bool recursive)
        {
            SearchOption searchOption = (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            string underPath = PathCombineSandbox(underLogicalPath);
            if (Directory.Exists(underPath))
            {
                DirectoryInfo underDir = new DirectoryInfo(underPath);

                if (types.HasFlag(EnumerateTypes.File))
                {
                    foreach (FileInfo file in underDir.EnumerateFiles("*.*", searchOption))
                    {
                        yield return Convert(file);
                    }
                }

                if (types.HasFlag(EnumerateTypes.Folder))
                {
                    foreach (DirectoryInfo folder in underDir.EnumerateDirectories("*.*", searchOption))
                    {
                        yield return Convert(folder);
                    }
                }
            }
        }

        public void Delete(string logicalPath)
        {
            DirectoryIO.DeleteAll(PathCombineSandbox(logicalPath));
        }

        public Stream OpenAppend(string logicalPath)
        {
            string physicalPath = PathCombineSandbox(logicalPath);
            EnsureFolderExists(physicalPath);
            return new BufferedStream(new FileStream(physicalPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read | FileShare.Delete), 64 * 1024);
        }

        public Stream OpenRead(string logicalPath)
        {
            return new FileStream(PathCombineSandbox(logicalPath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }

        public Stream OpenWrite(string logicalPath)
        {
            if (logicalPath.Equals("cout", StringComparison.OrdinalIgnoreCase)) return Console.OpenStandardOutput();

            string physicalPath = PathCombineSandbox(logicalPath);
            EnsureFolderExists(physicalPath);
            return new BufferedStream(new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete), 64 * 1024);
        }

        public void Publish(string logicalTablePath)
        {
            // Nothing: Files already written
        }

        private StreamAttributes Convert(FileInfo info)
        {
            return new StreamAttributes() { Exists = true, Type = EnumerateTypes.Folder, Path = info.FullName.Substring(RootPath.Length + 1), Length = info.Length, WhenModifiedUtc = info.LastWriteTimeUtc };
        }

        private StreamAttributes Convert(DirectoryInfo info)
        {
            return new StreamAttributes() { Exists = true, Type = EnumerateTypes.Folder, Path = info.FullName.Substring(RootPath.Length + 1), Length = 0, WhenModifiedUtc = info.LastWriteTimeUtc };
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

        public ItemVersions ItemVersions(LocationType location, string itemName)
        {
            ItemVersions versions = new ItemVersions(location, itemName);
            AddVersions(versions, CrawlType.Full);
            AddVersions(versions, CrawlType.Inc);
            return versions;
        }

        private void AddVersions(ItemVersions versions, CrawlType crawlType)
        {
            string sourceFullPath = this.Path(versions.LocationType, versions.Name, crawlType);
            foreach (StreamAttributes version in Enumerate(sourceFullPath, EnumerateTypes.Folder, false))
            {
                DateTime versionAsOf;

                // If it's not a DateTime folder, it's not a table version
                if (!DateTime.TryParseExact(System.IO.Path.GetFileName(version.Path), StreamProviderExtensions.DateTimeFolderFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out versionAsOf))
                {
                    continue;
                }

                // If we're looking at tables, verify there's a Schema.csv
                if (versions.LocationType == LocationType.Table && !Attributes(Path.Combine(version.Path, "Schema.csv")).Exists)
                {
                    continue;
                }

                versions.AddVersion(crawlType, versionAsOf);
            }
        }
    }
}

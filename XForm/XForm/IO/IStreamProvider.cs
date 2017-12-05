using Microsoft.CodeAnalysis.Elfie.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace XForm.IO
{
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
            string underPath = PathCombineSandbox(underLogicalPath);
            if (Directory.Exists(underPath))
            {
                foreach (FileInfo file in new DirectoryInfo(underPath).EnumerateFiles())
                {
                    yield return Convert(file);
                }
            }
        }

        public Stream OpenAppend(string logicalPath)
        {
            return new FileStream(PathCombineSandbox(logicalPath), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read | FileShare.Delete);
        }

        public Stream OpenRead(string logicalPath)
        {
            return new FileStream(PathCombineSandbox(logicalPath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Stream OpenWrite(string logicalPath)
        {
            return new FileStream(PathCombineSandbox(logicalPath), FileMode.Create, FileAccess.Read, FileShare.Read | FileShare.Delete);
        }

        public void Publish(string logicalTablePath)
        {
            // Nothing: Files already written
        }

        private StreamAttributes Convert(FileInfo info)
        {
            return new StreamAttributes() { Exists = true, Path = info.FullName.Substring(RootPath.Length + 1), Length = info.Length, WhenModifiedUtc = info.LastWriteTimeUtc };
        }

        private string PathCombineSandbox(string logicalPath)
        {
            string result = Path.Combine(RootPath, logicalPath);
            if (!result.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException($"LocalFileStreamProvider won't open \"{logicalPath}\" because it isn't under \"{RootPath}\".");
            return result;
        }
    }
}

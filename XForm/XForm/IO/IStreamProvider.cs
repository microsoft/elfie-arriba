using Microsoft.CodeAnalysis.Elfie.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace XForm.IO
{
    /// <summary>
    ///  IStreamProvider provides streams for components to read and write. 
    ///  It wraps the file system and can enforce compression, encryption, 
    ///  download from remote sources, and publishing.
    /// </summary>
    public interface IStreamProvider
    {
        Stream OpenRead(string logicalPath);
        Stream OpenWrite(string logicalPath);
        Stream OpenAppend(string logicalPath);
        StreamAttributes Attributes(string logicalPath);

        IEnumerable<StreamAttributes> Enumerate(string underLogicalPath, bool recursive);
        void Publish(string logicalTablePath);
        void Delete(string logicalPath);
    }

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
}

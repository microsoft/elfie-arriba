// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Extensions;

namespace XForm.IO.StreamProvider
{
    /// <summary>
    ///  IStreamProvider provides streams for components to read and write. 
    ///  It wraps the file system and can enforce compression, encryption, 
    ///  download from remote sources, and publishing.
    /// </summary>
    public interface IStreamProvider
    {
        string Description { get; }

        Stream OpenRead(string logicalPath);
        Stream OpenWrite(string logicalPath);
        Stream OpenAppend(string logicalPath);
        StreamAttributes Attributes(string logicalPath);

        ItemVersions ItemVersions(LocationType location, string itemName);

        IEnumerable<StreamAttributes> Enumerate(string underLogicalPath, EnumerateTypes types, bool recursive);
        void Publish(string logicalTablePath);
        void Delete(string logicalPath);
    }

    [Flags]
    public enum EnumerateTypes
    {
        None = 0,
        File = 1,
        Folder = 2,
        Both = 3
    }

    public enum CrawlType
    {
        Full,
        Inc
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
        public EnumerateTypes Type { get; set; }

        public override string ToString()
        {
            return Exists ? $"{Path} {Length.SizeString()} {WhenModifiedUtc:u}" : $"{Path} [NOT EXISTS]";
        }

        public static StreamAttributes NotExists = new StreamAttributes() { Exists = false, WhenModifiedUtc = DateTime.MinValue };
    }
}

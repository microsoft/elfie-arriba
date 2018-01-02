// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace XForm.IO.StreamProvider
{
    public class DeflateStreamProvider : IStreamProvider
    {
        private IStreamProvider Inner { get; set; }

        public DeflateStreamProvider(IStreamProvider inner)
        {
            this.Inner = inner;
        }

        public string Description => $"{Inner.Description}/ZIP";

        public StreamAttributes Attributes(string logicalPath)
        {
            // If there's a wrapping zip, return the attributes for it with the base path
            StreamAttributes zipAttributes = Inner.Attributes(logicalPath + ".zip");
            if (zipAttributes.Exists)
            {
                zipAttributes.Path = logicalPath;
                return zipAttributes;
            }

            return Inner.Attributes(logicalPath);
        }

        public void Delete(string logicalPath)
        {
            if (Inner.Attributes(logicalPath + ".zip").Exists)
            {
                Inner.Delete(logicalPath + ".zip");
            }
            else
            {
                Inner.Delete(logicalPath);
            }
        }

        public IEnumerable<StreamAttributes> Enumerate(string underLogicalPath, EnumerateTypes types, bool recursive)
        {
            // Enumerate items but make them look like the unpacked zip files
            foreach (StreamAttributes item in Inner.Enumerate(underLogicalPath, types, recursive))
            {
                if (item.Path.EndsWith(".zip")) item.Path = item.Path.Substring(0, item.Path.Length - 4);
                yield return item;
            }
        }

        public Stream OpenAppend(string logicalPath)
        {
            // Always write compressed
            return new DeflateStream(Inner.OpenAppend(logicalPath + ".zip"), CompressionMode.Compress);
        }

        public Stream OpenRead(string logicalPath)
        {
            // If there's a zip, open that
            if (Inner.Attributes(logicalPath + ".zip").Exists)
            {
                return new DeflateStream(Inner.OpenRead(logicalPath + ".zip"), CompressionMode.Decompress);
            }

            // If not, open the normal file
            return Inner.OpenRead(logicalPath);
        }

        public Stream OpenWrite(string logicalPath)
        {
            // Always write compressed
            return new DeflateStream(Inner.OpenWrite(logicalPath + ".zip"), CompressionMode.Compress);
        }

        public void Publish(string logicalTablePath)
        {
            Inner.Publish(logicalTablePath);
        }
    }
}

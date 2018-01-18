// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace XForm.IO.StreamProvider
{
    public class DeflateStreamProvider : IStreamProvider
    {
        private IStreamProvider _inner;

        public DeflateStreamProvider(IStreamProvider inner)
        {
            _inner = inner;
        }

        public string Description => $"{_inner.Description}/ZIP";

        public StreamAttributes Attributes(string logicalPath)
        {
            // If there's a wrapping zip, return the attributes for it with the base path
            StreamAttributes zipAttributes = _inner.Attributes(logicalPath + ".zip");
            if (zipAttributes.Exists)
            {
                zipAttributes.Path = logicalPath;
                return zipAttributes;
            }

            return _inner.Attributes(logicalPath);
        }

        public void Delete(string logicalPath)
        {
            if (_inner.Attributes(logicalPath + ".zip").Exists)
            {
                _inner.Delete(logicalPath + ".zip");
            }
            else
            {
                _inner.Delete(logicalPath);
            }
        }

        public IEnumerable<StreamAttributes> Enumerate(string underLogicalPath, EnumerateTypes types, bool recursive)
        {
            // Enumerate items but make them look like the unpacked zip files
            foreach (StreamAttributes item in _inner.Enumerate(underLogicalPath, types, recursive))
            {
                if (item.Path.EndsWith(".zip")) item.Path = item.Path.Substring(0, item.Path.Length - 4);
                yield return item;
            }
        }

        public Stream OpenAppend(string logicalPath)
        {
            // Always write compressed
            return new DeflateStream(_inner.OpenAppend(logicalPath + ".zip"), CompressionMode.Compress);
        }

        public Stream OpenRead(string logicalPath)
        {
            // If there's a zip, open that
            if (_inner.Attributes(logicalPath + ".zip").Exists)
            {
                return new DeflateStream(_inner.OpenRead(logicalPath + ".zip"), CompressionMode.Decompress);
            }

            // If not, open the normal file
            return _inner.OpenRead(logicalPath);
        }

        public Stream OpenWrite(string logicalPath)
        {
            // Always write compressed
            return new DeflateStream(_inner.OpenWrite(logicalPath + ".zip"), CompressionMode.Compress);
        }

        public void Publish(string logicalTablePath)
        {
            _inner.Publish(logicalTablePath);
        }

        public ItemVersions ItemVersions(LocationType location, string itemName)
        {
            return _inner.ItemVersions(location, itemName);
        }
    }
}

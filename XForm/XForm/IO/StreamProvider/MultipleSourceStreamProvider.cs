// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace XForm.IO.StreamProvider
{
    public class MultipleSourceStreamConfiguration
    {
        public bool ShouldDownload { get; set; }
        public bool ShouldPublish { get; set; }
        public bool UseRemoteConfiguration { get; set; }

        public static MultipleSourceStreamConfiguration LocalBranch = new MultipleSourceStreamConfiguration() { ShouldDownload = false, ShouldPublish = false, UseRemoteConfiguration = false };
        public static MultipleSourceStreamConfiguration ProductionNode = new MultipleSourceStreamConfiguration() { ShouldDownload = true, ShouldPublish = true, UseRemoteConfiguration = true };
        public static MultipleSourceStreamConfiguration TestEnvironment = new MultipleSourceStreamConfiguration() { ShouldDownload = true, ShouldPublish = false, UseRemoteConfiguration = false };
    }

    /// <summary>
    ///  MultipleSourceStreamProvider uses a 'local' and a 'remote' source.
    ///  It uses data in the local source first, if available, and writes everything locally.
    ///  It can be configured to open remote streams directly or download them and open the local copy.
    ///  It can be configured to publish new tables to the remote source or just keep them locally.
    ///  MultipleSourceStreamProviders can be chained together to have multiple remote sources with different publish rules.
    ///  
    ///  A local 'branch' of a database doesn't need to download and shouldn't publish.
    ///  A production node with a remote file share should both download and publish.
    ///  A continuous integration test node with a remote share should download but not publish.
    /// </summary>
    public class MultipleSourceStreamProvider : IStreamProvider
    {
        private IStreamProvider LocalProvider { get; set; }
        private IStreamProvider RemoteProvider { get; set; }
        private MultipleSourceStreamConfiguration Configuration { get; set; }

        public MultipleSourceStreamProvider(IStreamProvider localProvider, IStreamProvider remoteProvider, MultipleSourceStreamConfiguration configuration)
        {
            this.LocalProvider = localProvider;
            this.RemoteProvider = remoteProvider;
            this.Configuration = configuration;
        }

        public string Description => $"{RemoteProvider.Description} => {LocalProvider.Description}";

        public StreamAttributes Attributes(string logicalPath)
        {
            // Use the local file first, if found
            StreamAttributes attributes = LocalProvider.Attributes(logicalPath);

            // Return remote metadata instead if the local doesn't exist and the remote does
            if (!attributes.Exists)
            {
                StreamAttributes remoteAttributes = RemoteProvider.Attributes(logicalPath);
                if (remoteAttributes.Exists) attributes = remoteAttributes;
            }

            return attributes;
        }

        public void Delete(string logicalPath)
        {
            Trace.WriteLine($"DELETE: {logicalPath}");
            LocalProvider.Delete(logicalPath);

            if (Configuration.ShouldPublish)
            {
                Trace.Write($"DELETE: {logicalPath} in {RemoteProvider.Description}");
                RemoteProvider.Delete(logicalPath);
            }
        }

        public IEnumerable<StreamAttributes> Enumerate(string underLogicalPath, EnumerateTypes types, bool recursive)
        {
            // Track item paths we've already returned
            HashSet<string> pathsSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Return all local items first
            foreach (StreamAttributes item in LocalProvider.Enumerate(underLogicalPath, types, recursive))
            {
                pathsSeen.Add(item.Path);
                yield return item;
            }

            // Return remote items which we didn't already return seeing locally
            foreach (StreamAttributes remoteItem in RemoteProvider.Enumerate(underLogicalPath, types, recursive))
            {
                if (pathsSeen.Add(remoteItem.Path)) yield return remoteItem;
            }
        }

        public Stream OpenAppend(string logicalPath)
        {
            // Always write in the local location
            return LocalProvider.OpenWrite(logicalPath);
        }

        public Stream OpenRead(string logicalPath)
        {
            // If configured to only use remote configuration, don't use the local copy for Config and Query
            if (Configuration.UseRemoteConfiguration)
            {
                if (logicalPath.StartsWith("Config\\", StringComparison.OrdinalIgnoreCase) || logicalPath.StartsWith("Query\\", StringComparison.OrdinalIgnoreCase))
                {
                    return RemoteProvider.OpenRead(logicalPath);
                }
            }

            // Read the local copy first, if found
            if (LocalProvider.Attributes(logicalPath).Exists) return LocalProvider.OpenRead(logicalPath);

            // If there's a remote copy, use that
            if (LocalProvider.Attributes(logicalPath).Exists == false && RemoteProvider.Attributes(logicalPath).Exists == true)
            {
                if (Configuration.ShouldDownload)
                {
                    // Download the file if configured to
                    Trace.WriteLine($"DOWNLOAD: {logicalPath}");
                    using (Stream target = LocalProvider.OpenWrite(logicalPath))
                    {
                        using (Stream source = RemoteProvider.OpenRead(logicalPath))
                        {
                            source.CopyTo(target);
                        }
                    }

                    return LocalProvider.OpenRead(logicalPath);
                }

                return RemoteProvider.OpenRead(logicalPath);
            }

            // If neither location had the item, throw
            throw new IOException($"\"{logicalPath}\" did not exist in {LocalProvider.Description} or {RemoteProvider.Description}");
        }

        public Stream OpenWrite(string logicalPath)
        {
            // Always write in the local location
            return LocalProvider.OpenWrite(logicalPath);
        }

        public void Publish(string logicalTablePath)
        {
            if (logicalTablePath.Equals("cout", StringComparison.OrdinalIgnoreCase)) return;

            if (Configuration.ShouldPublish)
            {
                Trace.WriteLine($"PUBLISH: {logicalTablePath}");

                foreach (StreamAttributes item in LocalProvider.Enumerate(logicalTablePath, EnumerateTypes.File, true))
                {
                    using (Stream target = RemoteProvider.OpenWrite(item.Path))
                    {
                        using (Stream source = LocalProvider.OpenRead(item.Path))
                        {
                            source.CopyTo(target);
                        }
                    }
                }

                Trace.WriteLine($"Done with PUBLISH: {logicalTablePath}");
            }
        }

        public ItemVersions ItemVersions(LocationType location, string itemName)
        {
            ItemVersions versions = LocalProvider.ItemVersions(location, itemName);
            versions.Merge(RemoteProvider.ItemVersions(location, itemName));
            return versions;
        }
    }
}

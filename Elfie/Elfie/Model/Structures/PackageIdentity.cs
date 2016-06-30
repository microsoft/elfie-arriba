// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    public class PackageIdentity : IBinarySerializable
    {
        public string PackageName { get; set; }
        public string ReleaseName { get; set; }
        public string ProjectUrl { get; set; }
        public int DownloadCount { get; set; }

        public PackageIdentity(string packageName)
        {
            this.PackageName = packageName;

            // Other Members are optional
        }

        public string IndexFileName
        {
            get
            {
                if (String.IsNullOrEmpty(PackageName)) return null;

                // If only a package name was passed (Solution or Project case), use that
                if (String.IsNullOrEmpty(ReleaseName)) return FileIO.CleanPath(PackageName, 48) + ".idx";

                // Otherwise, include the Package Name and Release Name
                return FileIO.CleanPath(PackageName, 36) + " " + FileIO.CleanPath(ReleaseName, 12) + ".idx";
            }
        }

        /// <summary>
        ///  Returns whether this PackageIdentity matches one being queried for.
        ///  The name, release, and URL must match if the query provided them.
        ///  The download count must be at least the count provided.
        /// </summary>
        /// <param name="queryCriteria">PackageIdentity containing partial or full package details to match</param>
        /// <returns>True if this identity matches all provided criteria, False otherwise</returns>
        public bool Matches(PackageIdentity queryCriteria)
        {
            if (!String.IsNullOrEmpty(queryCriteria.PackageName) && queryCriteria.PackageName != this.PackageName) return false;
            if (!String.IsNullOrEmpty(queryCriteria.ReleaseName) && queryCriteria.ReleaseName != this.ReleaseName) return false;
            if (!String.IsNullOrEmpty(queryCriteria.ProjectUrl) && queryCriteria.ProjectUrl != this.ProjectUrl) return false;
            if (queryCriteria.DownloadCount > 0 && queryCriteria.DownloadCount >= this.DownloadCount) return false;

            return true;
        }

        public void WriteBinary(BinaryWriter w)
        {
            w.Write(this.PackageName ?? String.Empty);
            w.Write(this.ReleaseName ?? String.Empty);
            w.Write(this.ProjectUrl ?? String.Empty);

            // DownloadCount must be last so that it can be seeked to directly
            w.Write(this.DownloadCount);
        }

        public void ReadBinary(BinaryReader r)
        {
            this.PackageName = r.ReadString();
            this.ReleaseName = r.ReadString();
            this.ProjectUrl = r.ReadString();

            // DownloadCount must be last so that it can be seeked to directly
            this.DownloadCount = r.ReadInt32();
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.CodeAnalysis.Elfie.PDB
{
    /// <summary>
    ///  SourceFileDetails maps the original source file path in a PDB to the download and cache locations.
    /// </summary>
    public class SourceFileDetails
    {
        /// <summary>
        ///  Original Path to the source code file as encoded into the PDB.
        /// </summary>
        public string PathInPdb { get; set; }

        /// <summary>
        ///  Url from which the source code file can be downloaded.
        /// </summary>
        public string SourceUrl { get; set; }

        /// <summary>
        ///  Path under the PDB cache path [ex: C:\SymbolStore\[PdbFileName]\[DllSignature]\src]
        ///  where the source file should be cached when downloaded.
        /// </summary>
        public string CachedPathSuffix { get; set; }

        public SourceFileDetails(string pathInPdb, string sourceUrl)
        {
            this.PathInPdb = pathInPdb;
            this.SourceUrl = sourceUrl;
            this.CachedPathSuffix = SourceFileMap.CachePathSuffix(SourceUrl);
        }

        public override string ToString()
        {
            return String.Format("{0}\t{1}\t{2}", this.PathInPdb, this.SourceUrl, this.CachedPathSuffix);
        }
    }
}

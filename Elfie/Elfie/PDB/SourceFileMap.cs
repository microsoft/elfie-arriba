// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model;

namespace Microsoft.CodeAnalysis.Elfie.PDB
{
    /// <summary>
    ///  SourceFileMap contains the mapping of all PDB source file paths to download and cache paths,
    ///  and has the logic to build, save, and reload the map.
    ///  
    ///  The cache location is always in a single folder, with the first eight characters of the hex string
    ///  of the SHA-256 hash of the UTF8 form of the source url. This location is used because it can
    ///  be predicted from the source URL only and avoids path length limits for long source URLs.
    /// </summary>
    public class SourceFileMap
    {
        private const string FileMapFileExtension = ".map";

        private const string SrcToolExe = @"DebuggingTools\srctool.exe";
        private const string SrcToolFormatString = "-n \"{0}\"";
        private const string SrcToolParseRegex = @"\[(?<pathInPdb>[^\]]+)\] trg: (?<sourceUrl>[^\r\n]+)";

        public string PdbPath { get; private set; }
        public IList<SourceFileDetails> Files { get; private set; }

        private SourceFileMap(string pdbPath)
        {
            this.PdbPath = pdbPath;
            this.Files = new List<SourceFileDetails>();
        }

        /// <summary>
        ///  Return the SourceUrl for a given PDB path, if found. If not,
        ///  return the original path.
        /// </summary>
        /// <param name="pathInPdb">Source File Path in original PDB</param>
        /// <returns>Source URL for file, if available, or passed path otherwise</returns>
        public string this[string pathInPdb]
        {
            get
            {
                foreach (SourceFileDetails details in this.Files)
                {
                    if (String.Equals(pathInPdb, details.PathInPdb, StringComparison.OrdinalIgnoreCase))
                    {
                        return details.SourceUrl;
                    }
                }

                return pathInPdb;
            }
        }

        /// <summary>
        ///  Return the SourceFileDetails (with all paths) for a given source
        ///  file path [either the original path in the PDB or the Source URL]
        /// </summary>
        /// <param name="pathInPdbOrSourceUrl">Path in PDB or Source URL for file</param>
        /// <returns>SourceFileDetails for file, null if not found</returns>
        public SourceFileDetails Details(string pathInPdbOrSourceUrl)
        {
            foreach (SourceFileDetails details in this.Files)
            {
                if (String.Equals(pathInPdbOrSourceUrl, details.PathInPdb, StringComparison.OrdinalIgnoreCase)
                    || String.Equals(pathInPdbOrSourceUrl, details.SourceUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return details;
                }
            }

            return null;
        }


        /// <summary>
        ///  Create or Load a SourceFileMap for the given PDB. When created, the map is
        ///  serialized out for quick reloading later.
        /// </summary>
        /// <param name="pdbPath">Path to PDB to map</param>
        /// <returns>SourceFileMap for PDB</returns>
        public static SourceFileMap BuildOrLoad(string pdbPath)
        {
            SourceFileMap map = new SourceFileMap(pdbPath);

            if (File.Exists(map.FileMapPath))
            {
                map.Load();
            }
            else
            {
                map.Build();
                map.Save();
            }

            return map;
        }

        /// <summary>
        ///  Load the SourceFileMap for a given PDB. If no map exists, null is returned.
        /// </summary>
        /// <param name="pdbPath">Path to PDB to map</param>
        /// <returns>SourceFileMap for PDB, or null if not previously mapped.</returns>
        public static SourceFileMap Load(string pdbPath)
        {
            SourceFileMap map = new SourceFileMap(pdbPath);

            if (File.Exists(map.FileMapPath))
            {
                map.Load();
                return map;
            }

            return null;
        }

        private void Build()
        {
            // Run SrcTool to extract the path mappings
            string srcToolMapFilePath = Path.Combine(Path.GetDirectoryName(this.PdbPath), "SrcToolMap.txt");
            Cmd.Quiet(SrcToolExe + " " + String.Format(SrcToolFormatString, this.PdbPath), TimeSpan.FromSeconds(15), srcToolMapFilePath);

            // Regex parse output "[%pdbPath%] trg: %sourceUrl%" and build our own map [including file name SHA 256 hash]
            Regex r = new Regex(SrcToolParseRegex);
            foreach (Match m in r.Matches(File.ReadAllText(srcToolMapFilePath)))
            {
                this.Files.Add(new SourceFileDetails(m.Groups["pathInPdb"].Value, m.Groups["sourceUrl"].Value));
            }
        }

        private void Load()
        {
            foreach (string line in File.ReadAllLines(this.FileMapPath))
            {
                string[] parts = line.Split('\t');
                this.Files.Add(new SourceFileDetails(parts[0], parts[1]));
            }
        }

        public void Save(string filePath = null)
        {
            if (filePath == null) filePath = this.FileMapPath;
            File.WriteAllLines(filePath, this.Files.Select((sfd) => sfd.ToString()));
        }

        private string FileMapPath
        {
            get { return Path.ChangeExtension(this.PdbPath, FileMapFileExtension); }
        }

        public static string CachePathSuffix(string sourceFileUrl)
        {
            return Path.Combine(sourceFileUrl.ToSHA256String().Substring(0, 8), Path.GetFileName(sourceFileUrl));
        }

        public static string ComputeCachedPath(string binaryName, string sourceFileUrl)
        {
            return Path.Combine(SymbolCache.Path, Path.GetFileNameWithoutExtension(binaryName) + ".pdb", "src", CachePathSuffix(sourceFileUrl));
        }

        public static async Task Download(string pdbPath, string sourceFileUrl, TimeSpan timeout)
        {
            string localCachePath = ComputeCachedPath(pdbPath, sourceFileUrl);
            if (File.Exists(localCachePath)) return;

            WebRequest request = WebRequest.CreateHttp(sourceFileUrl);
            request.Timeout = (int)timeout.TotalMilliseconds;

            using (WebResponse response = await request.GetResponseAsync())
            {
                string localCacheDirectory = Path.GetDirectoryName(localCachePath);
                if (!String.IsNullOrEmpty(localCacheDirectory)) Directory.CreateDirectory(localCacheDirectory);

                using (FileStream localFile = new FileStream(localCachePath, FileMode.Create))
                {
                    await response.GetResponseStream().CopyToAsync(localFile);
                }
            }
        }

        public static string GetLocalPath(Symbol match)
        {
            string filePath = match.FilePath.ToString();
            if (String.IsNullOrEmpty(filePath)) return filePath;

            if (filePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                string cachedPath = ComputeCachedPath(match.AssemblyNameWithoutExtension.ToString() + ".pdb", filePath);

                if (!File.Exists(cachedPath))
                {
                    Trace.WriteLine("Downloading: {0}", filePath);
                    Download(match.AssemblyNameWithoutExtension.ToString() + ".pdb", filePath, TimeSpan.FromSeconds(10)).Wait();
                }

                filePath = cachedPath;
            }

            return filePath;
        }
    }
}

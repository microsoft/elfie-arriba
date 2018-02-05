// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XForm.Generator.Mappers
{
    public class UriStemMapper
    {
        private static string[] s_topTen = new string[] { "Index.htm", "Index.css", "bundle.js", "images/logo.png", "images/splash.png", "ads/provider.js", "Grid.htm", "Grid.js", "Grid.css", "config.js" };
        private static string[] s_fileTypes = new string[] { ".htm", ".css", ".js", ".png" };

        private PhraseMapper PhraseMapper { get; set; }

        public UriStemMapper()
        {
            this.PhraseMapper = new PhraseMapper();
        }

        public string Generate(uint hash)
        {
            // 90% the top ten URLs
            int topTenIndex = Hashing.Extract(ref hash, s_topTen.Length + 1);
            if (topTenIndex < s_topTen.Length)
            {
                return s_topTen[topTenIndex];
            }

            // In a folder half of the time
            bool hasFolder = Hashing.Extract(ref hash, 2) == 1;
            if (hasFolder)
            {
                // There are eight unique folders, 16 file names, and four extensions = 2^15 = 512 unique values total
                return $"{PhraseMapper.Generate((uint)Hashing.Extract(ref hash, 8))}/{PhraseMapper.Generate((uint)Hashing.Extract(ref hash, 16))}{s_fileTypes[Hashing.Extract(ref hash, s_fileTypes.Length)]}";
            }
            else
            {
                // There are 64 file names and 4 extensions = 256 unique values
                return $"{PhraseMapper.Generate((uint)Hashing.Extract(ref hash, 64) + 16)}{s_fileTypes[Hashing.Extract(ref hash, s_fileTypes.Length)]}";
            }
        }
    }
}

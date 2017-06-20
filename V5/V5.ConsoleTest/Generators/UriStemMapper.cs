using Xsv.Sanitize;

namespace V5.ConsoleTest.Generators
{
    public class UriStemMapper
    {
        private static string[] TopTen = new string[] { "Index.htm", "Index.css", "bundle.js", "images/logo.png", "images/splash.png", "ads/provider.js", "Grid.htm", "Grid.js", "Grid.css", "config.js" };
        private static string[] FileTypes = new string[] { ".htm", ".css", ".js", ".png" };

        private PhraseMapper PhraseMapper { get; set; }

        public UriStemMapper()
        {
            this.PhraseMapper = new PhraseMapper();
        }

        public string Generate(uint hash)
        {
            // 90% the top ten URLs
            int topTenIndex = Hashing.Extract(ref hash, TopTen.Length + 1);
            if(topTenIndex < TopTen.Length)
            {
                return TopTen[topTenIndex];
            }

            // In a folder half of the time
            bool hasFolder = Hashing.Extract(ref hash, 2) == 1;
            if (hasFolder)
            {
                // There are eight unique folders, 16 file names, and four extensions = 2^15 = 512 unique values total
                return $"{PhraseMapper.Generate((uint)Hashing.Extract(ref hash, 8))}/{PhraseMapper.Generate((uint)Hashing.Extract(ref hash, 16))}{FileTypes[Hashing.Extract(ref hash, FileTypes.Length)]}";
            }
            else
            {
                // There are 64 file names and 4 extensions = 256 unique values
                return $"{PhraseMapper.Generate((uint)Hashing.Extract(ref hash, 64) + 16)}{FileTypes[Hashing.Extract(ref hash, FileTypes.Length)]}";
            }

        }
    }
}

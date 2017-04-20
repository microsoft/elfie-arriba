using System;
using System.Text;

namespace Xsv.Sanitize
{
    /// <summary>
    ///  AliasMapper maps hashes to aliases appropriate for people or groups.
    ///  [Up to 8 letters, dashed prefix sometimes]
    /// </summary>
    public class AliasMapper : ISanitizeMapper
    {
        public AliasMapper()
        { }

        public string Generate(uint hash)
        {
            StringBuilder result = new StringBuilder();
            uint hashRemaining = hash;

            while(hashRemaining > 0)
            {
                result.Append((char)('A' + Uint.Extract(ref hashRemaining, 26)));
            }

            return result.ToString();
        }
    }
}

using System.Text;

namespace Xsv.Sanitize
{
    /// <summary>
    ///  IntMapper maps hashes to integers (trivially).
    /// </summary>
    public class IntMapper : ISanitizeMapper
    {
        public string Generate(uint hash)
        {
            return hash.ToString();
        }
    }
}

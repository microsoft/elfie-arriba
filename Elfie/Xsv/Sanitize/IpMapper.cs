using System.Text;

namespace Xsv.Sanitize
{
    /// <summary>
    ///  IpMapper maps hashes into IPv4 addresses.
    /// </summary>
    public class IpMapper : ISanitizeMapper
    {
        public string Generate(ISanitizeContext context)
        {
            StringBuilder result = new StringBuilder();
            uint hashRemaining = context.Hash;

            for (int i = 0; i < 4; ++i)
            {
                if (i > 0) result.Append(".");
                result.Append(Uint.Extract(ref hashRemaining, 256));
            }

            return result.ToString();
        }
    }
}

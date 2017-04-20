using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Xsv.Sanitize
{
    public static class Uint
    {
        public static int Extract(ref uint value, int optionsLength)
        {
            int result = (int)(value % optionsLength);
            value = value / (uint)optionsLength;
            return result;
        }
    }
}

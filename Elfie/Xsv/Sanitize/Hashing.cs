using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System.Security.Cryptography;

namespace Xsv.Sanitize
{
    public static class Hashing
    {
        private static HashAlgorithm hasher;
        private static byte[] buffer;

        /// <summary>
        ///  Extract a value in the range [0, optionsLength) from the given
        ///  hash. Used to turn the hash into mapped values using separate
        ///  portions of the hash for each one.
        /// </summary>
        /// <param name="value">Hash Value to extract value from</param>
        /// <param name="optionsLength">Upper bound of value to return</param>
        /// <returns>Value from [0, optionsLength) to use for mapping</returns>
        public static int Extract(ref uint value, int optionsLength)
        {
            int result = (int)(value % optionsLength);
            value = value / (uint)optionsLength;
            return result;
        }

        /// <summary>
        ///  Compute a cryptographic hash of 'value' and 'hashKeyHash' together.
        ///  Used to map values in the Sanitizer.
        /// </summary>
        /// <param name="value">Source value to hash</param>
        /// <param name="hashKeyHash">HashKey for this hash</param>
        /// <returns>uint of hash result</returns>
        public static uint Hash(String8 value, uint hashKeyHash)
        {
            if (hasher == null) hasher = SHA256Managed.Create();
            if (buffer == null || buffer.Length < value.Length + 4) buffer = new byte[value.Length + 4];

            buffer[0] = (byte)(hashKeyHash & 0xFF);
            buffer[1] = (byte)((hashKeyHash >> 8) & 0xFF);
            buffer[2] = (byte)((hashKeyHash >> 16) & 0xFF);
            buffer[3] = (byte)((hashKeyHash >> 24) & 0xFF);
            value.WriteTo(buffer, 4);

            byte[] hash = hasher.ComputeHash(buffer, 0, value.Length + 4);
            uint result = (uint)((hash[0] << 24) + (hash[1] << 16) + (hash[2] << 8) + hash[3]);
            return result;
        }
    }
}

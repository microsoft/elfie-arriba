using System;

namespace Xsv.Sanitize
{
    /// <summary>
    ///  GuidMapper maps hashes to GUIDs (trivially).
    /// </summary>
    public class GuidMapper : ISanitizeMapper
    {
        public string Generate(uint hash)
        {
            Guid result = new Guid(
                hash,
                (ushort)(hash >> 16),
                (ushort)(hash & ushort.MaxValue),
                (byte)(hash & byte.MaxValue),
                (byte)((hash >> 8) & byte.MaxValue),
                (byte)((hash >> 16) & byte.MaxValue),
                (byte)((hash >> 24) & byte.MaxValue),
                (byte)(hash & byte.MaxValue),
                (byte)((hash >> 8) & byte.MaxValue),
                (byte)((hash >> 16) & byte.MaxValue),
                (byte)((hash >> 24) & byte.MaxValue));
            return result.ToString("D");
        }
    }
}

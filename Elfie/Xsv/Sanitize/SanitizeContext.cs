using System;

namespace Xsv.Sanitize
{
    /// <summary>
    ///  SanitizeContext is passed to sanitizers and contains all state they
    ///  need to generate values.
    /// </summary>
    public interface ISanitizeContext
    {
        /// <summary>
        ///  Hash for which to generate a value.
        ///  Each hash must produce a unique value.
        /// </summary>
        uint Hash { get; }

        /// <summary>
        ///  Arguments passed to mapper, if any.
        /// </summary>
        string[] Args { get; }

        /// <summary>
        ///  ISanitizerProvider, used to get other mappers if needed.
        /// </summary>
        ISanitizerProvider Provider { get; }
    }

    public class SanitizeContext : ISanitizeContext
    {
        public string[] Args { get; set; }
        public uint Hash { get; set; }
        public ISanitizerProvider Provider { get; set; }
    }
}

namespace Xsv.Sanitize
{
    /// <summary>
    ///  ISanitizeMappers are classes which can generate sanitized values of
    ///  a particular type (ComputerName, IP, Phrase) given a uint hash.
    /// </summary>
    public interface ISanitizeMapper
    {
        /// <summary>
        ///  Generate a sanitized value for a given hash.
        ///  Value must be unique for every distinct hash value.
        /// </summary>
        /// <param name="context">Context from which to generate result.</param>
        /// <returns>Properly typed output unique to hash</returns>
        string Generate(ISanitizeContext context);
    }
}

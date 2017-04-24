using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Xsv.Sanitize
{
    public interface IColumnHandler
    {
        String8 Sanitize(String8 value);
    }

    /// <summary>
    ///  KeepColumnHandler passes values through unchanged.
    /// </summary>
    public class KeepColumnHandler : IColumnHandler
    {
        public String8 Sanitize(String8 value)
        {
            // Write the value as-is
            return value;
        }
    }

    /// <summary>
    ///  MapColumnHandler maps values with the configured mapper.
    /// </summary>
    public class MapColumnHandler : IColumnHandler
    {
        private uint HashKeyHash { get; set; }
        private ISanitizeMapper Mapper { get; set; }
        private String8Block Block { get; set; }

        public MapColumnHandler(uint hashKeyHash, ISanitizeMapper mapper)
        {
            this.HashKeyHash = hashKeyHash;
            this.Mapper = mapper;
            this.Block = new String8Block();
        }

        public String8 Sanitize(String8 value)
        {
            this.Block.Clear();
            uint hash = Hashing.Hash(value, this.HashKeyHash);
            return this.Block.GetCopy(this.Mapper.Generate(hash));
        }
    }

    /// <summary>
    ///  EchoColumnHandler handles echoing specific values unchanged.
    /// </summary>
    public class EchoColumnHandler : IColumnHandler
    {
        private HashSet<String8> EchoValues { get; set; }
        private IColumnHandler Inner { get; set; }

        public EchoColumnHandler(HashSet<String8> echoValues, IColumnHandler inner)
        {
            this.EchoValues = echoValues;
            this.Inner = inner;
        }

        public String8 Sanitize(String8 value)
        {
            if (this.EchoValues.Contains(value)) return value;
            return this.Inner.Sanitize(value);
        }
    }

    /// <summary>
    ///  RegexColumnHandler runs a Regex and maps each matching part.
    /// </summary>
    public class RegexColumnHandler : IColumnHandler
    {
        private Regex Regex { get; set; }
        private MapColumnHandler Inner { get; set; }

        private String8Block Block { get; set; }

        public RegexColumnHandler(string regex, MapColumnHandler inner)
        {
            // Pre-Compile the Regex
            this.Regex = new Regex(regex, RegexOptions.Compiled);
            this.Inner = inner;

            this.Block = new String8Block();
        }

        public String8 Sanitize(String8 value8)
        {
            this.Block.Clear();
            StringBuilder result = new StringBuilder();

            string value = value8.ToString();
            int nextIndexToWrite = 0;

            foreach (Match m in this.Regex.Matches(value))
            {
                // Replace the whole expression if no groups, otherwise the first parenthesized group
                Group g = m.Groups[0];
                if (m.Groups.Count > 1) g = m.Groups[1];

                // Write content before this match
                result.Append(value.Substring(nextIndexToWrite, g.Index - nextIndexToWrite));

                // Convert and write the match
                String8 part = this.Inner.Sanitize(this.Block.GetCopy(g.Value));
                result.Append(part.ToString());

                // Set the next non-match we need to write
                nextIndexToWrite = g.Index + g.Length;
            }

            // Write anything after the last match
            if(nextIndexToWrite < value.Length)
            {
                result.Append(value.Substring(nextIndexToWrite));
            }

            return this.Block.GetCopy(result.ToString());
        }
    }
}

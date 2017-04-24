using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

namespace Xsv.Sanitize
{
    public interface IColumnHandler
    {
        void Sanitize(ITabularValue value, ITabularWriter writer);
    }

    /// <summary>
    ///  KeepColumnHandler passes values through unchanged.
    /// </summary>
    public class KeepColumnHandler : IColumnHandler
    {
        public void Sanitize(ITabularValue value, ITabularWriter writer)
        {
            // Write the value as-is
            writer.Write(value.ToString8());
        }
    }

    /// <summary>
    ///  DropColumnHandler excludes a column from the output.
    /// </summary>
    public class DropColumnHandler : IColumnHandler
    {
        public void Sanitize(ITabularValue value, ITabularWriter writer)
        {
            // Don't output the column at all
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

        public string Sanitize(ITabularValue value)
        {
            uint hash = Hashing.Hash(value.ToString8(), this.HashKeyHash);
            return this.Mapper.Generate(hash);
        }

        public void Sanitize(ITabularValue value, ITabularWriter writer)
        {
            string sanitized = Sanitize(value);
            writer.Write(this.Block.GetCopy(sanitized));

            // Clear the String8Block space to reuse
            this.Block.Clear();
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

        public void Sanitize(ITabularValue value, ITabularWriter writer)
        {
            String8 value8 = value.ToString8();
            if (this.EchoValues.Contains(value8))
            {
                writer.Write(value8);
            }
            else
            {
                this.Inner.Sanitize(value, writer);
            }
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
        private ObjectTabularValue ValueWrapper { get; set; }

        public RegexColumnHandler(string regex, MapColumnHandler inner)
        {
            // Pre-Compile the Regex
            this.Regex = new Regex(regex, RegexOptions.Compiled);
            this.Inner = inner;

            this.Block = new String8Block();
            this.ValueWrapper = new ObjectTabularValue(this.Block);
        }

        public void Sanitize(ITabularValue valueTV, ITabularWriter writer)
        {
            writer.WriteValueStart();

            string value = valueTV.ToString();
            int nextIndexToWrite = 0;

            foreach (Match m in this.Regex.Matches(value))
            {
                // Replace the whole expression if no groups, otherwise the first parenthesized group
                Group g = m.Groups[0];
                if (m.Groups.Count > 1) g = m.Groups[1];

                // Write content before this match
                writer.WriteValuePart(this.Block.GetCopy(value.Substring(nextIndexToWrite, g.Index - nextIndexToWrite)));

                // Convert and write the match
                this.ValueWrapper.SetValue(g.Value);
                writer.WriteValuePart(this.Block.GetCopy(this.Inner.Sanitize(this.ValueWrapper)));

                // Clear the Block (so we don't keep allocating)
                this.Block.Clear();

                // Set the next non-match we need to write
                nextIndexToWrite = g.Index + g.Length;
            }

            // Write anything after the last match
            if(nextIndexToWrite < value.Length)
            {
                writer.WriteValuePart(this.Block.GetCopy(value.Substring(nextIndexToWrite)));
                this.Block.Clear();
            }

            writer.WriteValueEnd();
        }
    }
}

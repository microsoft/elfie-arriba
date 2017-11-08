using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XForm.Aggregators;
using XForm.Data;
using XForm.Extensions;
using XForm.Query;
using XForm.Readers;
using XForm.Transforms;
using XForm.Writers;

namespace XForm
{
    public class PipelineFactory
    {
        public static IDataBatchEnumerator BuildPipeline(string configurationSet)
        {
            IDataBatchEnumerator pipeline = null;

            foreach (string configurationText in configurationSet.Split('\n'))
            {
                pipeline = BuildStage(pipeline, configurationText);
            }

            return pipeline;
        }

        public static IDataBatchEnumerator BuildStage(IDataBatchEnumerator source, string configurationText)
        {
            List<string> configurationParts = SplitConfigurationLine(configurationText);
            string verb = configurationParts[0];

            switch(verb)
            {
                case "read":
                    if (source != null) throw new ArgumentException("'read' must be the first stage in a pipeline.");
                    if (configurationParts.Count < 2) throw new ArgumentException("'read' must be passed a file path.");
                    return new TabularFileReader(configurationParts[1]);
                case "select":
                case "columns":
                    return new ColumnSelector(source, configurationParts.Skip(1));
                case "write":
                    if (configurationParts.Count < 2) throw new ArgumentException("'write' must be passed a file path.");
                    return new TabularFileWriter(source, configurationParts[1]);
                case "limit":
                    if (configurationParts.Count < 2) throw new ArgumentException("'limit' must be passed a row limit.");
                    return new RowLimiter(source, int.Parse(configurationParts[1]));
                case "cast":
                case "convert":
                    if (configurationParts.Count < 3) throw new ArgumentException("'cast' must be passed a column name and type.");
                    return new TypeConverter(source, configurationParts[1], ParseType(configurationParts[2]), (configurationParts.Count > 3 ? configurationParts[2] : null), (configurationParts.Count > 4 ? bool.Parse(configurationParts[3]) : true));
                case "where":
                    if (configurationParts.Count < 4) throw new ArgumentException("'where' must be passed a column, operator, and value.");
                    return new WhereFilter(source, configurationParts[1], ParseCompareOperator(configurationParts[2]), configurationParts[3]);
                case "count":
                    return new CountAggregator(source);
                default:
                    throw new NotImplementedException($"XForm doesn't know how to create a stage for '{verb}'.");
            }
        }

        public static List<string> SplitConfigurationLine(string configurationText)
        {
            List<string> parts = new List<string>();

            int index = 0;
            while(true)
            {
                string part = ParseConfigurationPart(configurationText, ref index);
                if (part == null) break;
                parts.Add(part);
            }

            return parts;
        }

        public static string ParseConfigurationPart(string configurationText, ref int index)
        {
            // Ignore whitespace before the value
            while (index < configurationText.Length && Char.IsWhiteSpace(configurationText[index])) index++;

            // If this is the end of the text, return null
            if (index == configurationText.Length) return null;

            if(configurationText[index] == '"')
            {
                // Quoted value. Read until an end quote, treating "" as an escaped quote.
                StringBuilder value = new StringBuilder();

                index++;
                while (index < configurationText.Length)
                {
                    int nextQuote = configurationText.IndexOf('"', index);
                    if (nextQuote == -1) break;

                    if (configurationText.Length > (nextQuote + 1) && configurationText[nextQuote + 1] == '"')
                    {
                        // Escaped Quote - append the value so far including one quote and keep searching for the end
                        value.Append(configurationText, index, nextQuote - index + 1);
                        index = nextQuote + 2;
                    }
                    else
                    {
                        // Closing Quote. Append the value without the quote and return it
                        value.Append(configurationText, index, nextQuote - index);
                        index = nextQuote + 1;
                        return value.ToString();
                    }
                }

                throw new ArgumentException($"Unclosed Quote in query line: \"{configurationText}\"");
            }
            else
            {
                // Unquoted value. Return value until next whitespace or end of string
                int start = index;
                while(index < configurationText.Length && !Char.IsWhiteSpace(configurationText[index])) index++;
                return configurationText.Substring(start, index - start);
            }
        }

        public static Type ParseType(string typeString)
        {
            typeString = typeString.ToLowerInvariant();

            switch (typeString)
            {
                case "int":
                    return typeof(int);
                case "bool":
                    return typeof(bool);
                case "datetime":
                    return typeof(DateTime);
                case "string8":
                    return typeof(String8);
                default:
                    throw new NotImplementedException($"XForm doesn't know type \"{typeString}\".");
            }
        }

        public static CompareOperator ParseCompareOperator(string op)
        {
            switch(op)
            {
                case "<":
                    return CompareOperator.LessThan;
                case "<=":
                    return CompareOperator.LessThanOrEqual;
                case ">":
                    return CompareOperator.GreaterThan;
                case ">=":
                    return CompareOperator.GreaterThanOrEqual;
                case "=":
                case "==":
                    return CompareOperator.Equals;
                case "!=":
                case "<>":
                    return CompareOperator.NotEquals;
                default:
                    throw new NotImplementedException($"XForm doesn't know CompareOperator \"{op}\".");
            }
        }
    }
}

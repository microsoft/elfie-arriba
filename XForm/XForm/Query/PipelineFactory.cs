using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XForm.Aggregators;
using XForm.Data;
using XForm.Query;
using XForm.IO;
using XForm.Transforms;
using XForm.Types;

namespace XForm
{
    public class PipelineFactory
    {
        public static IDataBatchEnumerator BuildPipeline(string xqlQuery, IDataBatchEnumerator pipeline = null)
        {
            foreach (string xqlLine in xqlQuery.Split('\n'))
            {
                if (string.IsNullOrEmpty(xqlLine.Trim())) continue;
                pipeline = BuildStage(xqlLine, pipeline);
            }

            return pipeline;
        }

        public static IDataBatchEnumerator BuildStage(string xqlLine, IDataBatchEnumerator source)
        {
            List<string> configurationParts = SplitConfigurationLine(xqlLine);
            string verb = configurationParts[0].ToLowerInvariant();

            switch(verb)
            {
                case "read":
                    if (source != null) throw new ArgumentException("'read' must be the first stage in a pipeline.");
                    if (configurationParts.Count != 2) throw new ArgumentException("Usage: 'read' [filePath]");
                    if (configurationParts[1].EndsWith("xform"))
                    {
                        return new BinaryTableReader(configurationParts[1]);
                    }
                    else
                    {
                        return new TabularFileReader(configurationParts[1]);
                    }
                case "schema":
                    if (configurationParts.Count != 1) throw new ArgumentException("Usage: 'schema'");
                    return new SchemaTransformer(source);
                case "select":
                case "columns":
                    return new ColumnSelector(source, configurationParts.Skip(1));
                case "removecolumns":
                    return new ColumnRemover(source, configurationParts.Skip(1));
                case "write":
                    if (configurationParts.Count != 2) throw new ArgumentException("Usage 'write' [filePath]");
                    if (configurationParts[1].EndsWith("xform"))
                    {
                        return new BinaryTableWriter(source, configurationParts[1]);
                    }
                    else
                    {
                        return new TabularFileWriter(source, configurationParts[1]);
                    }
                case "limit":
                    if (configurationParts.Count != 2) throw new ArgumentException("Usage: 'limit' [rowCount]");
                    return new RowLimiter(source, int.Parse(configurationParts[1]));
                case "cast":
                case "convert":
                    if (configurationParts.Count < 3 || configurationParts.Count > 5) throw new ArgumentException("Usage: 'cast' [columnName] [targetType] [default?] [strict?]");
                    return new TypeConverter(source, configurationParts[1], TypeProviderFactory.Get(configurationParts[2]).Type, (configurationParts.Count > 3 ? configurationParts[2] : null), (configurationParts.Count > 4 ? bool.Parse(configurationParts[3]) : true));
                case "where":
                    if (configurationParts.Count != 4) throw new ArgumentException("Usage: 'where' [columnName] [operator] [value]");
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
            while (index < configurationText.Length && (Char.IsWhiteSpace(configurationText[index]) || configurationText[index] == ',')) index++;

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
                while(index < configurationText.Length && !(Char.IsWhiteSpace(configurationText[index]) || configurationText[index] == ',')) index++;
                return configurationText.Substring(start, index - start);
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

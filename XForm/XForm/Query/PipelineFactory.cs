// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using XForm.Data;
using XForm.Query;

namespace XForm
{
    public class PipelineFactory
    {
        private static Dictionary<string, IPipelineStageBuilder> s_pipelineStageBuildersByName;

        private static void EnsureLoaded()
        {
            if(s_pipelineStageBuildersByName != null) return;
            s_pipelineStageBuildersByName = new Dictionary<string, IPipelineStageBuilder>(StringComparer.OrdinalIgnoreCase);

            Add(new ReadCommandBuilder());
            Add(new SchemaCommandBuilder());
            Add(new ColumnsCommandBuilder());
            Add(new RemoveColumnsCommandBuilder());
            Add(new WriterCommandBuilder());
            Add(new LimitCommandBuilder());
            Add(new CountCommandBuilder());
            Add(new TypeConverterCommandBuilder());
            Add(new WhereCommandBuilder());
            Add(new MemoryCacheBuilder());
        }

        private static void Add(IPipelineStageBuilder builder)
        {
            foreach(string verb in builder.Verbs)
            {
                s_pipelineStageBuildersByName[verb] = builder;
            }
        }

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
            EnsureLoaded();

            List<string> configurationParts = SplitConfigurationLine(xqlLine);
            string verb = configurationParts[0].ToLowerInvariant();

            IPipelineStageBuilder builder;
            if(!s_pipelineStageBuildersByName.TryGetValue(verb, out builder))
            {
                throw new NotImplementedException($"XForm doesn't know how to create a stage for '{verb}'.");
            }

            return builder.Build(source, configurationParts);
        }

        public static List<string> SplitConfigurationLine(string configurationText)
        {
            List<string> parts = new List<string>();

            int index = 0;
            while (true)
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

            if (configurationText[index] == '"')
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
                while (index < configurationText.Length && !(Char.IsWhiteSpace(configurationText[index]) || configurationText[index] == ',')) index++;
                return configurationText.Substring(start, index - start);
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using XForm.Data;
using XForm.Extensions;
using XForm.Types;

namespace XForm.Query
{
    public class PipelineScanner
    {
        private List<string> _queryLines;
        private List<string> _currentLineParts;
        private int _currentLineIndex;
        private int _currentPartIndex;

        public PipelineScanner(string xqlQuery)
        {
            _queryLines = new List<string>();

            foreach (string queryLine in xqlQuery.Split('\n'))
            {
                string cleanedLine = queryLine.TrimEnd('\r').Trim();
                if (!String.IsNullOrEmpty(cleanedLine)) _queryLines.Add(cleanedLine);
            }

            _currentLineIndex = -1;
        }

        public string CurrentPart => _currentLineParts[_currentPartIndex];
        public string CurrentLine => _queryLines[_currentLineIndex];
        public bool IsLastPart => (_currentLineParts == null || _currentPartIndex >= _currentLineParts.Count - 1);

        public bool NextLine()
        {
            _currentLineIndex++;
            if (_currentLineIndex >= _queryLines.Count) return false;

            _currentLineParts = SplitLine(_queryLines[_currentLineIndex]);
            _currentPartIndex = -1;

            return true;
        }

        public void NextPart()
        {
            _currentPartIndex++;
            if (_currentPartIndex >= _currentLineParts.Count) throw new ArgumentException("No more arguments in query line.");
        }

        private static List<string> SplitLine(string xqlQueryLine)
        {
            List<string> parts = new List<string>();

            int index = 0;
            while (true)
            {
                string part = SplitLinePart(xqlQueryLine, ref index);
                if (part == null) break;
                parts.Add(part);
            }

            return parts;
        }

        private static string SplitLinePart(string configurationText, ref int index)
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

    public class PipelineParser
    {
        private PipelineScanner _scanner;
        private IPipelineStageBuilder _currentLineBuilder;

        private static Dictionary<string, IPipelineStageBuilder> s_pipelineStageBuildersByName;

        private PipelineParser(string xqlQuery)
        {
            EnsureLoaded();
            _scanner = new PipelineScanner(xqlQuery);
        }

        private static void EnsureLoaded()
        {
            if (s_pipelineStageBuildersByName != null) return;
            s_pipelineStageBuildersByName = new Dictionary<string, IPipelineStageBuilder>(StringComparer.OrdinalIgnoreCase);

            foreach (IPipelineStageBuilder builder in InterfaceLoader.BuildAll<IPipelineStageBuilder>())
            {
                Add(builder);
            }
        }

        private static void Add(IPipelineStageBuilder builder)
        {
            foreach (string verb in builder.Verbs)
            {
                s_pipelineStageBuildersByName[verb] = builder;
            }
        }

        public static IDataBatchEnumerator BuildPipeline(string xqlQuery, IDataBatchEnumerator source = null)
        {
            PipelineParser parser = new PipelineParser(xqlQuery);
            return parser.NextPipeline(source);
        }

        public static IDataBatchEnumerator BuildStage(string xqlQueryLine, IDataBatchEnumerator source)
        {
            PipelineParser parser = new PipelineParser(xqlQueryLine);
            parser._scanner.NextLine();
            return parser.NextStage(source);
        }

        public IDataBatchEnumerator NextPipeline(IDataBatchEnumerator source)
        {
            IDataBatchEnumerator pipeline = source;

            while (_scanner.NextLine())
            {
                pipeline = NextStage(pipeline);
            }

            return pipeline;
        }

        public IDataBatchEnumerator NextStage(IDataBatchEnumerator source)
        {
            NextOrThrow();
            if (!s_pipelineStageBuildersByName.TryGetValue(_scanner.CurrentPart, out _currentLineBuilder)) Throw($"was not a known verb.\r\nVerbs:\r\n{string.Join("\r\n", s_pipelineStageBuildersByName.Keys)}");
            IDataBatchEnumerator stage = _currentLineBuilder.Build(source, this);

            if (!_scanner.IsLastPart)
            {
                _scanner.NextPart();
                Throw("was after all expected arguments.");
            }
            return stage;
        }

        public Type NextType()
        {
            NextOrThrow();
            ITypeProvider provider = TypeProviderFactory.TryGet(_scanner.CurrentPart);
            if (provider == null) Throw("was not a supported type.");
            return provider.Type;
        }

        public string NextColumnName(IDataBatchEnumerator currentSource)
        {
            NextOrThrow();
            ColumnDetails current = currentSource.Columns[currentSource.Columns.IndexOfColumn(_scanner.CurrentPart)];
            return current.Name;
        }

        public string NextTableName()
        {
            NextOrThrow();
            return _scanner.CurrentPart;
        }

        public bool NextBoolean()
        {
            NextOrThrow();
            bool value;
            if (!bool.TryParse(_scanner.CurrentPart, out value)) Throw("was not a valid boolean.");
            return value;
        }

        public int NextInteger()
        {
            NextOrThrow();
            int value;
            if (!int.TryParse(_scanner.CurrentPart, out value)) Throw("was not a valid integer.");
            return value;
        }

        public string NextString()
        {
            NextOrThrow();
            return _scanner.CurrentPart;
        }

        public object NextLiteralValue()
        {
            NextOrThrow();
            object value = _scanner.CurrentPart;
            return value;
        }

        public CompareOperator NextCompareOperator()
        {
            NextOrThrow();
            return _scanner.CurrentPart.ParseCompareOperator();
        }

        private void NextOrThrow()
        {
            if (_scanner.IsLastPart) throw new ArgumentException($"Usage: {_currentLineBuilder.Usage}");
            _scanner.NextPart();
        }

        private void Throw(string badArgumentMessage)
        {
            StringBuilder message = new StringBuilder();
            if (_currentLineBuilder != null) message.AppendLine(_currentLineBuilder.Usage);
            message.AppendLine($"\"{_scanner.CurrentPart}\" {badArgumentMessage}");

            throw new ArgumentException(message.ToString());
        }

        public bool IsLastLinePart => _scanner.IsLastPart;
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

using XForm.Data;
using XForm.Extensions;
using XForm.IO;
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
            NextLine();
        }

        public bool HasCurrentLine => _currentLineIndex < _queryLines.Count;
        public string CurrentLine => (HasCurrentLine ? _queryLines[_currentLineIndex] : null);

        public bool HasCurrentPart => (HasCurrentLine && _currentPartIndex < _currentLineParts.Count);
        public string CurrentPart => (HasCurrentPart ? _currentLineParts[_currentPartIndex] : null);
       
        public bool NextLine()
        {
            _currentLineIndex++;
            _currentPartIndex = 0;
            if (_currentLineIndex >= _queryLines.Count) return false;

            _currentLineParts = SplitLine(_queryLines[_currentLineIndex]);

            return true;
        }

        public bool NextPart()
        {
            _currentPartIndex++;
            return _currentPartIndex < _currentLineParts.Count;
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
        private WorkflowContext _workflow;

        private static Dictionary<string, IPipelineStageBuilder> s_pipelineStageBuildersByName;

        private PipelineParser(string xqlQuery, WorkflowContext workflow = null)
        {
            EnsureLoaded();
            _scanner = new PipelineScanner(xqlQuery);
            _workflow = workflow;
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

        public static IEnumerable<string> SupportedVerbs
        {
            get
            {
                EnsureLoaded();
                return s_pipelineStageBuildersByName.Keys;
            }
        }

        private static void Add(IPipelineStageBuilder builder)
        {
            foreach (string verb in builder.Verbs)
            {
                s_pipelineStageBuildersByName[verb] = builder;
            }
        }

        public static IDataBatchEnumerator BuildPipeline(string xqlQuery, IDataBatchEnumerator source = null, WorkflowContext context = null)
        {
            PipelineParser parser = new PipelineParser(xqlQuery, context);
            return parser.NextPipeline(source);
        }

        public static IDataBatchEnumerator BuildStage(string xqlQueryLine, IDataBatchEnumerator source, WorkflowContext context = null)
        {
            PipelineParser parser = new PipelineParser(xqlQueryLine, context);
            return parser.NextStage(source);
        }

        public IDataBatchEnumerator NextPipeline(IDataBatchEnumerator source)
        {
            IDataBatchEnumerator pipeline = source;

            while (_scanner.HasCurrentLine)
            {
                if (_scanner.HasCurrentPart && _scanner.CurrentPart.Equals("end", StringComparison.OrdinalIgnoreCase))
                {
                    _scanner.NextLine();
                    break;
                }

                pipeline = NextStage(pipeline);
                _scanner.NextLine();
            }

            return pipeline;
        }

        public IDataBatchEnumerator NextStage(IDataBatchEnumerator source)
        {
            _currentLineBuilder = null;
            ParseNextOrThrow(() => s_pipelineStageBuildersByName.TryGetValue(_scanner.CurrentPart, out _currentLineBuilder), "verb", SupportedVerbs);
            IDataBatchEnumerator stage = _currentLineBuilder.Build(source, this);

            // Verify all arguments are used
            if (_scanner.HasCurrentPart) Throw(null);

            return stage;
        }

        public Type NextType()
        {
            ITypeProvider provider = null;
            ParseNextOrThrow(() => (provider = TypeProviderFactory.TryGet(_scanner.CurrentPart)) != null, "type", TypeProviderFactory.SupportedTypes);
            return provider.Type;
        }

        public string NextColumnName(IDataBatchEnumerator currentSource)
        {
            int columnIndex = -1;
            ParseNextOrThrow(() => currentSource.Columns.TryGetIndexOfColumn(_scanner.CurrentPart, out columnIndex), "columnName", currentSource.Columns.Select((cd) => cd.Name));
            return currentSource.Columns[columnIndex].Name;
        }

        public string NextOutputTableName()
        {
            string tableName = _scanner.CurrentPart;
            if (_workflow != null) throw new ArgumentException("'write' commands are not allowed in Database XQL queries. Each XQL file writes a binary table with the same name as the file.");
            ParseNextOrThrow(() => true, "tableName", null);
            return tableName;
        }

        public IDataBatchEnumerator NextTableSource()
        {
            string tableName = _scanner.CurrentPart;

            if (_workflow != null)
            {
                ParseNextOrThrow(() => true, "tableName", _workflow.Runner.TableNames);
                return _workflow.Runner.Build(_scanner.CurrentPart, _workflow);
            }

            ParseNextOrThrow(() => true, "tableName", null);
            if (tableName.EndsWith("xform") || Directory.Exists(tableName))
            {
                return new BinaryTableReader(tableName);
            }
            else
            {
                return new TabularFileReader(tableName);
            }
        }

        public bool NextBoolean()
        {
            bool value = false;
            ParseNextOrThrow(() => bool.TryParse(_scanner.CurrentPart, out value), "boolean", new string[] { "true", "false" });
            return value;
        }

        public int NextInteger()
        {
            int value = -1;
            ParseNextOrThrow(() => int.TryParse(_scanner.CurrentPart, out value), "integer");
            return value;
        }

        public string NextString()
        {
            string value = _scanner.CurrentPart;
            ParseNextOrThrow(() => true, "string");
            return value;
        }

        public object NextLiteralValue()
        {
            object value = (object)_scanner.CurrentPart;
            ParseNextOrThrow(() => true, "literal");
            return value;
        }

        public T NextEnum<T>() where T : struct
        {
            T value = default(T);
            ParseNextOrThrow(() => Enum.TryParse<T>(_scanner.CurrentPart, true, out value), typeof(T).Name, Enum.GetNames(typeof(T)));
            return value;
        }

        public CompareOperator NextCompareOperator()
        {
            CompareOperator cOp = CompareOperator.Equals;
            ParseNextOrThrow(() => _scanner.CurrentPart.TryParseCompareOperator(out cOp), "compareOperator", OperatorExtensions.ValidCompareOperators);
            return cOp;
        }

        private void ParseNextOrThrow(Func<bool> parseMethod, string valueCategory, IEnumerable<string> validValues = null)
        {
            if (!_scanner.HasCurrentPart || !parseMethod()) Throw(valueCategory, validValues);
            _scanner.NextPart();
        }

        private void Throw(string valueCategory, IEnumerable<string> validValues = null)
        {
            throw new UsageException(
                    (_currentLineBuilder != null ? _currentLineBuilder.Usage : null),
                    _scanner.CurrentPart,
                    valueCategory,
                    validValues);
        }

        public bool HasAnotherPart => _scanner.HasCurrentPart;
    }

    [Serializable]
    public class UsageException : ArgumentException
    {
        public string Usage { get; private set; }
        public string InvalidValue { get; private set; }
        public string InvalidValueCategory { get; private set; }
        public IEnumerable<string> ValidValues { get; private set; }

        public UsageException(string usage, string invalidValue, string invalidValueCategory, IEnumerable<string> validValues)
            : base(BuildMessage(usage, invalidValue, invalidValueCategory, validValues))
        {
            Usage = usage;
            InvalidValue = invalidValue;
            InvalidValueCategory = InvalidValueCategory;
            ValidValues = validValues;
        }

        private static string BuildMessage(string usage, string invalidValue, string invalidValueCategory, IEnumerable<string> validValues)
        {
            StringBuilder message = new StringBuilder();
            if (!String.IsNullOrEmpty(usage)) message.AppendLine($"Usage: {usage}");

            if (String.IsNullOrEmpty(invalidValueCategory))
            {
                message.AppendLine($"Value \"{invalidValue}\" found when no more arguments were expected.");
            }
            else if (String.IsNullOrEmpty(invalidValue))
            {
                message.AppendLine($"No argument found when {invalidValueCategory} was required.");
            }
            else
            {
                message.AppendLine($"\"{invalidValue}\" was not a valid {invalidValueCategory}.");
            }

            if (validValues != null)
            {
                message.AppendLine("Valid Options:");
                foreach (string value in validValues)
                {
                    message.AppendLine(value);
                }
            }

            return message.ToString();
        }

        public UsageException() { }
        public UsageException(string message) : base(message) { }
        public UsageException(string message, Exception inner) : base(message, inner) { }
        protected UsageException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

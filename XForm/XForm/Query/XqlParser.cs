// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

using XForm.Data;
using XForm.Extensions;
using XForm.Types;

namespace XForm.Query
{
    public class XqlParser
    {
        private XqlScanner _scanner;
        private IPipelineStageBuilder _currentLineBuilder;
        private WorkflowContext _workflow;

        private static Dictionary<string, IPipelineStageBuilder> s_pipelineStageBuildersByName;

        public XqlParser(string xqlQuery, WorkflowContext workflow)
        {
            EnsureLoaded();
            _scanner = new XqlScanner(xqlQuery);
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
            s_pipelineStageBuildersByName[builder.Verb] = builder;
        }

        public static IDataBatchEnumerator Parse(string xqlQuery, IDataBatchEnumerator source, WorkflowContext outerContext)
        {
            if (outerContext == null) throw new ArgumentNullException("outerContext");
            if (outerContext.StreamProvider == null) throw new ArgumentNullException("outerContext.StreamProvider");
            if (outerContext.Runner == null) throw new ArgumentNullException("outerContext.Runner");

            // Build an inner context to hold this copy of the parser
            WorkflowContext innerContext = WorkflowContext.Push(outerContext);
            XqlParser parser = new XqlParser(xqlQuery, innerContext);
            innerContext.CurrentQuery = xqlQuery;
            innerContext.Parser = parser;

            // Build the Pipeline
            IDataBatchEnumerator result = parser.NextPipeline(source);

            // Copy inner context results back out to the outer context
            innerContext.Pop(outerContext);

            return result;
        }

        public IDataBatchEnumerator NextPipeline(IDataBatchEnumerator source)
        {
            IDataBatchEnumerator pipeline = source;

            // For nested pipelines, we should still be on the line for the stage which has a nested pipeline. Move forward.
            if(_scanner.Current.Type == TokenType.Newline) _scanner.Next();

            while(_scanner.Current.Type != TokenType.End)
            {
                // If this line is 'end', this is the end of the inner pipeline. Leave it at the end of this line; the outer NextPipeline will skip to the next line.
                if (_scanner.Current.Value.Equals("end", StringComparison.OrdinalIgnoreCase))
                {
                    _scanner.Next();
                    break;
                }

                pipeline = NextStage(pipeline);
                _scanner.Next();
            }

            return pipeline;
        }

        public IDataBatchEnumerator NextStage(IDataBatchEnumerator source)
        {
            _currentLineBuilder = null;
            ParseNextOrThrow(() => s_pipelineStageBuildersByName.TryGetValue(_scanner.Current.Value, out _currentLineBuilder), "verb", TokenType.Value, SupportedVerbs);

            // Verify the Workflow Parser is this parser (need to use copy constructor on WorkflowContext when recursing to avoid resuming by parsing the wrong query)
            Debug.Assert(_workflow.Parser == this);

            IDataBatchEnumerator stage = _currentLineBuilder.Build(source, _workflow);

            // Verify all arguments are used
            if (HasAnotherPart) Throw(null);

            return stage;
        }

        public Type NextType()
        {
            ITypeProvider provider = null;
            ParseNextOrThrow(() => (provider = TypeProviderFactory.TryGet(_scanner.Current.Value)) != null, "type", TokenType.Value, TypeProviderFactory.SupportedTypes);
            return provider.Type;
        }

        public string NextColumnName(IDataBatchEnumerator currentSource)
        {
            int columnIndex = -1;
            ParseNextOrThrow(() => currentSource.Columns.TryGetIndexOfColumn(_scanner.Current.Value, out columnIndex), "columnName", TokenType.ColumnName, currentSource.Columns.Select((cd) => cd.Name));
            return currentSource.Columns[columnIndex].Name;
        }

        public string NextOutputColumnName(IDataBatchEnumerator currentSource)
        {
            string value = _scanner.Current.Value;
            ParseNextOrThrow(() => true, "columnName", TokenType.ColumnName);
            return value;
        }

        public string NextOutputTableName()
        {
            string tableName = _scanner.Current.Value;
            ParseNextOrThrow(() => true, "tableName", TokenType.Value, null);
            return tableName;
        }

        public IDataBatchEnumerator NextTableSource()
        {
            string tableName = _scanner.Current.Value;
            ParseNextOrThrow(() => _scanner.Current.Type == TokenType.Value, "tableName", TokenType.Value, _workflow.Runner.SourceNames);

            // If there's a WorkflowProvider, ask it to get the table. This will recurse.
            return _workflow.Runner.Build(tableName, _workflow);
        }

        public bool NextBoolean()
        {
            bool value = false;
            ParseNextOrThrow(() => _scanner.Current.Type == TokenType.Value && bool.TryParse(_scanner.Current.Value, out value), "boolean", TokenType.Value, new string[] { "true", "false" });
            return value;
        }

        public int NextInteger()
        {
            int value = -1;
            ParseNextOrThrow(() => int.TryParse(_scanner.Current.Value, out value), "integer", TokenType.Value);
            return value;
        }

        public TimeSpan NextTimeSpan()
        {
            TimeSpan value = TimeSpan.Zero;
            ParseNextOrThrow(() => _scanner.Current.Value.TryParseTimeSpanFriendly(out value), "TimeSpan [ex: '60s', '15m', '24h', '7d']", TokenType.Value);
            return value;
        }

        public string NextString()
        {
            string value = _scanner.Current.Value;
            ParseNextOrThrow(() => true, "string", TokenType.Value);
            return value;
        }

        public object NextLiteralValue()
        {
            object value = (object)_scanner.Current.Value;
            ParseNextOrThrow(() => true, "literal", TokenType.Value);
            return value;
        }

        public T NextEnum<T>() where T : struct
        {
            T value = default(T);
            ParseNextOrThrow(() => Enum.TryParse<T>(_scanner.Current.Value, true, out value), typeof(T).Name, TokenType.Value, Enum.GetNames(typeof(T)));
            return value;
        }

        public CompareOperator NextCompareOperator()
        {
            CompareOperator cOp = CompareOperator.Equals;
            ParseNextOrThrow(() => _scanner.Current.Value.TryParseCompareOperator(out cOp), "compareOperator", TokenType.Value, OperatorExtensions.ValidCompareOperators);
            return cOp;
        }

        private void ParseNextOrThrow(Func<bool> parseMethod, string valueCategory, TokenType? requiredTokenType = null, IEnumerable<string> validValues = null)
        {
            if (!HasAnotherPart
                || (requiredTokenType.HasValue && _scanner.Current.Type != requiredTokenType.Value)
                || !parseMethod())
            {
                Throw(valueCategory, validValues);
            }

            _scanner.Next();
        }

        private void Throw(string valueCategory, IEnumerable<string> validValues = null)
        {
            throw new UsageException(
                    _workflow.CurrentTable,
                    _scanner.Current.LineNumber,
                    (_currentLineBuilder != null ? _currentLineBuilder.Usage : null),
                    _scanner.Current.Value,
                    valueCategory,
                    XqlScanner.Escape(validValues, (valueCategory == "columnName" ? TokenType.ColumnName : TokenType.Value)));
        }

        public bool HasAnotherPart => _scanner.Current.Type != TokenType.Newline && _scanner.Current.Type != TokenType.End;
        public int CurrentLineNumber => _scanner.Current.LineNumber;
    }

    [Serializable]
    public class UsageException : ArgumentException
    {
        public string TableName { get; set; }
        public int QueryLineNumber { get; private set; }
        public string Usage { get; private set; }
        public string InvalidValue { get; private set; }
        public string InvalidValueCategory { get; private set; }
        public IEnumerable<string> ValidValues { get; private set; }

        public UsageException(string invalidValue, string invalidValueCategory, IEnumerable<string> validValues)
        : this(null, 0, null, invalidValue, invalidValueCategory, validValues)
        { }

        public UsageException(string tableName, int queryLineNumber, string usage, string invalidValue, string invalidValueCategory, IEnumerable<string> validValues)
            : base(BuildMessage(tableName, queryLineNumber, usage, invalidValue, invalidValueCategory, validValues))
        {
            TableName = tableName;
            QueryLineNumber = queryLineNumber;
            Usage = usage;
            InvalidValue = invalidValue;
            InvalidValueCategory = invalidValueCategory;

            if (validValues != null) validValues = validValues.OrderBy((s) => s);
            ValidValues = validValues;
        }

        private static string BuildMessage(string tableName, int queryLineNumber, string usage, string invalidValue, string invalidValueCategory, IEnumerable<string> validValues)
        {
            StringBuilder message = new StringBuilder();
            if (!String.IsNullOrEmpty(tableName)) message.AppendLine($"Table: {tableName}");
            if (queryLineNumber > 0) message.AppendLine($"Line: {queryLineNumber}");
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
                foreach (string value in validValues.OrderBy((s) => s))
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

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Aggregators;
using XForm.Columns;
using XForm.Data;
using XForm.Extensions;
using XForm.Functions;
using XForm.Query.Expression;
using XForm.Types;

namespace XForm.Query
{
    public class XqlParser
    {
        private XqlScanner _scanner;
        private XDatabaseContext _workflow;
        private Stack<IUsage> _currentlyBuilding;

        private static Dictionary<string, IVerbBuilder> s_pipelineStageBuildersByName;

        public XqlParser(string xqlQuery, XDatabaseContext workflow)
        {
            EnsureLoaded();
            _scanner = new XqlScanner(xqlQuery);
            _workflow = workflow;
            _currentlyBuilding = new Stack<IUsage>();
        }

        public void RewindTo(Position position)
        {
            _scanner.RewindTo(position);
        }

        private static void EnsureLoaded()
        {
            if (s_pipelineStageBuildersByName != null) return;
            s_pipelineStageBuildersByName = new Dictionary<string, IVerbBuilder>(StringComparer.OrdinalIgnoreCase);

            foreach (IVerbBuilder builder in InterfaceLoader.BuildAll<IVerbBuilder>())
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

        private static void Add(IVerbBuilder builder)
        {
            s_pipelineStageBuildersByName[builder.Verb] = builder;
        }

        public static IXTable Parse(string xqlQuery, IXTable source, XDatabaseContext outerContext)
        {
            if (outerContext == null) throw new ArgumentNullException("outerContext");
            if (outerContext.StreamProvider == null) throw new ArgumentNullException("outerContext.StreamProvider");
            if (outerContext.Runner == null) throw new ArgumentNullException("outerContext.Runner");

            // Build an inner context to hold this copy of the parser
            XDatabaseContext innerContext = XDatabaseContext.Push(outerContext);
            XqlParser parser = new XqlParser(xqlQuery, innerContext);
            innerContext.CurrentQuery = xqlQuery;
            innerContext.Parser = parser;

            // Build the Pipeline
            IXTable result = parser.NextQuery(source);

            // Copy inner context results back out to the outer context
            innerContext.Pop(outerContext);

            return result;
        }

        public IXTable NextQuery(IXTable source)
        {
            IXTable pipeline = source;

            // For nested pipelines, we should still be on the line for the stage which has a nested pipeline. Move forward.
            if (_scanner.Current.Type == TokenType.Newline) _scanner.Next();

            do
            {
                // If this line is 'end', this is the end of the inner pipeline. Leave it at the end of this line; the outer NextPipeline will skip to the next line.
                if (_scanner.Current.Value.Equals("end", StringComparison.OrdinalIgnoreCase))
                {
                    _scanner.Next();
                    break;
                }

                pipeline = NextVerb(pipeline);
                _scanner.Next();
            } while (_scanner.Current.Type != TokenType.End) ;

            return pipeline;
        }

        public IXTable NextVerb(IXTable source)
        {
            IVerbBuilder builder = null;
            ParseNextOrThrow(() => s_pipelineStageBuildersByName.TryGetValue(_scanner.Current.Value, out builder), "verb", TokenType.Value, SupportedVerbs);
            _currentlyBuilding.Push(builder);

            // Verify the Workflow Parser is this parser (need to use copy constructor on XDatabaseContext when recursing to avoid resuming by parsing the wrong query)
            Debug.Assert(_workflow.Parser == this);

            IXTable stage = null;

            try
            {
                stage = builder.Build(source, _workflow);
            }
            catch (ArgumentException ex)
            {
                Rethrow(ex);
            }

            // Verify all arguments are used
            if (HasAnotherPart) Throw(null);
            _currentlyBuilding.Pop();

            return stage;
        }

        public Type NextType()
        {
            ITypeProvider provider = null;
            ParseNextOrThrow(() => (provider = TypeProviderFactory.TryGet(_scanner.Current.Value)) != null, "type", TokenType.Value, TypeProviderFactory.SupportedTypes);
            return provider.Type;
        }

        public string NextColumnName(IXTable source, Type requiredType = null)
        {
            if (source == null) throw new ArgumentNullException("source");
            IXColumn column = null;

            ParseNextOrThrow(
                () => source.Columns.TryFind(_scanner.Current.Value, out column)
                && (requiredType == null || column.ColumnDetails.Type == requiredType),
                "[Column]",
                TokenType.ColumnName,
                EscapedColumnList(source, requiredType));

            return column.ColumnDetails.Name;
        }

        public string NextOutputColumnName(IXTable source)
        {
            string value = _scanner.Current.Value;
            ParseNextOrThrow(() => true, "[Column]", TokenType.ColumnName);
            return value;
        }

        public string NextOutputTableName()
        {
            string tableName = _scanner.Current.Value;
            ParseNextOrThrow(() => true, "Table", TokenType.Value, null);
            return tableName;
        }

        public IXTable NextTableSource()
        {
            string tableName = _scanner.Current.Value;

            // Throw for tokens of the wrong type, but don't consume the table name token yet
            if (_scanner.Current.Type != TokenType.Value) Throw("Table", _workflow.Runner.SourceNames);

            // As the WorkflowProvider to get the table. This will recurse.
            try
            {
                IXTable table = _workflow.Runner.Build(tableName, _workflow);

                // Consume the table *after* the runner built it
                _scanner.Next();

                return table;
            }
            catch (ArgumentException ex)
            {
                Rethrow(ex);
                return null;
            }
        }

        public IXColumn NextColumn(IXTable source, XDatabaseContext context, Type requiredType = null)
        {
            if (source == null) throw new ArgumentNullException("source");

            IXColumn result = null;

            if (_scanner.Current.Type == TokenType.Value)
            {
                object value = String8.Convert(_scanner.Current.Value, new byte[String8.GetLength(_scanner.Current.Value)]);

                if (requiredType != null && requiredType != typeof(String8))
                {
                    value = TypeConverterFactory.ConvertSingle(value, requiredType);
                }

                result = new ConstantColumn(source, value, (requiredType == null ? typeof(String8) : requiredType), !_scanner.Current.IsWrapped);
                _scanner.Next();
            }
            else if (_scanner.Current.Type == TokenType.FunctionName)
            {
                result = NextFunction(source, context);
            }
            else if (_scanner.Current.Type == TokenType.ColumnName)
            {
                result = source.Columns.Find(NextColumnName(source, requiredType));
            }

            if (result == null || (requiredType != null && result.ColumnDetails.Type != requiredType))
            {
                Throw("Col|Func|Const", EscapedColumnList(source, requiredType).Concat(EscapedFunctionList(requiredType)));
            }

            if (_scanner.Current.Value.Equals("as", StringComparison.OrdinalIgnoreCase))
            {
                _scanner.Next();
                string columnName = NextOutputColumnName(source);
                result = RenamedColumn.Build(result, columnName);
            }

            return result;
        }

        public IXColumn NextFunction(IXTable source, XDatabaseContext context, Type requiredType = null)
        {
            string value = _scanner.Current.Value;

            // Get the builder for the function
            IFunctionBuilder builder = null;
            ParseNextOrThrow(() => Factories.FunctionFactory.TryGetBuilder(_scanner.Current.Value, out builder)
                && (requiredType == null || builder.ReturnType == null || requiredType == builder.ReturnType),
                "Function",
                TokenType.FunctionName,
                Factories.FunctionFactory.SupportedFunctions(requiredType));

            _currentlyBuilding.Push(builder);

            // Parse the open paren
            ParseNextOrThrow(() => true, "(", TokenType.OpenParen);

            // Build the function (getting arguments for it)
            try
            {
                IXColumn result = builder.Build(source, context);

                // Ensure we've parsed all arguments, and consume the close paren
                ParseNextOrThrow(() => true, ")", TokenType.CloseParen);
                _currentlyBuilding.Pop();

                // Error if the final function doesn't have the required return type
                if (requiredType != null && result.ColumnDetails.Type != requiredType)
                {
                    Throw("Function", Factories.FunctionFactory.SupportedFunctions(requiredType));
                }

                return result;
            }
            catch (ArgumentException ex)
            {
                Rethrow(ex);
                return null;
            }
        }

        public IAggregator NextAggregator(IXTable source, XDatabaseContext context)
        {
            IAggregatorBuilder builder = null;
            ParseNextOrThrow(
                () => Factories.AggregatorFactory.TryGetBuilder(_scanner.Current.Value, out builder),
                "Aggregator",
                TokenType.FunctionName,
                Factories.AggregatorFactory.SupportedNames);

            _currentlyBuilding.Push(builder);

            // Parse the open paren
            ParseNextOrThrow(() => true, "(", TokenType.OpenParen);

            try
            {
                IAggregator result = builder.Build(source, context);

                // Ensure we've parsed all arguments, and consume the close paren
                ParseNextOrThrow(() => true, ")", TokenType.CloseParen);
                _currentlyBuilding.Pop();

                return result;
            }
            catch (ArgumentException ex)
            {
                Rethrow(ex);
                return null;
            }
        }

        public bool NextBoolean()
        {
            bool value = false;
            ParseNextOrThrow(() => _scanner.Current.Type == TokenType.Value
                && _scanner.Current.IsWrapped == false
                && bool.TryParse(_scanner.Current.Value, out value), "Boolean", TokenType.Value, new string[] { "true", "false" });
            return value;
        }

        public int NextInteger()
        {
            int value = -1;
            ParseNextOrThrow(() => _scanner.Current.Type == TokenType.Value
                && _scanner.Current.IsWrapped == false
                && int.TryParse(_scanner.Current.Value, out value), "Integer", TokenType.Value);
            return value;
        }

        public TimeSpan NextTimeSpan()
        {
            object value = null;
            ParseNextOrThrow(() => (TypeConverterFactory.TryConvertSingle(_scanner.Current.Value, typeof(TimeSpan), out value) && value != null), "TimeSpan ['60s', '7d']", TokenType.Value);
            return (TimeSpan)value;
        }

        public string NextString()
        {
            string value = _scanner.Current.Value;
            ParseNextOrThrow(() => _scanner.Current.IsWrapped || value == "null", "string", TokenType.Value);
            return value;
        }

        public object NextLiteralValue()
        {
            object value = (object)_scanner.Current.Value;
            ParseNextOrThrow(() => true, "Constant", TokenType.Value);
            return value;
        }

        public void NextSingleKeyword(string requiredValue)
        {
            ParseNextOrThrow(() => requiredValue.Equals(_scanner.Current.Value, StringComparison.OrdinalIgnoreCase), "Keyword", TokenType.Value, new string[] { requiredValue });
        }

        public T NextEnum<T>() where T : struct
        {
            T value = default(T);
            ParseNextOrThrow(() => Enum.TryParse<T>(_scanner.Current.Value, true, out value), typeof(T).Name, TokenType.Value, Enum.GetNames(typeof(T)));
            return value;
        }

        public CompareOperator NextCompareOperator()
        {
            CompareOperator cOp = CompareOperator.Equal;
            ParseNextOrThrow(() => _scanner.Current.Value.TryParseCompareOperator(out cOp), "CompareOperator", TokenType.Value, OperatorExtensions.ValidCompareOperators);
            return cOp;
        }

        public IExpression NextExpression(IXTable source, XDatabaseContext context)
        {
            List<IExpression> terms = new List<IExpression>();

            // Parse the first term (and any 'AND'ed terms)
            terms.Add(NextAndExpression(source, context));

            while (HasAnotherArgument)
            {
                // If this is not an OR, stop
                BooleanOperator bOp;
                if (!_scanner.Current.Value.TryParseBooleanOperator(out bOp)) break;
                if (bOp != BooleanOperator.Or) break;
                _scanner.Next();

                // Parse the next term
                terms.Add(NextAndExpression(source, context));
            }

            // Return the full expression
            if (terms.Count == 1) return terms[0];
            return new OrExpression(terms.ToArray());
        }

        private IExpression NextAndExpression(IXTable source, XDatabaseContext context)
        {
            List<IExpression> terms = new List<IExpression>();

            // Parse the first term
            terms.Add(NextTerm(source, context));

            while (HasAnotherArgument)
            {
                BooleanOperator bOp;
                if (_scanner.Current.Value.TryParseBooleanOperator(out bOp))
                {
                    // If this is an 'Or', pop out and parse the OrExpression
                    if (bOp == BooleanOperator.Or) break;

                    // Otherwise, consume it
                    _scanner.Next();
                }
                else
                {
                    // This is an implied AND, look for the next expression
                    // If there's a hint token here, suggest boolean operators
                    if (_scanner.Current.Type == TokenType.NextTokenHint) Throw("{AND|OR|NOT}", new string[] { "AND", "OR", "NOT" });
                }

                // Parse the next term
                terms.Add(NextTerm(source, context));
            }

            // Return the full expression
            if (terms.Count == 1) return terms[0];
            return new AndExpression(terms.ToArray());
        }

        private IExpression NextTerm(IXTable source, XDatabaseContext context)
        {
            IExpression term;
            bool negate = false;

            // Look for NOT
            if (_scanner.Current.Value.TryParseNot())
            {
                _scanner.Next();
                negate = true;
            }

            // Look for nested subexpression
            if (_scanner.Current.Type == TokenType.OpenParen)
            {
                // Consume the open paren
                _scanner.Next();

                term = NextExpression(source, context);

                // Consume the close paren (and tolerate it missing - partially typed queries)
                if (_scanner.Current.Type == TokenType.CloseParen) _scanner.Next();
            }
            else
            {
                // Otherwise, it's a simple term
                term = new TermExpression(
                    source,
                    NextColumn(source, context),
                    NextCompareOperator(),
                    NextColumn(source, context));
            }

            if (negate) return new NotExpression(term);
            return term;
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

        public static IEnumerable<string> EscapedColumnList(IXTable source, Type requiredType = null)
        {
            return source.Columns
                .Where((col) => (requiredType == null || col.ColumnDetails.Type == requiredType))
                .Select((col) => XqlScanner.Escape(col.ColumnDetails.Name, TokenType.ColumnName));
        }

        public static IEnumerable<string> EscapedFunctionList(Type requiredType = null)
        {
            return Factories.FunctionFactory.SupportedFunctions(requiredType).Select((name) => $"{name}(");
        }

        private ErrorContext BuildErrorContext()
        {
            ErrorContext context = new ErrorContext();
            context.TableName = _workflow.CurrentTable;
            context.QueryLineNumber = _scanner.Current.Position.LineNumber;
            context.Usage = (_currentlyBuilding.Count > 0 ? _currentlyBuilding.Peek().Usage : null);
            context.InvalidValue = _scanner.Current.RawValue;
            context.InvalidTokenIndex = _scanner.Current.Position.Index;
            context.ErrorMessage = "";

            return context;
        }

        private void Throw(string valueCategory, IEnumerable<string> validValues = null)
        {
            ErrorContext context = BuildErrorContext().Merge(new ErrorContext(_scanner.Current.RawValue, valueCategory, validValues));

            // Consume the bad token
            _scanner.Next();

            if (this.WasLastTokenInQuery)
            {
                // If this was the last token, no error message (wait for it to be complete)
                context.ErrorMessage = "";
            }
            else
            {
                // If not, return the error message
                if (String.IsNullOrEmpty(context.ErrorMessage))
                {
                    context.ErrorMessage = $"Invalid {context.InvalidValueCategory} \"{context.InvalidValue ?? "<null>"}\"";
                }
            }

            throw new UsageException(context);
        }

        private void Rethrow(Exception ex)
        {
            if (ex is UsageException)
            {
                ErrorContext context = BuildErrorContext().Merge(((UsageException)ex).Context);

                // Consume the bad token
                _scanner.Next();

                if (!this.WasLastTokenInQuery && String.IsNullOrEmpty(context.ErrorMessage))
                {
                    context.ErrorMessage = $"Invalid {context.InvalidValueCategory} \"{context.InvalidValue ?? "<null>"}\"";
                }

                throw new UsageException(context, ex);
            }
            else if (ex is ArgumentNullException && ((ArgumentNullException)ex).ParamName.Equals("source", StringComparison.OrdinalIgnoreCase))
            {
                ErrorContext context = BuildErrorContext();
                context.ErrorMessage = "read must be called before any other query verbs.";
                throw new UsageException(context, ex);
            }
            else if (ex is ArgumentException)
            {
                ErrorContext context = BuildErrorContext();
                context.ErrorMessage = ex.Message;
                throw new UsageException(context, ex);
            }
            else
            {
                throw ex;
            }
        }

        public bool WasLastTokenInQuery => ((_scanner.Current.Type == TokenType.End || _scanner.Current.Type == TokenType.NextTokenHint) && _scanner.Current.WhitespacePrefix.Length == 0);
        public bool HasAnotherPart => _scanner.Current.Type != TokenType.Newline && _scanner.Current.Type != TokenType.End;
        public bool HasAnotherArgument => HasAnotherPart && _scanner.Current.Type != TokenType.CloseParen;
        public Position CurrentPosition => _scanner.Current.Position;
        public string NextTokenText => (HasAnotherPart ? _scanner.Current.Value : "");
    }
}

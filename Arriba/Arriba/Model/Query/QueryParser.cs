// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Arriba.Extensions;
using Arriba.Model.Expressions;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  A Query where clause parser, used to convert string queries into IExpressions.
    ///  The goals of the grammar are to work for regular web search, code, or SQL syntax,
    ///  and to handle as many characters without explicit wrapping (quoting or bracing) as possible.
    ///  
    /// Query := AndExpression (Or AndExpression)*
    /// AndExpression := Term (And? Term)*
    /// Term := Negate? ( '(' Query ')' | ColumnNameOrValue (CompareOperator Value)?
    ///
    /// Negate := '-' | '!' | 'NOT'
    /// Or :=  '|' | '||' | 'OR'
    /// And := '&' | '&&' | 'AND'
    /// CompareOperator := ':' | '::' | '|&gt;' | '&lt;' | '&lt;=' | '&gt;' | '&gt;=' | '=' | '==' | '!=' | '&lt;&gt;'
    /// 
    /// ColumnNameOrValue := ExplicitColumnName | ExplicitValue | (NotSpaceParenOrCompareOperator)+
    /// Value := ExplicitValue | (NotSpaceOrParen)+
    /// 
    /// ExplicitColumnName:= '[' (NotRightBrace | ']]')+ ']'
    /// ExplicitValue := '"' (NotDoubleQuote | '""')+ '"'
    /// </summary>
    public class QueryParser
    {
        private QueryScanner _scanner;

        private QueryParser(TextReader reader)
        {
            _scanner = new QueryScanner(reader);
            _scanner.Next();
        }

        /// <summary>
        ///  Convert a literal column name back to a safe-to-parse identifier.
        /// </summary>
        /// <param name="columnName">Column Name to wrap</param>
        /// <returns>Parsable version of column name</returns>
        public static string WrapColumnName(string columnName)
        {
            if (String.IsNullOrEmpty(columnName)) return "";
            return StringExtensions.Format("[{0}]", columnName.Replace("]", "]]"));
        }

        /// <summary>
        ///  Convert a literal value back to a safe-to-parse identifier.
        /// </summary>
        /// <param name="value">Value to wrap</param>
        /// <returns>Identifier version of value</returns>
        public static string WrapValue(object value)
        {
            if (value == null) return "\"\"";

            // Nicer DateTime formatting
            if(value is DateTime)
            {
                DateTime dtv = (DateTime)value;
                if(dtv.TimeOfDay.TotalSeconds == 0)
                {
                    value = dtv.ToString("yyyy-MM-dd");
                }
                else
                {
                    value = dtv.ToString("u");
                }
            }

            string valueString = value.ToString();
            if (valueString.Length == 0) return "\"\"";

            bool shouldEscape = false;
            for (int i = 0; i < valueString.Length; ++i)
            {
                char current = valueString[i];
                if (Char.IsWhiteSpace(current) || current == '"')
                {
                    shouldEscape = true;
                    break;
                }
            }

            if (!shouldEscape) return valueString;

            return StringExtensions.Format("\"{0}\"", valueString.Replace("\"", "\"\""));
        }

        /// <summary>
        ///  Convert a column name to the literal column name (unescape it).
        /// </summary>
        /// <param name="columnName">Column Name to unwrap</param>
        /// <returns>Literal, unescaped column name</returns>
        public static string UnwrapColumnName(string columnName)
        {
            if (String.IsNullOrEmpty(columnName)) return "";
            if (!columnName.StartsWith("[") || !columnName.EndsWith("]")) return columnName;
            return Unescape(columnName, ']');
        }

        /// <summary>
        ///  Convert a value to the value (unescape it).
        /// </summary>
        /// <param name="value">Value to unwrap</param>
        /// <returns>Literal, unescaped value</returns>
        public static string UnwrapValue(string value)
        {
            if (String.IsNullOrEmpty(value)) return "";
            if (!value.StartsWith("\"") || !value.EndsWith("\"")) return value;
            return Unescape(value, '"');
        }

        private static string Unescape(string value, char escape)
        {
            StringBuilder result = new StringBuilder();
            int lastIndex = 1;
            int length = value.Length - 1;

            while (lastIndex < length)
            {
                // Find the next escape char
                int nextIndex = value.IndexOf(escape, lastIndex);

                // If there are two together here, append one of them as a literal
                if (nextIndex < length && value[nextIndex + 1] == escape)
                {
                    nextIndex++;
                }

                // Append everything before the escape
                int appendLength = nextIndex - lastIndex;
                if (appendLength > 0)
                {
                    result.Append(value.Substring(lastIndex, appendLength));
                }

                // Look after the escape for the next content
                lastIndex = nextIndex + 1;
            }

            return result.ToString();
        }

        public static IExpression Parse(string whereClause, bool includeHintTerms = false)
        {
            // Empty Where means everything (consider aggregations)
            if (String.IsNullOrEmpty(whereClause) || whereClause == "*") return new AllExpression();

            // Attempt to parse the query
            QueryParser parser = new QueryParser(new StringReader(whereClause));
            IExpression expression = parser.ParseQuery(includeHintTerms);

            // An unparsable query is empty
            if (expression == null) expression = new EmptyExpression();

            return expression;
        }

        public IExpression ParseQuery(bool includeHintTerms)
        {
            if (_scanner.Current.Type == TokenType.End) return null;

            List<IExpression> terms = new List<IExpression>();

            // Parse the first term
            IExpression term = ParseAndExpression(includeHintTerms);
            if (term == null) return null;
            terms.Add(term);

            while (_scanner.Current.Type == TokenType.BooleanOperatorOr)
            {
                // If this is an 'Or', combine with the next expression
                string operatorText = _scanner.Current.Content;
                _scanner.Next();

                if (_scanner.Current.Type == TokenType.End && String.IsNullOrEmpty(_scanner.Current.Prefix) && operatorText == "|")
                {
                    // If '|' was typed and there's no follow space, keep suggesting '||'
                    if (includeHintTerms) terms.Add(new TermExpression("") { Guidance = new IntelliSenseGuidance(operatorText, QueryTokenCategory.BooleanOperator) });
                }
                else
                {
                    term = ParseAndExpression(includeHintTerms);
                    if (term == null && includeHintTerms) term = new TermExpression("") { Guidance = new IntelliSenseGuidance("", QueryTokenCategory.Term) };
                    if (term != null) terms.Add(term);
                }
            }

            // If there was something invalid at the end of the query, return a null result
            if (_scanner.Current.Type != TokenType.End && _scanner.Current.Type != TokenType.RightParen)
            {
                return null;
            }

            if (terms.Count == 1)
            {
                return terms[0];
            }
            else
            {
                return new OrExpression(terms.ToArray());
            }
        }

        private IExpression ParseAndExpression(bool includeHintTerms)
        {
            List<IExpression> terms = new List<IExpression>();

            // Parse the first term
            IExpression term = ParseTerm(includeHintTerms, true);
            if (term == null) return null;
            terms.Add(term);

            while (_scanner.Current.Type != TokenType.End)
            {
                bool hadExplicitBooleanOperator = false;

                // If this is an 'And', combine with the next expression
                if (_scanner.Current.Type == TokenType.BooleanOperatorOr)
                {
                    // If this is an Or, pop up to the OrExpression
                    break;
                }
                else if (_scanner.Current.Type == TokenType.BooleanOperatorAnd)
                {
                    // If this is an explicit And, eat the operator and combine with the next expression
                    string operatorText = _scanner.Current.Content;
                    _scanner.Next();
                    hadExplicitBooleanOperator = true;

                    // If there's nothing after the And, add an empty term to hint intellisense
                    if (_scanner.Current.Type == TokenType.End)
                    {
                        if (String.IsNullOrEmpty(_scanner.Current.Prefix))
                        {
                            if (operatorText == "&")
                            {
                                // If '&' was typed and there's no following space, keep suggesting '&&'
                                if (includeHintTerms) terms.Add(new TermExpression("") { Guidance = new IntelliSenseGuidance(operatorText, QueryTokenCategory.BooleanOperator) });
                            }
                            else if (operatorText == "&&")
                            {
                                // If '&&', suggest next term (complete, unambiguous operator)
                                if (includeHintTerms) terms.Add(new TermExpression("") { Guidance = new IntelliSenseGuidance("", QueryTokenCategory.Term) });
                            }
                            else
                            {
                                // If 'AND' with no space, it could still be a column name or value
                                if (includeHintTerms) terms.Add(new TermExpression(operatorText) { Guidance = new IntelliSenseGuidance(operatorText, QueryTokenCategory.BooleanOperator | QueryTokenCategory.Term) });
                            }
                        }
                        else
                        {
                            // Otherwise, suggest column name or value (a new term without boolean operator)
                            if (includeHintTerms) terms.Add(new TermExpression("") { Guidance = new IntelliSenseGuidance("", QueryTokenCategory.Term) });
                        }
                    }
                }
                else
                {
                    // If this is an implied And, look for the next expression
                }

                // Parse the next term and add it. If no remaining terms, return an empty term to hint intellisense
                term = ParseTerm(includeHintTerms, hadExplicitBooleanOperator);
                if (term == null) break;
                terms.Add(term);
            }

            if (terms.Count == 1)
            {
                return terms[0];
            }
            else
            {
                return new AndExpression(terms.ToArray());
            }
        }

        private IExpression ParseTerm(bool includeHintTerms, bool hadExplicitBooleanOperator)
        {
            bool hadExplicitValueCompletion;
            IExpression expression = null;
            bool negate = false;

            // Negate?
            if (_scanner.Current.Type == TokenType.UnaryOperatorNot)
            {
                negate = true;
                _scanner.Next();

                // If the query ends with a !, create a term to hint
                if (_scanner.Current.Type == TokenType.End)
                {
                    if (includeHintTerms) return new TermExpression("") { Guidance = new IntelliSenseGuidance("", QueryTokenCategory.Term) };
                }
            }

            if (_scanner.Current.Type == TokenType.LeftParen)
            {
                // Subquery := '(' Query ')'

                // Consume the left paren
                _scanner.Next();

                // If there's nothing left, add a hint expression suggesting another term
                if (_scanner.Current.Type == TokenType.End)
                {
                    if (includeHintTerms) expression = new TermExpression("") { Guidance = new IntelliSenseGuidance("", QueryTokenCategory.Term) };
                }
                else
                {
                    // Parse the subquery
                    expression = ParseQuery(includeHintTerms);

                    // Consume right paren, if provided (tolerate it missing)
                    if (_scanner.Current.Type == TokenType.RightParen)
                    {
                        _scanner.Next();

                        // *If* an explicit closing paren was found, tell IntelliSense the previous term was complete
                        // [Can't add a hint expression here without breaking valid expressions]
                        if (expression != null)
                        {
                            TermExpression lastTerm = expression.GetLastTerm();
                            if (lastTerm != null)
                            {
                                lastTerm.Guidance = new IntelliSenseGuidance("", QueryTokenCategory.BooleanOperator | QueryTokenCategory.Term);
                            }
                        }
                    }
                }
            }
            else if (_scanner.Current.Type == TokenType.LeftBrace)
            {
                // ExplicitColumnName := '[' (NotEndBrace | ']]')+ ']'
                bool hadExplicitNameCompletion;
                string columnName = ParseUntilEndToken(TokenType.RightBrace, out hadExplicitNameCompletion);

                if (!hadExplicitNameCompletion)
                {
                    // "[PartialColumnNa" -> make it an equals empty term, indicate the column name needs completion
                    if (includeHintTerms) expression = new TermExpression(columnName, Operator.NotEquals, String.Empty) { Guidance = new IntelliSenseGuidance(columnName, QueryTokenCategory.ColumnName) };
                }
                else
                {
                    expression = ParseOperatorAndValue(columnName, includeHintTerms, true);
                }
            }
            else if (_scanner.Current.Type == TokenType.DoubleQuote)
            {
                // ExplicitValue := '"' (NotEndQuote || '""')+ '"'
                string value = ParseUntilEndToken(TokenType.DoubleQuote, out hadExplicitValueCompletion);
                if (!hadExplicitValueCompletion)
                {
                    // "\"IncompleteQuotedValue" -> indicate value incomplete
                    expression = new TermExpression(value) { Guidance = new IntelliSenseGuidance(value, QueryTokenCategory.Value) };
                }
                else if (_scanner.Current.Type == TokenType.End && String.IsNullOrEmpty(_scanner.Current.Prefix))
                {
                    // "\"QuotedValue\"", no trailing space -> don't suggest the next thing yet
                    expression = new TermExpression(value) { Guidance = new IntelliSenseGuidance(String.Empty, QueryTokenCategory.None) };
                }
                else
                {
                    // "\"QuotedValue\" ", trailing space -> suggest the next operator or term
                    expression = new TermExpression(value) { Guidance = new IntelliSenseGuidance(String.Empty, QueryTokenCategory.BooleanOperator | QueryTokenCategory.Term) };
                }
            }
            else
            {
                // NotSpaceParenOrCompareOperator, then look for compare operator and, if found, value
                string firstValue = ParseNotSpaceParenOrCompareOperator(out hadExplicitValueCompletion);

                // If there was no valid value left, it's the end of this term
                if (String.IsNullOrEmpty(firstValue)) return null;

                if (!hadExplicitValueCompletion)
                {
                    // "BareValu" -> column or value completion on this term
                    QueryTokenCategory options = QueryTokenCategory.ColumnName | QueryTokenCategory.Value;
                    if (!hadExplicitBooleanOperator) options |= QueryTokenCategory.BooleanOperator;

                    expression = new TermExpression(firstValue) { Guidance = new IntelliSenseGuidance(firstValue, options) };
                }
                else
                {
                    expression = ParseOperatorAndValue(firstValue, includeHintTerms, false);
                }
            }

            // Negate the expression, if not was present
            if (negate && expression != null)
            {
                expression = new NotExpression(expression);
            }

            return expression;
        }

        private IExpression ParseOperatorAndValue(string columnName, bool includeHintTerms, bool wasExplicitColumnName)
        {
            if (!IsCompareOperator(_scanner.Current.Type))
            {
                // If this is the end of the query and the start of an operator, assume the user wants to finish typing it
                string possibleOperatorPrefix = String.Empty;
                if (_scanner.IsLastToken())
                {
                    if (_scanner.Current.Content == "|" || _scanner.Current.Content == "!")
                    {
                        possibleOperatorPrefix = _scanner.Current.Content;
                        _scanner.Next();
                    }
                }

                if (wasExplicitColumnName)
                {
                    // "[ColumnName]" -> make it a not equals empty term, indicate operator needs completion
                    if (includeHintTerms)
                    {
                        return new TermExpression(columnName, Operator.NotEquals, String.Empty) { Guidance = new IntelliSenseGuidance(possibleOperatorPrefix, QueryTokenCategory.CompareOperator) };
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    if (possibleOperatorPrefix == String.Empty)
                    {
                        // "BareTerm" -> next thing could be a compare operator or another term
                        return new TermExpression(columnName) { Guidance = new IntelliSenseGuidance(String.Empty, QueryTokenCategory.CompareOperator | QueryTokenCategory.BooleanOperator | QueryTokenCategory.Term) };
                    }
                    else
                    {
                        // "BareTerm !" -> could be a compare operator or boolean operator, but can't be a column or term because it would require escaping
                        return new TermExpression(columnName) { Guidance = new IntelliSenseGuidance(possibleOperatorPrefix, QueryTokenCategory.CompareOperator | QueryTokenCategory.BooleanOperator) };
                    }
                }
            }

            Operator op = ConvertToOperator(_scanner.Current.Type);
            string opString = _scanner.Current.Content;
            _scanner.Next();

            // Parse value
            bool hadExplicitValueCompletion;
            string value = ParseValue(out hadExplicitValueCompletion);

            if (value == null)
            {
                if (String.IsNullOrEmpty(_scanner.Current.Prefix))
                {
                    // "[ColumnName] =" -> if no space after operator, suggest operator until space is typed
                    if (includeHintTerms)
                    {
                        return new TermExpression(columnName, op, String.Empty) { Guidance = new IntelliSenseGuidance(opString, QueryTokenCategory.CompareOperator) };
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    // "[ColumnName] = " -> time for the value
                    if (includeHintTerms)
                    {
                        return new TermExpression(columnName, op, String.Empty) { Guidance = new IntelliSenseGuidance(String.Empty, QueryTokenCategory.Value) };
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                if (!hadExplicitValueCompletion)
                {
                    // "[ColumnName] = \"Value" -> indicate value incomplete
                    return new TermExpression(columnName, op, value) { Guidance = new IntelliSenseGuidance(value, QueryTokenCategory.Value) };
                }
                else if (_scanner.Current.Type == TokenType.End && String.IsNullOrEmpty(_scanner.Current.Prefix))
                {
                    // "[ColumnName] = \"Value\"" [no trailing space] -> don't suggest anything yet
                    return new TermExpression(columnName, op, value) { Guidance = new IntelliSenseGuidance(String.Empty, QueryTokenCategory.None) };
                }
                else
                {
                    // "[ColumnName] = \"Value\" " [trailing space] -> time for next term (boolean operator, next column name, or next bare value)
                    return new TermExpression(columnName, op, value) { Guidance = new IntelliSenseGuidance(String.Empty, QueryTokenCategory.BooleanOperator | QueryTokenCategory.Term) };
                }
            }
        }

        private string ParseValue(out bool hadExplicitCompletion)
        {
            if (_scanner.Current.Type == TokenType.DoubleQuote)
            {
                // Quoted value - accept anything until end quote
                return ParseUntilEndToken(TokenType.DoubleQuote, out hadExplicitCompletion);
            }
            else
            {
                // Unquoted value - accept anything without space
                string value = ParseNotSpaceOrParen();

                // Value was explicitly completed if there was any following token or trailing whitespace
                hadExplicitCompletion = !(_scanner.Current.Type == TokenType.End && String.IsNullOrEmpty(_scanner.Current.Prefix));

                // If there was no value and it was unquoted, return null to indicate incomplete term
                if (String.IsNullOrEmpty(value)) return null;

                return value;
            }
        }

        private string ParseNotSpaceParenOrCompareOperator(out bool hadExplicitCompletion)
        {
            StringBuilder value = new StringBuilder();
            bool isFirstToken = true;

            // Consume and add values until space is seen
            while (_scanner.Current.Type != TokenType.End)
            {
                // If there was space (except before first token), stop
                if (isFirstToken == false && _scanner.Current.Prefix.Length > 0) break;

                // If there was a paren, stop
                if (IsParen(_scanner.Current.Type)) break;

                // If this is a compare operator, stop
                if (IsCompareOperator(_scanner.Current.Type)) break;

                // Otherwise, keep appending to value
                value.Append(_scanner.Current.Content);
                _scanner.Next();
                isFirstToken = false;
            }

            // Value was explicitly completed if there was any following token or trailing whitespace
            hadExplicitCompletion = !(_scanner.Current.Type == TokenType.End && String.IsNullOrEmpty(_scanner.Current.Prefix));

            return value.ToString();
        }

        private string ParseNotSpaceOrParen()
        {
            StringBuilder value = new StringBuilder();
            bool isFirstToken = true;

            // Consume and add values until space is seen
            while (_scanner.Current.Type != TokenType.End)
            {
                // If there was space, stop
                if (isFirstToken == false && _scanner.Current.Prefix.Length > 0) break;

                // If there was a paren, stop
                if (IsParen(_scanner.Current.Type)) break;

                // Otherwise, keep appending to value
                value.Append(_scanner.Current.Content);
                _scanner.Next();
                isFirstToken = false;
            }

            return value.ToString();
        }

        private string ParseUntilEndToken(TokenType endToken, out bool hadExplicitCompletion)
        {
            StringBuilder value = new StringBuilder();
            hadExplicitCompletion = false;

            // Consume start token
            _scanner.Next();

            // Consume and add complete values until end token
            while (_scanner.Current.Type != TokenType.End)
            {
                if (_scanner.Current.Type == endToken)
                {
                    // If we see an end token, add any prefix first
                    value.Append(_scanner.Current.Prefix);

                    // Now, is this an escaped token or the end?
                    _scanner.Next();

                    if (_scanner.Current.Type == endToken && _scanner.Current.Prefix.Length == 0)
                    {
                        // This was an escaped value. Consume and add one of them to the output
                        value.Append(_scanner.Current.Content);
                    }
                    else
                    {
                        // This was the end. Stop.
                        hadExplicitCompletion = true;
                        break;
                    }
                }
                else
                {
                    // Add anything else as literal content
                    value.Append(_scanner.Current.Prefix);
                    value.Append(_scanner.Current.Content);
                }

                _scanner.Next();
            }

            // If the end token wasn't seen and there's trailing whitespace, it should be part of the value
            if (!hadExplicitCompletion && _scanner.Current.Type == TokenType.End && _scanner.Current.Prefix.Length > 0)
            {
                value.Append(_scanner.Current.Prefix);
            }

            // NOTE: End token consumed by above loop

            return value.ToString();
        }

        private Operator ConvertToOperator(TokenType type)
        {
            switch (type)
            {
                case TokenType.CompareOperatorEquals:
                    return Operator.Equals;
                case TokenType.CompareOperatorStartsWith:
                    return Operator.StartsWith;
                case TokenType.CompareOperatorGreaterThan:
                    return Operator.GreaterThan;
                case TokenType.CompareOperatorGreaterThanOrEqual:
                    return Operator.GreaterThanOrEqual;
                case TokenType.CompareOperatorLessThan:
                    return Operator.LessThan;
                case TokenType.CompareOperatorLessThanOrEqual:
                    return Operator.LessThanOrEqual;
                case TokenType.CompareOperatorMatches:
                    return Operator.Matches;
                case TokenType.CompareOperatorMatchesExact:
                    return Operator.MatchesExact;
                case TokenType.CompareOperatorNotEquals:
                    return Operator.NotEquals;

                default:
                    return default(Operator);
            }
        }

        private bool IsParen(TokenType type)
        {
            switch (type)
            {
                case TokenType.LeftParen:
                case TokenType.RightParen:
                    return true;

                default:
                    return false;
            }
        }

        private bool IsCompareOperator(TokenType type)
        {
            switch (type)
            {
                case TokenType.CompareOperatorEquals:
                case TokenType.CompareOperatorGreaterThan:
                case TokenType.CompareOperatorGreaterThanOrEqual:
                case TokenType.CompareOperatorLessThan:
                case TokenType.CompareOperatorLessThanOrEqual:
                case TokenType.CompareOperatorMatches:
                case TokenType.CompareOperatorMatchesExact:
                case TokenType.CompareOperatorNotEquals:
                case TokenType.CompareOperatorStartsWith:
                    return true;

                default:
                    return false;
            }
        }
    }
}

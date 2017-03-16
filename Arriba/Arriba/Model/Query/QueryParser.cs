// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static IExpression Parse(string whereClause)
        {
            // Empty Where means everything (consider aggregations)
            if (String.IsNullOrEmpty(whereClause)) return new AllExpression();

            // Attempt to parse the query
            QueryParser parser = new QueryParser(new StringReader(whereClause));
            IExpression expression = parser.ParseQuery();

            // An unparsable query is empty
            if (expression == null) expression = new EmptyExpression();

            return expression;
        }

        public IExpression ParseQuery()
        {
            if (_scanner.Current.Type == TokenType.End) return null;

            List<IExpression> terms = new List<IExpression>();

            // Parse the first term
            IExpression term = ParseAndExpression();
            if (term == null) return null;
            terms.Add(term);

            while (_scanner.Current.Type == TokenType.BooleanOperatorOr)
            {
                // If this is an 'Or', combine with the next expression
                _scanner.Next();

                // Parse the next term and add it or stop of no more terms
                term = ParseAndExpression();
                if (term == null) break;
                terms.Add(term);
            }

            // If there was something invalid at the end of the query, return a null result
            if(_scanner.Current.Type != TokenType.End && _scanner.Current.Type != TokenType.RightParen)
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

        private IExpression ParseAndExpression()
        {
            List<IExpression> terms = new List<IExpression>();

            // Parse the first term
            IExpression term = ParseTerm(true);
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
                    _scanner.Next();
                    hadExplicitBooleanOperator = true;
                }
                else
                {
                    // If this is an implied And, look for the next expression
                }

                // Parse the next term and add it or stop of no more terms
                term = ParseTerm(hadExplicitBooleanOperator);
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

        private IExpression ParseTerm(bool hadExplicitBooleanOperator)
        {
            bool hadExplicitValueCompletion;
            IExpression expression = null;
            bool negate = false;

            // Negate?
            if (_scanner.Current.Type == TokenType.UnaryOperatorNot)
            {
                negate = true;
                _scanner.Next();
            }

            if (_scanner.Current.Type == TokenType.LeftParen)
            {
                // Subquery := '(' Query ')'

                // Consume the left paren
                _scanner.Next();

                // Parse the subquery
                expression = ParseQuery();

                // Consume right paren, if provided (tolerate it missing)
                if (_scanner.Current.Type == TokenType.RightParen)
                {
                    _scanner.Next();
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
                    expression = new TermExpression(columnName, Operator.Equals, String.Empty) { Guidance = new IntelliSenseGuidance(columnName, CurrentTokenCategory.ColumnName) };
                }
                else
                {
                    expression = ParseOperatorAndValue(columnName, true);
                }
            }
            else if (_scanner.Current.Type == TokenType.DoubleQuote)
            {
                // ExplicitValue := '"' (NotEndQuote || '""')+ '"'
                string value = ParseUntilEndToken(TokenType.DoubleQuote, out hadExplicitValueCompletion);
                if (!hadExplicitValueCompletion)
                {
                    // "\"IncompleteQuotedValue" -> indicate value incomplete
                    expression = new TermExpression(value) { Guidance = new IntelliSenseGuidance(value, CurrentTokenCategory.Value) };
                }
                else
                {
                    // "\"QuotedValue\" -> time for next term (boolean operator, next column name, or next bare value)
                    expression = new TermExpression(value) { Guidance = new IntelliSenseGuidance(String.Empty, CurrentTokenCategory.BooleanOperator | CurrentTokenCategory.ColumnName | CurrentTokenCategory.Value) };
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
                    expression = new TermExpression(firstValue) { Guidance = new IntelliSenseGuidance(firstValue, CurrentTokenCategory.ColumnName | CurrentTokenCategory.Value) };
                }
                else
                {
                    expression = ParseOperatorAndValue(firstValue, false);
                }
            }

            // Negate the expression, if not was present
            if (negate && expression != null)
            {
                expression = new NotExpression(expression);
            }

            return expression;
        }

        private IExpression ParseOperatorAndValue(string columnName, bool wasExplicitColumnName)
        {
            if (!IsCompareOperator(_scanner.Current.Type))
            {
                // If this is the end of the query and the start of an operator, assume the user wants to finish typing it
                string possibleOperatorPrefix = String.Empty;
                if(_scanner.IsLastToken())
                {
                    if(_scanner.Current.Content == "|" || _scanner.Current.Content == "!")
                    {
                        possibleOperatorPrefix = _scanner.Current.Content;
                        _scanner.Next();
                    }
                }
                
                if (wasExplicitColumnName)
                {
                    // "[ColumnName]" -> make it a not equals empty term, indicate operator needs completion
                    return new TermExpression(columnName, Operator.NotEquals, String.Empty) { Guidance = new IntelliSenseGuidance(possibleOperatorPrefix, CurrentTokenCategory.CompareOperator) };
                }
                else
                {
                    if (possibleOperatorPrefix == String.Empty)
                    { 
                        // "BareTerm" -> next thing could be a compare operator or another term
                        return new TermExpression(columnName) { Guidance = new IntelliSenseGuidance(String.Empty, CurrentTokenCategory.CompareOperator | CurrentTokenCategory.BooleanOperator | CurrentTokenCategory.ColumnName | CurrentTokenCategory.Value) };
                    }
                    else
                    {
                        // "BareTerm !" -> could be a compare operator or boolean operator, but can't be a column or term because it would require escaping
                        return new TermExpression(columnName) { Guidance = new IntelliSenseGuidance(possibleOperatorPrefix, CurrentTokenCategory.CompareOperator | CurrentTokenCategory.BooleanOperator) };
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
                    return new TermExpression(columnName, op, String.Empty) { Guidance = new IntelliSenseGuidance(opString, CurrentTokenCategory.CompareOperator) };
                }
                else
                {
                    // "[ColumnName] = " -> time for the value
                    return new TermExpression(columnName, op, String.Empty) { Guidance = new IntelliSenseGuidance(String.Empty, CurrentTokenCategory.Value) };
                }
            }
            else
            {
                if (hadExplicitValueCompletion)
                {
                    // "[ColumnName] = \"Value\"" -> time for next term (boolean operator, next column name, or next bare value)
                    return new TermExpression(columnName, op, value) { Guidance = new IntelliSenseGuidance(String.Empty, CurrentTokenCategory.BooleanOperator | CurrentTokenCategory.ColumnName | CurrentTokenCategory.Value) };
                }
                else
                {
                    // "[ColumnName] = \"Value" -> indicate value incomplete
                    return new TermExpression(columnName, op, value) { Guidance = new IntelliSenseGuidance(value, CurrentTokenCategory.Value) };
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

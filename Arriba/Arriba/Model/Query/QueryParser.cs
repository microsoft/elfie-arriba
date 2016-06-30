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
            IExpression term = ParseTerm();
            if (term == null) return null;
            terms.Add(term);

            while (_scanner.Current.Type != TokenType.End)
            {
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
                }
                else
                {
                    // If this is an implied And, look for the next expression
                }

                // Parse the next term and add it or stop of no more terms
                term = ParseTerm();
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

        private IExpression ParseTerm()
        {
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
                string columnName = ParseUntilEndToken(TokenType.RightBrace);

                // Parse operator (or error)
                if (!IsCompareOperator(_scanner.Current.Type)) return null;
                Operator op = ConvertToOperator(_scanner.Current.Type);
                _scanner.Next();

                // Parse value
                string value = ParseValue();
                if (value != null)
                {
                    expression = new TermExpression(columnName, op, value);
                }
            }
            else if (_scanner.Current.Type == TokenType.DoubleQuote)
            {
                // ExplicitValue := '"' (NotEndQuote || '""')+ '"'
                string value = ParseUntilEndToken(TokenType.DoubleQuote);
                expression = new TermExpression("*", Operator.Matches, value);
            }
            else
            {
                // NotSpaceParenOrCompareOperator, then look for compare operator and, if found, value
                string firstValue = ParseNotSpaceParenOrCompareOperator();

                // If there was no valid value left, it's the end of this term
                if (String.IsNullOrEmpty(firstValue)) return null;

                if (!IsCompareOperator(_scanner.Current.Type))
                {
                    // MatchAllTerm := Value
                    expression = new TermExpression("*", Operator.Matches, firstValue);
                }
                else
                {
                    // ColumnTerm := ColumnName CompareOperator Value
                    Operator op = ConvertToOperator(_scanner.Current.Type);
                    _scanner.Next();

                    string value = ParseValue();
                    if (value != null)
                    {
                        expression = new TermExpression(firstValue, op, value);
                    }
                }
            }

            // Negate the expression, if not was present
            if (negate && expression != null)
            {
                expression = new NotExpression(expression);
            }

            return expression;
        }

        private string ParseValue()
        {
            if (_scanner.Current.Type == TokenType.DoubleQuote)
            {
                // Quoted value - accept anything until end quote
                return ParseUntilEndToken(TokenType.DoubleQuote);
            }
            else
            {
                // Unquoted value - accept anything without space
                string value = ParseNotSpaceOrParen();

                // If there was no value and it was unquoted, return null to indicate incomplete term
                if (String.IsNullOrEmpty(value)) return null;

                return value;
            }
        }

        private string ParseNotSpaceParenOrCompareOperator()
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

        private string ParseUntilEndToken(TokenType endToken)
        {
            StringBuilder value = new StringBuilder();

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

            // NOTE: End token already consumed here checking to see if it was escaped

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

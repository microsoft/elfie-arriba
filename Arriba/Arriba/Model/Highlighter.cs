// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Arriba.Extensions;
using Arriba.Indexing;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Structures;

namespace Arriba.Model
{
    /// <summary>
    ///  Highlighter is used to highlight terms from the query in returned column values.
    ///  It highlights only terms which apply to the column being returned, only those
    ///  which match using the MATCH or MATCHEXACT operators, and only those which really
    ///  match using the column splitter.
    /// </summary>
    public class Highlighter
    {
        private string _wrapPrefix;
        private string _wrapSuffix;

        public Highlighter(string prefix, string suffix)
        {
            _wrapPrefix = prefix;
            _wrapSuffix = suffix;
        }

        public static List<HighlightTerm> WordsForColumn(IExpression where, string columnName, IWordSplitter splitter)
        {
            List<HighlightTerm> terms = new List<HighlightTerm>();

            // For each term in the query expression...
            IList<TermExpression> matchingTerms = where.GetAllTerms(columnName);
            foreach (TermExpression term in matchingTerms)
            {
                ByteBlock termValue;
                if (term.Value.TryConvert<ByteBlock>(out termValue))
                {
                    ByteBlock termValueLower = termValue.Copy();
                    termValueLower.ToLowerInvariant();

                    if (term.Operator == Operator.MatchesExact)
                    {
                        terms.Add(new HighlightTerm(termValueLower, true));
                    }
                    else if (term.Operator == Operator.Matches)
                    {
                        // Split the term value
                        RangeSet set = splitter.Split(termValueLower);

                        // Add each range as a word to the result [exact when under expand length limit]
                        for (int i = 0; i < set.Count; ++i)
                        {
                            Range r = set.Ranges[i];
                            ByteBlock partBlock = new ByteBlock(termValueLower.Array, r.Index, r.Length);
                            terms.Add(new HighlightTerm(partBlock, partBlock.Length < WordIndex.MinimumPrefixExpandLength));
                        }
                    }
                }
            }

            return terms;
        }

        public void Highlight(Array values, IColumn column, SelectQuery query)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (column == null) throw new ArgumentNullException("column");
            if (query == null) throw new ArgumentNullException("query");

            // Get the index for this column, if there is one
            IndexedColumn index = column.FindComponent<IndexedColumn>();

            // Non-indexed columns are not highlighted
            if (index == null) return;

            // Get the splitter for this column
            IWordSplitter splitter = index.Splitter;

            // Find all words for this column
            List<HighlightTerm> terms = WordsForColumn(query.Where, column.Name, splitter);

            // Highlight each value in this column
            for (int i = 0; i < values.Length; ++i)
            {
                object rawValue = values.GetValue(i);

                if (rawValue is ByteBlock)
                {
                    values.SetValue(Highlight((ByteBlock)rawValue, splitter, terms), i);
                }
            }
        }

        public ByteBlock Highlight(ByteBlock value, IWordSplitter splitter, List<HighlightTerm> terms)
        {
            if (terms == null) throw new ArgumentNullException("terms");
            if (splitter == null) throw new ArgumentNullException("splitter");

            ByteBlockAppender appender = new ByteBlockAppender(value);

            // Make a lowercase value copy to compare
            ByteBlock valueLower = value.Copy();
            valueLower.ToLowerInvariant();

            // Split the value and compare words to highlight terms
            RangeSet ranges = splitter.Split(valueLower);
            for (int rangeIndex = 0; rangeIndex < ranges.Count; ++rangeIndex)
            {
                Range r = ranges.Ranges[rangeIndex];
                ByteBlock valueWord = new ByteBlock(valueLower.Array, r.Index, r.Length);

                foreach (HighlightTerm term in terms)
                {
                    // If this word in the value starts with a search term...
                    if (term.Matches(valueWord))
                    {
                        // Append to the beginning of the word, if not already past this point
                        if (appender.AppendTo(valueWord.Index))
                        {
                            // Wrap and append the *prefix* of the term from the query
                            appender.Append(_wrapPrefix);
                            appender.AppendTo(valueWord.Index + term.Value.Length);
                            appender.Append(_wrapSuffix);
                        }

                        // If we matched, do not check other HighlightTerms against this word
                        break;
                    }
                }
            }

            // Append the remaining content and return
            appender.AppendRemainder();
            return appender.Value();
        }

        /// <summary>
        ///  Helper to wrap a source string during highlighting. Tracks how much of
        ///  the source string has been written and writes values between highlighted
        ///  segments automatically.
        /// </summary>
        internal struct ByteBlockAppender
        {
            private StringBuilder _result;
            private ByteBlock _source;
            private int _lastAppendIndex;

            public ByteBlockAppender(ByteBlock source)
            {
                _result = null;
                _source = source;
                _lastAppendIndex = source.Index;
            }

            public void Append(string value)
            {
                // Create the StringBuilder (only when required)
                if (_result == null) _result = new StringBuilder();

                _result.Append(value);
            }

            public bool AppendTo(int index)
            {
                // Compute the index relative to the source ByteBlock
                int sourceRelativeIndex = _source.Index + index;

                // Don't append if we've already appended past this point
                if (sourceRelativeIndex < _lastAppendIndex) return false;

                // Create the StringBuilder (only when required)
                if (_result == null) _result = new StringBuilder();

                ByteBlock appendPart = new ByteBlock(_source.Array, _lastAppendIndex, (sourceRelativeIndex - _lastAppendIndex));
                _result.Append(appendPart.ToString());

                // Track the remainder left to append
                _lastAppendIndex += appendPart.Length;

                return true;
            }

            public void AppendRemainder()
            {
                // If we have a new value, append the remainder
                if (_result != null)
                {
                    int remainingLength = (_source.Index + _source.Length - _lastAppendIndex);
                    if (remainingLength > 0)
                    {
                        ByteBlock appendPart = new ByteBlock(_source.Array, _lastAppendIndex, remainingLength);
                        _result.Append(appendPart.ToString());
                    }
                }
            }

            public ByteBlock Value()
            {
                // Return the original if we never changed the value
                if (_result == null)
                {
                    return _source;
                }
                else
                {
                    return _result.ToString();
                }
            }
        }

        /// <summary>
        ///  Represents a term to be highlighted by the highlighter. Populated by
        ///  walking the query for a particular column to find all matching terms.
        /// </summary>
        public struct HighlightTerm
        {
            public ByteBlock Value;
            public bool IsExact;

            public HighlightTerm(ByteBlock value, bool isExact)
            {
                this.Value = value;
                this.IsExact = isExact;
            }

            public bool Matches(ByteBlock other)
            {
                if (this.IsExact)
                {
                    return this.Value.CompareTo(other) == 0;
                }
                else
                {
                    return this.Value.IsPrefixOf(other) == 0;
                }
            }
        }
    }
}

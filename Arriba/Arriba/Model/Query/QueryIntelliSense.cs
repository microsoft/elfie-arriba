using Arriba.Model.Column;
using Arriba.Model.Expressions;
using System;
using System.Collections.Generic;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  Enum of different broad token categories which the last token could be.
    /// </summary>
    [Flags]
    public enum QueryTokenCategory : byte
    {
        None = 0x0,
        BooleanOperator = 0x1,
        ColumnName = 0x2,
        CompareOperator = 0x4,
        TermPrefixes = 0x8,
        Value = 0x10,
        Term = TermPrefixes | ColumnName | Value
    }

    /// <summary>
    ///  Contains the last incomplete token (if any) and possible grammar categories for it.
    ///  
    ///  Ex:
    ///    "[Inter"        -> Value = "Inter", Options = ColumnName
    ///    "Inter          -> Value = "Inter", Options = TermPrefix, ColumnName, Value
    ///    "Internal = f"  -> Value = "v", Options = Value
    /// </summary>
    public struct IntelliSenseGuidance
    {
        public string Value;
        public QueryTokenCategory Options;

        public IntelliSenseGuidance(string value, QueryTokenCategory options)
        {
            this.Value = value;
            this.Options = options;
        }

        public override string ToString()
        {
            return String.Format("[{0}] [{1}]", this.Value, this.Options);
        }
    }

    /// <summary>
    ///  IntelliSenseItem represents one item IntelliSense could complete at the current
    ///  cursor position for the given query.
    /// </summary>
    public class IntelliSenseItem
    {
        public QueryTokenCategory Category { get; set; }
        public string Value { get; set; }
        public string Hint { get; set; }
        public string CompleteAs { get; set; }

        public IntelliSenseItem(QueryTokenCategory category, string value, string hint) : this(category, value, hint, value)
        { }

        public IntelliSenseItem(QueryTokenCategory category, string value, string hint, string completeAs)
        {
            this.Category = category;
            this.Value = value;
            this.Hint = hint;
            this.CompleteAs = completeAs;
        }

        public override string ToString()
        {
            return this.Value;
        }
    }

    public class IntelliSenseResult
    {
        public string CurrentIncompleteValue;
        public IList<IntelliSenseItem> Suggestions;
        public IReadOnlyList<char> CompletionCharacters;
    }

    public class QueryIntelliSense
    {
        #region Static Token IntelliSense Items
        private static List<IntelliSenseItem> BooleanOperators = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.BooleanOperator, "AND", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.BooleanOperator, "OR", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.BooleanOperator, "&&", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.BooleanOperator, "||", String.Empty)
        };

        private static List<IntelliSenseItem> CompareOperators = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, ":", "contains word starting with"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "=", "equals [case sensitive]"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "==", "equals [case sensitive]"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "::", "contains exact word"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "!=", "not equals"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "<>", "not equals"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "<", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "<=", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, ">", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, ">=", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "|>", "starts with")
        };

        private static List<IntelliSenseItem> TermPrefixes = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.TermPrefixes, "!", "negate next term"),
            new IntelliSenseItem(QueryTokenCategory.TermPrefixes, "(", "start subexpression")
        };

        private static IntelliSenseItem Value = new IntelliSenseItem(QueryTokenCategory.Value, "\"<value>\"", "value (quote escaped)", "\"");
        #endregion

        public string GetCompletedQuery(string queryBeforeCursor, IntelliSenseResult result, IntelliSenseItem selectedItem, char completionCharacter)
        {
            string queryWithoutIncompleteValue = queryBeforeCursor.TrimEnd();
            if (!queryWithoutIncompleteValue.EndsWith(result.CurrentIncompleteValue)) throw new ArribaException("Error: IntelliSense suggestion couldn't be applied.");
            queryWithoutIncompleteValue = queryWithoutIncompleteValue.Substring(0, queryWithoutIncompleteValue.Length - result.CurrentIncompleteValue.Length);

            return queryWithoutIncompleteValue + selectedItem.CompleteAs + completionCharacter;
        }

        /// <summary>
        ///  Get the set of IntelliSense suggestions valid at the current position.
        ///  It's filtered to the set of valid query parts are the current position,
        ///  as well as the set of values the current partial value is a prefix for.
        /// </summary>
        /// <param name="queryBeforeCursor">Current Arriba Query up to the cursor position</param>
        /// <param name="targetTables">Table[s] which are valid for the current query</param>
        /// <returns>IntelliSenseResult reporting what to show</returns>
        public IntelliSenseResult GetIntelliSenseItems(string queryBeforeCursor, IReadOnlyCollection<Table> targetTables)
        {
            IntelliSenseGuidance guidance = GetCurrentTokenOptions(queryBeforeCursor);

            List<IntelliSenseItem> suggestions = new List<IntelliSenseItem>();

            if (guidance.Options.HasFlag(QueryTokenCategory.BooleanOperator))
            {
                AddWhenPrefixes(BooleanOperators, guidance.Value, suggestions);
            }

            if (guidance.Options.HasFlag(QueryTokenCategory.CompareOperator))
            {
                AddWhenPrefixes(CompareOperators, guidance.Value, suggestions);
            }

            if (guidance.Options.HasFlag(QueryTokenCategory.ColumnName))
            {
                List<IntelliSenseItem> selectedColumns = new List<IntelliSenseItem>();

                foreach (Table table in targetTables)
                {
                    foreach (ColumnDetails column in table.ColumnDetails)
                    {
                        if (column.Name.StartsWith(guidance.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            selectedColumns.Add(new IntelliSenseItem(QueryTokenCategory.ColumnName, column.Name, String.Format("{0}.{1} [{2}]", table.Name, column.Name, column.Type)));
                        }
                    }
                }

                selectedColumns.Sort((left, right) => left.Value.CompareTo(right.Value));
                suggestions.AddRange(selectedColumns);
            }

            if (guidance.Options.HasFlag(QueryTokenCategory.Value))
            {
                suggestions.Add(Value);
            }

            if (guidance.Options.HasFlag(QueryTokenCategory.TermPrefixes))
            {
                AddWhenPrefixes(TermPrefixes, guidance.Value, suggestions);
            }

            return new IntelliSenseResult() { CurrentIncompleteValue = guidance.Value, Suggestions = suggestions };
        }

        internal IntelliSenseGuidance GetCurrentTokenOptions(string queryBeforeCursor)
        {
            IntelliSenseGuidance defaultGuidance = new IntelliSenseGuidance(String.Empty, QueryTokenCategory.ColumnName | QueryTokenCategory.Value);

            // If the query is empty, return the guidance for the beginning of the first term
            if (String.IsNullOrEmpty(queryBeforeCursor)) return defaultGuidance;

            // Parse the query
            IExpression query = QueryParser.Parse(queryBeforeCursor);

            // If the query had parse errors, return empty guidance
            if (query is EmptyExpression) return new IntelliSenseGuidance(String.Empty, QueryTokenCategory.None);

            // Get the last query term to look at the IntelliSense guidance
            TermExpression lastTerm = query.GetLastTerm();

            // If no last term, return first term guidance (ex: inside new '('
            if (lastTerm == null) return defaultGuidance;

            // Otherwise, grab the last term guidance
            IntelliSenseGuidance guidance = lastTerm.Guidance;

            return guidance;
        }

        private static void AddWhenPrefixes(ICollection<IntelliSenseItem> items, string prefix, List<IntelliSenseItem> resultSet)
        {
            if (String.IsNullOrEmpty(prefix))
            {
                resultSet.AddRange(items);
            }
            else
            {
                foreach (IntelliSenseItem item in items)
                {
                    if (item.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        resultSet.Add(item);
                    }
                }
            }
        }
    }
}

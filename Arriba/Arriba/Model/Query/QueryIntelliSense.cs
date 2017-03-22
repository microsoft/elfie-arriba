using Arriba.Extensions;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Structures;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <summary>
        ///  Grammatical Gategory of item [BooleanOperator, ColumnName, etc]
        /// </summary>
        public QueryTokenCategory Category { get; set; }

        /// <summary>
        ///  Value to show in IntelliSense
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        ///  Hint Text to show in IntelliSense
        /// </summary>
        public string Hint { get; set; }

        /// <summary>
        ///  Actual value to append to query when completed
        /// </summary>
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
            return String.Format("{0} {1} [{2}] ({3})", this.Value, this.Hint, this.Category, this.CompleteAs);
        }
    }

    /// <summary>
    ///  IntelliSenseResult is the response from GetIntelliSenseItems.
    /// </summary>
    public class IntelliSenseResult
    {
        /// <summary>
        ///  The User Query for which IntelliSense items were requested.
        /// </summary>
        public string Query;

        /// <summary>
        ///  The last incomplete token being completed. This value must
        ///  be removed from the end of the query and replaced with the 
        ///  'CompleteAs' value and non-whitespace completion character to
        ///  complete a value.
        /// </summary>
        public string CurrentIncompleteValue;

        /// <summary>
        ///  The query up to the beginning of the CurrentIncompleteValue.
        ///  This is the prefix which the 'CompleteAs' value for the
        ///  selected IntelliSenseItem should be appended to.
        /// </summary>
        public string CurrentCompleteValue;

        /// <summary>
        ///  The set of suggested completions in ranked order, best match
        ///  first.
        /// </summary>
        public IList<IntelliSenseItem> Suggestions;

        /// <summary>
        ///  The set of characters which should cause the selected
        ///  IntelliSenseItem to be completed immediately in this state
        /// </summary>
        public IReadOnlyList<char> CompletionCharacters;
    }

    /// <summary>
    ///  QueryIntelliSense provides IntelliSense support for the Arriba Query Syntax for a given set of in-scope tables
    ///  and a provided query.
    /// </summary>
    public class QueryIntelliSense
    {
        #region Static Token IntelliSense Items
        internal static List<IntelliSenseItem> BooleanOperators = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.BooleanOperator, "AND", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.BooleanOperator, "OR", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.BooleanOperator, "&&", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.BooleanOperator, "||", String.Empty)
        };

        internal static List<IntelliSenseItem> CompareOperators = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, ":", "contains word starting with"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "::", "contains exact word"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "=", "equals [case sensitive]"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "==", "equals [case sensitive]"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "<", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "<=", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, ">", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, ">=", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "!=", "not equals"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "<>", "not equals"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "|>", "starts with")
        };

        internal static List<IntelliSenseItem> TermPrefixes = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.TermPrefixes, "!", "negate next term"),
            new IntelliSenseItem(QueryTokenCategory.TermPrefixes, "(", "start subexpression")
        };

        internal static IntelliSenseItem Value = new IntelliSenseItem(QueryTokenCategory.Value, "\"<value>\"", "value (quote escaped)", String.Empty);
        internal static IntelliSenseItem StringValue = new IntelliSenseItem(QueryTokenCategory.Value, "\"<string>\"", "string value (quote escaped)", String.Empty);
        internal static IntelliSenseItem DateTimeValue = new IntelliSenseItem(QueryTokenCategory.Value, "\"<DateTime>\"", "DateTime (ex: \"2017-03-20\")", String.Empty);
        internal static IntelliSenseItem TimeSpanValue = new IntelliSenseItem(QueryTokenCategory.Value, "\"<TimeSpan>\"", "TimeSpan (ex: \"7.12:00:00\")", String.Empty);
        internal static IntelliSenseItem NumericValue = new IntelliSenseItem(QueryTokenCategory.Value, "\"<Number>\"", "numeric value", String.Empty);

        internal static List<IntelliSenseItem> BooleanValues = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.Value, "0", "false"),
            new IntelliSenseItem(QueryTokenCategory.Value, "1", "true"),
            new IntelliSenseItem(QueryTokenCategory.Value, "false", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.Value, "true", String.Empty)
        };

        internal static char[] ColumnNameCompletionCharacters = new char[] { ':', '<', '>', '=', '!' };
        #endregion

        /// <summary>
        ///  CompleteQuery takes a query before the cursor, the IntelliSenseResult, a selected IntelliSenseItem, and the
        ///  completion character and returns the proper completed query.
        ///  
        ///  It removes the token in progress, adds the 'CompleteAs' value, a space, and the non-whitespace completion character.
        /// </summary>
        /// <param name="queryBeforeCursor">Query up to the cursor position</param>
        /// <param name="result">IntelliSenseResult from GetIntelliSenseItems</param>
        /// <param name="selectedItem">IntelliSenseItem selected</param>
        /// <param name="completionCharacter">Completion Character typed</param>
        /// <returns>New Arriba Query after completion</returns>
        public string CompleteQuery(string queryBeforeCursor, IntelliSenseResult result, IntelliSenseItem selectedItem, char completionCharacter)
        {
            // If there is no completion for this item (grammar suggestions), just append the character
            if (selectedItem == null || String.IsNullOrEmpty(selectedItem.CompleteAs)) return queryBeforeCursor + completionCharacter;

            // Add the value to complete and a space to complete the value
            string newQuery = result.CurrentCompleteValue + selectedItem.CompleteAs + ' ';

            // If the completion character isn't '\t' or ' ', add the completion character as well
            if (completionCharacter != '\t' && completionCharacter != ' ') newQuery += completionCharacter;

            return newQuery;
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
            // If no tables were passed, show no IntelliSense (hint that there's an error blocking all tables)
            if (queryBeforeCursor == null || targetTables == null || targetTables.Count == 0)
            {
                return new IntelliSenseResult() { Query = queryBeforeCursor, CurrentIncompleteValue = "", CurrentCompleteValue = "", CompletionCharacters = new char[0], Suggestions = new List<IntelliSenseItem>() };
            }

            // Get grammatical categories valid after the query prefix
            bool spaceIsSafeCompletionCharacter = true;
            TermExpression lastTerm;
            IntelliSenseGuidance guidance = GetCurrentTokenOptions(queryBeforeCursor, out lastTerm);

            // Build a ranked list of suggestions - preferred token categories, filtered to the prefix already typed
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
                            selectedColumns.Add(new IntelliSenseItem(QueryTokenCategory.ColumnName, column.Name, String.Format("{0}.{1} [{2}]", table.Name, column.Name, column.Type), "[" + column.Name + "]"));

                            if (column.Name.Length > guidance.Value.Length && column.Name[guidance.Value.Length] == ' ')
                            {
                                // Space is unsafe to complete with if a suggest column has a space next in the value
                                spaceIsSafeCompletionCharacter = false;
                            }
                        }
                    }
                }

                selectedColumns.Sort((left, right) => left.Value.CompareTo(right.Value));
                suggestions.AddRange(selectedColumns);
            }

            if (guidance.Options.HasFlag(QueryTokenCategory.Value))
            {
                // Space is unsafe for value completion (except when all explicit values are listed)
                spaceIsSafeCompletionCharacter = false;

                Type columnType = FindSingleMatchingColumnType(targetTables, lastTerm);
                if (columnType == null)
                {
                    suggestions.Add(Value);
                }
                else
                {
                    if(columnType == typeof(ByteBlock))
                    {
                        suggestions.Add(StringValue);
                    }
                    else if(columnType == typeof(bool))
                    {
                        AddWhenPrefixes(BooleanValues, guidance.Value, suggestions);
                        spaceIsSafeCompletionCharacter = true;
                    }
                    else if(columnType == typeof(DateTime))
                    {
                        suggestions.Add(DateTimeValue);
                    }
                    else if(columnType == typeof(TimeSpan))
                    {
                        suggestions.Add(TimeSpanValue);
                    }
                    else
                    {
                        suggestions.Add(new IntelliSenseItem(QueryTokenCategory.Value, String.Format("<{0}>", columnType.Name), String.Empty));
                    }
                }
            }

            if (guidance.Options.HasFlag(QueryTokenCategory.TermPrefixes))
            {
                AddWhenPrefixes(TermPrefixes, guidance.Value, suggestions);
            }

            // Build a list of valid completion characters
            List<char> completionCharacters = new List<char>();
            completionCharacters.Add('\t');
            if (spaceIsSafeCompletionCharacter) completionCharacters.Add(' ');

            // If column names are valid here but term prefixes or compare operators, operator start characters are valid completion characters
            if (guidance.Options.HasFlag(QueryTokenCategory.ColumnName) && !guidance.Options.HasFlag(QueryTokenCategory.CompareOperator) && !guidance.Options.HasFlag(QueryTokenCategory.TermPrefixes))
            {
                completionCharacters.AddRange(ColumnNameCompletionCharacters);
            }

            // Compute the CurrentCompleteValue
            string queryWithoutIncompleteValue = queryBeforeCursor;
            if (!queryWithoutIncompleteValue.EndsWith(guidance.Value)) throw new ArribaException("Error: IntelliSense suggestion couldn't be applied.");
            queryWithoutIncompleteValue = queryWithoutIncompleteValue.Substring(0, queryWithoutIncompleteValue.Length - guidance.Value.Length);

            // If the CurrentIncompleteValue is an explicit column name, remove and re-complete that, also
            if (queryWithoutIncompleteValue.EndsWith("[")) queryWithoutIncompleteValue = queryWithoutIncompleteValue.Substring(0, queryWithoutIncompleteValue.Length - 1);

            return new IntelliSenseResult() { Query = queryBeforeCursor, CurrentIncompleteValue = guidance.Value, CurrentCompleteValue = queryWithoutIncompleteValue, Suggestions = suggestions, CompletionCharacters = completionCharacters };
        }

        /// <summary>
        ///  Get the grammatical categories and value being completed at the given query position.
        ///  This is the pure grammar part of IntelliSense determination.
        /// </summary>
        /// <param name="queryBeforeCursor">Query up to where the cursor is placed</param>
        /// <param name="lastTerm">The TermExpression in progress as parsed</param>
        /// <returns>IntelliSenseGuidance showing the token in progress and possible grammar categories for it</returns>
        internal IntelliSenseGuidance GetCurrentTokenOptions(string queryBeforeCursor, out TermExpression lastTerm)
        {
            lastTerm = null;
            IntelliSenseGuidance defaultGuidance = new IntelliSenseGuidance(String.Empty, QueryTokenCategory.Term);

            // If the query is empty, return the guidance for the beginning of the first term
            if (String.IsNullOrEmpty(queryBeforeCursor)) return defaultGuidance;

            // Parse the query
            IExpression query = QueryParser.Parse(queryBeforeCursor);

            // If the query had parse errors, return empty guidance
            if (query is EmptyExpression) return new IntelliSenseGuidance(String.Empty, QueryTokenCategory.None);

            // Get the last query term to look at the IntelliSense guidance
            lastTerm = query.GetLastTerm();

            // If no last term, return first term guidance (ex: inside new '('
            if (lastTerm == null) return defaultGuidance;

            // Otherwise, grab the last term guidance
            IntelliSenseGuidance guidance = lastTerm.Guidance;

            return guidance;
        }

        internal IntelliSenseGuidance GetCurrentTokenOptions(string queryBeforeCursor)
        {
            TermExpression unused;
            return GetCurrentTokenOptions(queryBeforeCursor, out unused);
        }

        private static Type FindSingleMatchingColumnType(IReadOnlyCollection<Table> targetTables, TermExpression lastTerm)
        {
            Type matchingColumnType = null;

            if (lastTerm != null && !String.IsNullOrEmpty(lastTerm.ColumnName) && lastTerm.ColumnName != "*")
            {
                foreach (Table table in targetTables)
                {
                    foreach (ColumnDetails column in table.ColumnDetails)
                    {
                        if (column.Name.Equals(lastTerm.ColumnName, StringComparison.OrdinalIgnoreCase))
                        {
                            // If there's already a match, we have multiple matches
                            if (matchingColumnType != null) return null;

                            matchingColumnType = table.GetColumnType(column.Name);
                        }
                    }
                }
            }

            return matchingColumnType;
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

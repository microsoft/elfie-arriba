// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Structures;

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
        public string Display { get; set; }

        /// <summary>
        ///  Hint Text to show in IntelliSense
        /// </summary>
        public string Hint { get; set; }

        /// <summary>
        ///  Actual value to append to query when completed
        /// </summary>
        public string CompleteAs { get; set; }

        /// <summary>
        ///  Count of items with value (when relevant)
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        ///  Overall set of items (with or without value; when relevant)
        /// </summary>
        public long Denominator { get; set; }

        public IntelliSenseItem(QueryTokenCategory category, string value, string hint) : this(category, value, hint, value)
        { }

        public IntelliSenseItem(QueryTokenCategory category, string value, long count, long denominator) : this(category, value, PercentageString(count, denominator), value)
        {
            this.Count = count;
            this.Denominator = denominator;
        }

        public IntelliSenseItem(QueryTokenCategory category, string display, string hint, string completeAs)
        {
            this.Category = category;
            this.Display = display;
            this.Hint = hint;
            this.CompleteAs = completeAs;
        }

        public static string PercentageString(long count, long total)
        {
            if (count == total || total == 0) return "all";

            double percentage = (double)count / (double)total;
            if (percentage < 0.01) return percentage.ToString("P2");
            if (percentage < 0.10) return percentage.ToString("P1");

            // Round 99% down if it's not every item (and thus "all")
            if (percentage > 0.99) percentage = 0.99;

            return percentage.ToString("P0");
        }

        public override string ToString()
        {
            return String.Format("{0} | {1} | {2} | {3}", this.Display, this.Hint, this.Category, this.CompleteAs);
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
        public string Incomplete;

        /// <summary>
        ///  The query up to the beginning of the CurrentIncompleteValue.
        ///  This is the prefix which the 'CompleteAs' value for the
        ///  selected IntelliSenseItem should be appended to.
        /// </summary>
        public string Complete;

        /// <summary>
        ///  A hint about valid syntax in the current location, to show
        ///  as a watermark after the typed-so-far query.
        /// </summary>
        public string SyntaxHint;

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
        internal static List<IntelliSenseItem> TermPrefixes = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.TermPrefixes, "!", "not"),
            new IntelliSenseItem(QueryTokenCategory.TermPrefixes, "(", "subexpression")
        };

        internal static List<IntelliSenseItem> BooleanOperators = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.BooleanOperator, "AND", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.BooleanOperator, "OR", String.Empty)
        };

        internal static List<IntelliSenseItem> CompareOperatorsForBoolean = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "=", "equals"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "!=", "not equals")
        };

        internal static List<IntelliSenseItem> CompareOperatorsForString = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, ":", "contains word prefix"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "::", "contains exact word"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "=", "equals exact case"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "!=", "not equals"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "<", "less than"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "<=", "less or equal"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, ">", "greater than"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, ">=", "greater or equal"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "|>", "starts with")
        };

        internal static List<IntelliSenseItem> CompareOperatorsForOther = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "=", "equals"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "!=", "not equals"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "<", "less than"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, "<=", "less or equal"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, ">", "greater than"),
            new IntelliSenseItem(QueryTokenCategory.CompareOperator, ">=", "greater or equal"),
        };

        internal const string Value = "\"<value>\"";
        internal const string StringValue = "\"<string>\"";
        internal const string DateTimeValue = "\"1999-12-31\"";
        internal const string TimeSpanValue = "\"6.23:59:59\"";
        internal const string FloatValue = "123.45";
        internal const string IntegerValue = "12345";

        internal const string MultipleTables = "<Multiple Tables>";

        internal static List<IntelliSenseItem> BooleanValues = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(QueryTokenCategory.Value, "False", String.Empty),
            new IntelliSenseItem(QueryTokenCategory.Value, "True", String.Empty)
        };

        internal static IntelliSenseItem AllColumnNames = new IntelliSenseItem(QueryTokenCategory.ColumnName, "[*]", "all columns");

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

            // Add the value to complete. Add a space afterward for non-values (so we don't jump to the next term). Add a space if you completed with space.
            string separator = (selectedItem.Category == QueryTokenCategory.Value && completionCharacter != ' ' ? "" : " ");
            string newQuery = result.Complete + selectedItem.CompleteAs + separator;

            // If the completion character isn't '\t' or ' ', add the completion character as well
            if (completionCharacter != '\n' && completionCharacter != '\t' && completionCharacter != ' ') newQuery += completionCharacter;

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
            IntelliSenseResult result = new IntelliSenseResult() { Query = queryBeforeCursor, Incomplete = "", Complete = "", SyntaxHint = "", CompletionCharacters = new char[0], Suggestions = new List<IntelliSenseItem>() };

            // If no tables were passed, show no IntelliSense (hint that there's an error blocking all tables)
            if (queryBeforeCursor == null || targetTables == null || targetTables.Count == 0)
            {
                return result;
            }

            // Filter the set of tables to those valid for the query so far
            targetTables = FilterToValidTablesForQuery(targetTables, queryBeforeCursor);

            // If no tables remain valid, show no IntelliSense (hint that there's an error blocking all tables)
            if (targetTables == null || targetTables.Count == 0)
            {
                return result;
            }

            // Get grammatical categories valid after the query prefix
            TermExpression lastTerm;
            IExpression query;
            IntelliSenseGuidance guidance = GetCurrentTokenOptions(queryBeforeCursor, out lastTerm, out query);
            bool spaceIsSafeCompletionCharacter = !String.IsNullOrEmpty(guidance.Value);

            // If there are no tokens suggested here, return empty completion
            if (guidance.Options == QueryTokenCategory.None)
            {
                return result;
            }

            // Compute the CurrentCompleteValue, *before* considering values, so value IntelliSense can use them
            string queryWithoutIncompleteValue = queryBeforeCursor;
            if (!queryWithoutIncompleteValue.EndsWith(guidance.Value)) throw new ArribaException("Error: IntelliSense suggestion couldn't be applied.");
            queryWithoutIncompleteValue = queryWithoutIncompleteValue.Substring(0, queryWithoutIncompleteValue.Length - guidance.Value.Length);

            // If the CurrentIncompleteValue is an explicit column name, remove and re-complete that, also
            if (queryWithoutIncompleteValue.EndsWith("[")) queryWithoutIncompleteValue = queryWithoutIncompleteValue.Substring(0, queryWithoutIncompleteValue.Length - 1);

            result.Complete = queryWithoutIncompleteValue;
            result.Incomplete = guidance.Value;

            // Build a ranked list of suggestions - preferred token categories, filtered to the prefix already typed
            List<IntelliSenseItem> suggestions = new List<IntelliSenseItem>();

            if (guidance.Options.HasFlag(QueryTokenCategory.BooleanOperator))
            {
                AddWhenPrefixes(BooleanOperators, guidance.Value, suggestions);
            }

            if (guidance.Options.HasFlag(QueryTokenCategory.CompareOperator))
            {
                AddSuggestionsForCompareOperator(targetTables, lastTerm, guidance, suggestions);
            }

            if (guidance.Options.HasFlag(QueryTokenCategory.ColumnName))
            {
                AddSuggestionsForColumnNames(targetTables, guidance, ref spaceIsSafeCompletionCharacter, suggestions);
            }

            // Space isn't safe to complete values (except when all explicit values shown, bool below)
            if (guidance.Options.HasFlag(QueryTokenCategory.Value))
            {
                spaceIsSafeCompletionCharacter = false;

                AddSuggestionsForTerm(targetTables, result, lastTerm, guidance, suggestions);
            }

            // If *only* a value is valid here, provide a syntax hint for the value type (and reconsider if space is safe to complete)
            if (guidance.Options == QueryTokenCategory.Value)
            {
                AddSuggestionsForValue(targetTables, result, lastTerm, guidance, ref spaceIsSafeCompletionCharacter, suggestions);
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

            // If there's only one suggestion and it's been fully typed, remove it
            if (suggestions.Count == 1 && suggestions[0].Display == guidance.Value) suggestions.Clear();

            // Finish populating the result
            result.Suggestions = suggestions;
            result.CompletionCharacters = completionCharacters;

            return result;
        }

        public static IReadOnlyCollection<Table> FilterToValidTablesForQuery(IReadOnlyCollection<Table> tables, string query)
        {
            // Parse the query to execute (exclude incomplete terms)
            IExpression queryToExecute = QueryParser.Parse(query);

            // If there's no query yet, all tables are valid
            if (queryToExecute == null) return tables;

            // Otherwise, find tables in which every used column name is found
            List<Table> matchingTables = new List<Table>();

            IList<TermExpression> allTerms = queryToExecute.GetAllTerms();
            foreach (Table table in tables)
            {
                bool hasAllColumns = true;

                foreach (TermExpression term in allTerms)
                {
                    if (term.ColumnName != "*" && table.GetColumnType(term.ColumnName) == null)
                    {
                        hasAllColumns = false;
                        break;
                    }
                }

                if (hasAllColumns)
                {
                    matchingTables.Add(table);
                }
            }

            return matchingTables;
        }

        private static void AddSuggestionsForCompareOperator(IReadOnlyCollection<Table> targetTables, TermExpression lastTerm, IntelliSenseGuidance guidance, List<IntelliSenseItem> suggestions)
        {
            Type columnType = null;
            Table singleTable;
            ColumnDetails singleColumn;

            if (TryFindSingleMatchingColumn(targetTables, lastTerm, out singleTable, out singleColumn))
            {
                columnType = singleTable.GetColumnType(singleColumn.Name);
            }

            if (columnType == null)
            {
                AddWhenPrefixes(CompareOperatorsForString, guidance.Value, suggestions);
            }
            else if (columnType == typeof(ByteBlock))
            {
                AddWhenPrefixes(CompareOperatorsForString, guidance.Value, suggestions);
            }
            else if (columnType == typeof(bool))
            {
                AddWhenPrefixes(CompareOperatorsForBoolean, guidance.Value, suggestions);
            }
            else
            {
                AddWhenPrefixes(CompareOperatorsForOther, guidance.Value, suggestions);
            }
        }

        private static void AddSuggestionsForColumnNames(IReadOnlyCollection<Table> targetTables, IntelliSenseGuidance guidance, ref bool spaceIsSafeCompletionCharacter, List<IntelliSenseItem> suggestions)
        {
            List<IntelliSenseItem> selectedColumns = new List<IntelliSenseItem>();

            foreach (Table table in targetTables)
            {
                foreach (ColumnDetails column in table.ColumnDetails)
                {
                    if (column.Name.StartsWith(guidance.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        // Add the matching column
                        // Hack: Set the Display value to the bare column name and CompleteAs to the wrapped [ColumnName], so sort order is right
                        selectedColumns.Add(new IntelliSenseItem(QueryTokenCategory.ColumnName, column.Name, String.Format("{0}, {1}", column.Type, table.Name), "[" + column.Name + "]"));

                        if (column.Name.Length > guidance.Value.Length && column.Name[guidance.Value.Length] == ' ')
                        {
                            // Space is unsafe to complete with if a suggest column has a space next in the value
                            spaceIsSafeCompletionCharacter = false;
                        }
                    }
                }
            }

            // Sort selected columns alphabetically *by bare column name*, so [Count] will be before [Count of Options].
            selectedColumns.Sort((left, right) => left.Display.CompareTo(right.Display));

            // Unhack: Set the display value back to the wrapped [ColumnName]
            foreach (IntelliSenseItem item in selectedColumns)
            {
                item.Display = item.CompleteAs;
            }

            // Remove entries where the same column name was suggested by multiple tables (set the hint to '<Multiple Tables>')
            for (int i = 1; i < selectedColumns.Count; ++i)
            {
                if (selectedColumns[i - 1].Display == selectedColumns[i].Display)
                {
                    selectedColumns[i - 1].Hint = MultipleTables;
                    selectedColumns.RemoveAt(i);
                    --i;
                }
            }

            // Add 'All Columns' hint (last, only if nothing or '*' typed so far)
            if ("*".StartsWith(guidance.Value))
            {
                selectedColumns.Add(AllColumnNames);
            }

            // Add results to IntelliSense suggestions
            suggestions.AddRange(selectedColumns);
        }

        private static void AddSuggestionsForValue(IReadOnlyCollection<Table> targetTables, IntelliSenseResult result, TermExpression lastTerm, IntelliSenseGuidance guidance, ref bool spaceIsSafeCompletionCharacter, List<IntelliSenseItem> suggestions)
        {
            Table singleTable;
            ColumnDetails singleColumn;

            if (!TryFindSingleMatchingColumn(targetTables, lastTerm, out singleTable, out singleColumn))
            {
                // If more than one table has the column, we can't recommend anything
                result.SyntaxHint = Value;
            }
            else
            {
                if (lastTerm.Operator == Operator.Equals || lastTerm.Operator == Operator.NotEquals || lastTerm.Operator == Operator.Matches || lastTerm.Operator == Operator.MatchesExact)
                {
                    AddTopColumnValues(result, lastTerm, guidance, suggestions, singleTable, singleColumn);
                }
                else if (lastTerm.Operator == Operator.LessThan || lastTerm.Operator == Operator.LessThanOrEqual || lastTerm.Operator == Operator.GreaterThan || lastTerm.Operator == Operator.GreaterThanOrEqual)
                {
                    AddValueDistribution(result, lastTerm, guidance, suggestions, singleTable, singleColumn);
                }

                Type columnType = singleTable.GetColumnType(singleColumn.Name);

                if (columnType == typeof(ByteBlock))
                {
                    result.SyntaxHint = StringValue;
                }
                else if (columnType == typeof(bool))
                {
                    if (suggestions.Count == 0) AddWhenPrefixes(BooleanValues, guidance.Value, suggestions);
                    spaceIsSafeCompletionCharacter = true;
                }
                else if (columnType == typeof(DateTime))
                {
                    result.SyntaxHint = DateTimeValue;
                }
                else if (columnType == typeof(TimeSpan))
                {
                    result.SyntaxHint = TimeSpanValue;
                }
                else if (columnType == typeof(float) || columnType == typeof(double))
                {
                    result.SyntaxHint = FloatValue;
                }
                else if (columnType == typeof(byte) || columnType == typeof(sbyte) || columnType == typeof(short) || columnType == typeof(ushort) || columnType == typeof(int) || columnType == typeof(uint) || columnType == typeof(long) || columnType == typeof(ulong))
                {
                    result.SyntaxHint = IntegerValue;
                }
                else
                {
                    result.SyntaxHint = String.Format("<{0}>", columnType.Name);
                }
            }
        }

        private static void AddTopColumnValues(IntelliSenseResult result, TermExpression lastTerm, IntelliSenseGuidance guidance, List<IntelliSenseItem> suggestions, Table singleTable, ColumnDetails singleColumn)
        {
            string completeQuery = GetCompleteQueryPrefix(result);

            // Recommend the top ten values in the column with the prefix typed so far
            DistinctResult topValues = singleTable.Query(new DistinctQueryTop(singleColumn.Name, lastTerm.Value.ToString(), completeQuery, 10));
            int total = (int)topValues.Total;
            if (topValues.Total == 0) return;

            // Walk values in order for ==, :, ::, backwards with inverse percentages for !=
            bool isNotEquals = lastTerm.Operator == Operator.NotEquals;
            int start = isNotEquals ? topValues.Values.RowCount - 1 : 0;
            int end = isNotEquals ? -1 : topValues.Values.RowCount;
            int step = isNotEquals ? -1 : 1;

            for (int i = start; i != end; i += step)
            {
                string value = topValues.Values[i, 0].ToString();
                int countForValue = (int)topValues.Values[i, 1];
                if (isNotEquals) countForValue = (int)topValues.Total - countForValue;

                 if ((countForValue > 1 || total <= 10) && value.StartsWith(guidance.Value, StringComparison.OrdinalIgnoreCase) && value.Length < 100)
                {
                    suggestions.Add(new IntelliSenseItem(QueryTokenCategory.Value, QueryParser.WrapValue(topValues.Values[i, 0]), countForValue, topValues.Total));
                }
            }
        }

        private static void AddValueDistribution(IntelliSenseResult result, TermExpression lastTerm, IntelliSenseGuidance guidance, List<IntelliSenseItem> suggestions, Table singleTable, ColumnDetails singleColumn)
        {
            string completeQuery = GetCompleteQueryPrefix(result);

            bool inclusive = (lastTerm.Operator == Operator.LessThanOrEqual || lastTerm.Operator == Operator.GreaterThan);
            bool reverse = (lastTerm.Operator == Operator.GreaterThan || lastTerm.Operator == Operator.GreaterThanOrEqual);

            // Recommend the top ten values in the column with the prefix typed so far
            DataBlockResult distribution = singleTable.Query(new DistributionQuery(singleColumn.Name, completeQuery, inclusive));
            if (distribution.Total == 0 || distribution.Details.Succeeded == false) return;

            int countSoFar;
            if (reverse)
            {
                countSoFar = (int)distribution.Values[distribution.Values.RowCount - 1, 1];

                for (int i = distribution.Values.RowCount - 2; i >= 0; --i)
                {
                    string value = distribution.Values[i, 0].ToString();
                    double frequency = (double)countSoFar / (double)(distribution.Total);
                    int countForRange = (int)distribution.Values[i, 1];

                    if ((distribution.Values.RowCount == 2 || (int)distribution.Values[i + 1, 1] > 0) && value.StartsWith(guidance.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        suggestions.Add(new IntelliSenseItem(QueryTokenCategory.Value, QueryParser.WrapValue(distribution.Values[i, 0]), countSoFar, distribution.Total));
                    }

                    countSoFar += countForRange;
                }
            }
            else
            {
                countSoFar = 0;

                for (int i = 0; i < distribution.Values.RowCount - 1; ++i)
                {
                    string value = distribution.Values[i, 0].ToString();
                    int countForRange = (int)distribution.Values[i, 1];
                    countSoFar += countForRange;

                    double frequency = (double)countSoFar / (double)(distribution.Total);

                    if ((distribution.Values.RowCount == 2 || countForRange > 0) && value.StartsWith(guidance.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        suggestions.Add(new IntelliSenseItem(QueryTokenCategory.Value, QueryParser.WrapValue(distribution.Values[i, 0]), countSoFar, distribution.Total));
                    }
                }
            }
        }

        private static void AddSuggestionsForTerm(IReadOnlyCollection<Table> targetTables, IntelliSenseResult result, TermExpression lastTerm, IntelliSenseGuidance guidance, List<IntelliSenseItem> suggestions)
        {
            if (lastTerm != null && lastTerm.ColumnName == "*")
            {
                object termValue = lastTerm.Value;
                List<Tuple<string, int, int>> columnsForTerm = new List<Tuple<string, int, int>>();

                foreach (Table table in targetTables)
                {
                    DataBlockResult columns = table.Query(new TermInColumnsQuery(termValue.ToString(), GetCompleteQueryPrefix(result)));
                    for (int i = 0; i < columns.Values.RowCount; ++i)
                    {
                        columnsForTerm.Add(new Tuple<string, int, int>((string)columns.Values[i, 0], (int)columns.Values[i, 1], (int)columns.Total));
                    }
                }

                // Sort overall set by frequency descending
                columnsForTerm.Sort((left, right) => ((double)right.Item2 / (double)right.Item3).CompareTo((double)left.Item2 / (double)left.Item3));

                // Add top 10 suggestions
                int countToReturn = Math.Min(10, columnsForTerm.Count);
                for (int i = 0; i < countToReturn; ++i)
                {
                    suggestions.Add(new IntelliSenseItem(QueryTokenCategory.ColumnName, QueryParser.WrapColumnName(columnsForTerm[i].Item1) + " : " + QueryParser.WrapValue(termValue), columnsForTerm[i].Item2, columnsForTerm[i].Item3));
                }
            }
        }

        /// <summary>
        ///  Get the grammatical categories and value being completed at the given query position.
        ///  This is the pure grammar part of IntelliSense determination.
        /// </summary>
        /// <param name="queryBeforeCursor">Query up to where the cursor is placed</param>
        /// <param name="lastTerm">The TermExpression in progress as parsed</param>
        /// <returns>IntelliSenseGuidance showing the token in progress and possible grammar categories for it</returns>
        internal IntelliSenseGuidance GetCurrentTokenOptions(string queryBeforeCursor, out TermExpression lastTerm, out IExpression query)
        {
            lastTerm = null;
            query = null;
            IntelliSenseGuidance defaultGuidance = new IntelliSenseGuidance(String.Empty, QueryTokenCategory.Term);

            // If the query is empty, return the guidance for the beginning of the first term
            if (String.IsNullOrEmpty(queryBeforeCursor)) return defaultGuidance;

            // Parse the query, asking for hint terms
            query = QueryParser.Parse(queryBeforeCursor, true);

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
            TermExpression unusedTerm;
            IExpression unusedQuery;
            return GetCurrentTokenOptions(queryBeforeCursor, out unusedTerm, out unusedQuery);
        }

        private static string GetCompleteQueryPrefix(IntelliSenseResult result)
        {
            // Lame, to turn single terms into AllQuery [normally they return nothing]
            return QueryParser.Parse(result.Complete).ToString();
        }

        private static bool TryFindSingleMatchingColumn(IReadOnlyCollection<Table> targetTables, TermExpression lastTerm, out Table matchTable, out ColumnDetails matchColumn)
        {
            matchTable = null;
            matchColumn = null;
            if (lastTerm == null || String.IsNullOrEmpty(lastTerm.ColumnName) || lastTerm.ColumnName == "*") return false;

            foreach (Table table in targetTables)
            {
                foreach (ColumnDetails column in table.ColumnDetails)
                {
                    if (column.Name.Equals(lastTerm.ColumnName, StringComparison.OrdinalIgnoreCase))
                    {
                        // If there's already a match, we have multiple matches
                        if (matchColumn != null)
                        {
                            matchTable = null;
                            matchColumn = null;
                            return false;
                        }

                        matchTable = table;
                        matchColumn = column;
                    }
                }
            }

            return matchColumn != null;
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
                    if (item.Display.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        resultSet.Add(item);
                    }
                }
            }
        }
    }
}

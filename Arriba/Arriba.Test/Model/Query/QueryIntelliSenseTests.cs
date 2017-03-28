// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Query;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Arriba.Test.Model.Query
{
    [TestClass]
    public class QueryIntelliSenseProviderTests
    {
        private Table College { get; set; }
        private Table Student { get; set; }
        private List<Table> Tables { get; set; }

        public QueryIntelliSenseProviderTests()
        {
            College = new Table();
            College.Name = "College";
            College.AddColumn(new ColumnDetails("ID", "int", -1));
            College.AddColumn(new ColumnDetails("Name", "string", null));
            College.AddColumn(new ColumnDetails("WhenFounded", "DateTime", null));
            College.AddColumn(new ColumnDetails("SchoolYearLength", "TimeSpan", null));
            College.AddColumn(new ColumnDetails("SchoolHasMascot", "bool", null));
            College.AddColumn(new ColumnDetails("Student Count", "long", -1));

            Student = new Table();
            Student.Name = "Student";
            Student.AddColumn(new ColumnDetails("ID", "guid", null));
            Student.AddColumn(new ColumnDetails("Name", "stringset", null));
            Student.AddColumn(new ColumnDetails("City", "string", null));
            Student.AddColumn(new ColumnDetails("Age", "TimeSpan", null));

            Tables = new List<Table>();
            Tables.Add(College);
            Tables.Add(Student);
        }

        [TestMethod]
        public void QueryIntelliSense_CompleteQuery()
        {
            // Complete everything with Tab
            Assert.AreEqual("[ID] < 15 AND [WhenFounded] > \"1900-01-01\" ([SchoolYearLength] = 18)", CompleteEachKeystroke("I\t<\t15 AN\t[Whe\t>\t\"1900-01-01\" (SchoolY\t=\t18)"));

            // Complete with spaces where safe
            Assert.AreEqual("[SchoolHasMascot] = true AND [WhenFounded] > \"  \"", CompleteEachKeystroke("[SchoolH = tr AND [When > \"  \""));

            // No space completion when unsafe - column name has a space next, bare values, values without all values in IntelliSense
            Assert.AreEqual("[Student ", CompleteEachKeystroke("[Student "));
            Assert.AreEqual("[Student Count] ", CompleteEachKeystroke("[Student  "));
            Assert.AreEqual("Na ", CompleteEachKeystroke("Na "));
            Assert.AreEqual("City = Va ", CompleteEachKeystroke("City = Va "));

            // Complete with operators
            Assert.AreEqual("[SchoolYearLength] > ", CompleteEachKeystroke("[SchoolY> "));
        }

        private string CompleteEachKeystroke(string fullQuery)
        {
            QueryIntelliSense qi = new QueryIntelliSense();
            string query = "";
            IntelliSenseResult result = qi.GetIntelliSenseItems(query, this.Tables);

            foreach (char c in fullQuery)
            {
                if (result.CompletionCharacters.Contains(c))
                {
                    query = qi.CompleteQuery(query, result, (result.Suggestions.Count > 0 ? result.Suggestions[0] : null), c);
                }
                else
                {
                    query += c;
                }

                result = qi.GetIntelliSenseItems(query, this.Tables);
            }

            return query;
        }

        [TestMethod]
        public void QueryIntelliSense_GetIntelliSenseItems_Basic()
        {
            QueryIntelliSense qi = new QueryIntelliSense();
            IntelliSenseResult result;

            // ""Name" = "Na" suggests nothing (invalid query)
            result = qi.GetIntelliSenseItems("\"Name\" = \"Na", Tables);
            Assert.AreEqual(0, result.Suggestions.Count);

            // No Query: ColumnNames, alphabetical, with no duplicates, then bare value, then TermPrefixes
            string allColumnNamesOrTerm = "[Age], [City], [ID], [Name], [SchoolHasMascot], [SchoolYearLength], [Student Count], [WhenFounded], [*], !, (";
            result = qi.GetIntelliSenseItems("", Tables);
            Assert.AreEqual(allColumnNamesOrTerm, string.Join(", ", result.Suggestions.Select(ii => ii.Display)));
            Assert.AreEqual("", result.SyntaxHint);
            Assert.AreEqual("", result.Incomplete);
            Assert.AreEqual("", result.Complete);
            Assert.AreEqual("", result.Query);

            // No Query, one table: ColumnNames for single table, then bare value, then TermPrefixes
            string studentTableNamesOrTerm = "[Age], [City], [ID], [Name], [*], !, (";
            result = qi.GetIntelliSenseItems("", new List<Table>() { Student });
            Assert.AreEqual(studentTableNamesOrTerm, string.Join(", ", result.Suggestions.Select(ii => ii.Display)));
            Assert.AreEqual("", result.SyntaxHint);
            Assert.AreEqual("", result.Incomplete);
            Assert.AreEqual("", result.Complete);
            Assert.AreEqual("", result.Query);

            // No Query, no tables: no response
            result = qi.GetIntelliSenseItems(null, Tables);
            Assert.AreEqual(0, result.Suggestions.Count);
            result = qi.GetIntelliSenseItems("", new List<Table>() { });
            Assert.AreEqual(0, result.Suggestions.Count);
            result = qi.GetIntelliSenseItems("", null);
            Assert.AreEqual(0, result.Suggestions.Count);

            // "Age > 10 AND SchoolHasMascot = true AND " suggests nothing (no tables have both columns)
            result = qi.GetIntelliSenseItems("Age > 10 AND SchoolHasMascot = true AND ", new List<Table>() { Student });
            Assert.AreEqual(0, result.Suggestions.Count);

            // "Name: hey AND " suggests all table columns (no tables excludable yet)
            result = qi.GetIntelliSenseItems("Name: hey AND ", Tables);
            Assert.AreEqual(allColumnNamesOrTerm, string.Join(", ", result.Suggestions.Select(ii => ii.Display)));
            Assert.AreEqual("", result.Incomplete);

            // "Age > 10 AND (" suggests student table columns only (no last term)
            result = qi.GetIntelliSenseItems("Age > 10 AND ( ", Tables);
            Assert.AreEqual(studentTableNamesOrTerm, string.Join(", ", result.Suggestions.Select(ii => ii.Display)));
            Assert.AreEqual("Age > 10 AND ( ", result.Complete);
            Assert.AreEqual("", result.Incomplete);

            // "* : 10 AND " suggests all column only ('*' doesn't filter anything)
            result = qi.GetIntelliSenseItems("* : 10 AND ", Tables);
            Assert.AreEqual(allColumnNamesOrTerm, string.Join(", ", result.Suggestions.Select(ii => ii.Display)));

            // "[Na" must be a column name, and one of the 'Name' ones
            // CurrentIncompleteValue is just the bare column name (no '[') for list filtering, but CurrentCompleteValue is "", so the "[" is replaced by the completion.
            result = qi.GetIntelliSenseItems("[Na", Tables);
            Assert.AreEqual("[Name]", string.Join(", ", result.Suggestions.Select(ii => ii.Display)));
            Assert.AreEqual("[Name] | <Multiple Tables> | ColumnName | [Name]", string.Join(", ", result.Suggestions));
            Assert.AreEqual("Na", result.Incomplete);
            Assert.AreEqual("", result.Complete); 
            Assert.AreEqual("[Na", result.Query);

            // "[SchoolSumm" should suggest nothing (no remaining column names)
            result = qi.GetIntelliSenseItems("[SchoolSumm", Tables);
            Assert.AreEqual("", string.Join(", ", result.Suggestions.Select(ii => ii.Display)));

            // "[Name] " should suggest operators for string
            result = qi.GetIntelliSenseItems("[Name] ", Tables);
            Assert.AreEqual(string.Join(", ", QueryIntelliSense.CompareOperatorsForString.Select(ii => ii.Display)), string.Join(", ", result.Suggestions.Select(ii => ii.Display)));
            Assert.AreEqual("", result.Incomplete);
            Assert.AreEqual("[Name] ", result.Query);

            // "[Name] = " should suggest value but doesn't know the type (multiple matches)
            result = qi.GetIntelliSenseItems("[Name] = ", Tables);
            Assert.AreEqual(QueryIntelliSense.Value, result.SyntaxHint);
            Assert.AreEqual("", result.Incomplete);

            // "[Student " should match 'Student Count' (space in column filtering is correct)
            result = qi.GetIntelliSenseItems("[Student ", Tables);
            Assert.AreEqual("[Student Count]", string.Join(", ", result.Suggestions.Select(ii => ii.Display)));
            Assert.AreEqual("Student ", result.Incomplete);
            
            // "[Student  " should not match 'Student Count' (second space means non-match)
            result = qi.GetIntelliSenseItems("[Student  ", Tables);
            Assert.AreEqual("", string.Join(", ", result.Suggestions.Select(ii => ii.Display)));
            Assert.AreEqual("Student  ", result.Incomplete);

            // "[Student Count]" should suggest operators for numbers
            result = qi.GetIntelliSenseItems("[Student Count]", Tables);
            Assert.AreEqual(string.Join(", ", QueryIntelliSense.CompareOperatorsForOther.Select(ii => ii.Display)), string.Join(", ", result.Suggestions.Select(ii => ii.Display)));

            // "Age > " suggests TimeSpans
            result = qi.GetIntelliSenseItems("Age > ", Tables);
            Assert.AreEqual(QueryIntelliSense.TimeSpanValue, result.SyntaxHint);

            // "WhenFounded > " suggests DateTimes
            result = qi.GetIntelliSenseItems("WhenFounded <= ", Tables);
            Assert.AreEqual(QueryIntelliSense.DateTimeValue, result.SyntaxHint);
            Assert.AreEqual("WhenFounded <= ", result.Complete);
            Assert.AreEqual("", result.Incomplete);

            // "[SchoolHasMascot] " suggests boolean operators
            result = qi.GetIntelliSenseItems("[SchoolHasMascot] ", Tables);
            Assert.AreEqual(string.Join(", ", QueryIntelliSense.CompareOperatorsForBoolean.Select(ii => ii.Display)), string.Join(", ", result.Suggestions.Select(ii => ii.Display)));

            // "SchoolHasMascot = " suggests booleans
            result = qi.GetIntelliSenseItems("SchoolHasMascot = ", Tables);
            Assert.AreEqual(string.Join(", ", QueryIntelliSense.BooleanValues.Select(ii => ii.Display)), string.Join(", ", result.Suggestions.Select(ii => ii.Display)));

            // "City : " suggests string
            result = qi.GetIntelliSenseItems("City : ", Tables);
            Assert.AreEqual(QueryIntelliSense.StringValue, result.SyntaxHint);

            // "[Student Count] < " suggests that type
            result = qi.GetIntelliSenseItems("[Student Count] < ", Tables);
            Assert.AreEqual(QueryIntelliSense.IntegerValue, result.SyntaxHint);

            // "[Student Count] < 7000 " suggests boolean, columns, value
            result = qi.GetIntelliSenseItems("[Student Count] < 7000 ", Tables);
            Assert.AreEqual(string.Join(", ", QueryIntelliSense.BooleanOperators.Select(ii => ii.Display)), string.Join(", ", result.Suggestions.Where(ii => ii.Category == QueryTokenCategory.BooleanOperator).Select(ii => ii.Display)));

            // "[Student Count] < 7000 &&" suggests column, value, term prefix
            string collegeNamesOrTerm = "[ID], [Name], [SchoolHasMascot], [SchoolYearLength], [Student Count], [WhenFounded], [*], !, (";
            result = qi.GetIntelliSenseItems("[Student Count] < 7000 &&", Tables);
            Assert.AreEqual(collegeNamesOrTerm, string.Join(", ", result.Suggestions.Select(ii => ii.Display)));

            // "[Name] : Hey A" shows both "AND", "Age" (either valid at this point)
            result = qi.GetIntelliSenseItems("[Name] : Hey A", Tables);
            Assert.AreEqual("AND, [Age], \"A\"", string.Join(", ", result.Suggestions.Select(ii => ii.Display)));
        }

        [TestMethod]
        public void QueryIntelliSense_TokenOptionWalkthrough()
        {
            QueryIntelliSense qi = new QueryIntelliSense();

            // This walkthrough checks what IntelliSense thinks while fully typing the query:
            //    "BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND \"Target \""
            //  It covers:
            //   - BareValue distinguished between column name (after operator) or values (after space, next boolean operator)
            //   - ":" still suggests operator because it could be "::"
            //   - Value distinguished as value (unambiguous because of operator beforehand) until space)
            //   - "AnotherValue" could be "AND" until "O"
            //   - "!" could be "!=" until "("
            //   - "(" starts new term but no leading boolean operator is valid
            //   - Explicit Column names while unterminated, right after terminator
            //   - "==" still suggests operator after "=" because it could still be "=="
            //   - Explicit Value while unterminated, right after terminator
            //   - Nothing suggested until space typed after quoted value
            //   - "||" still suggests boolean operator after "|" because it could be "||"
            //   - ")" terminates previous value
            //   - "AND" still suggests column name or value until complete with space
            //   - Explicit value without column name beforehand

            Assert.AreEqual("[] [Term]", qi.GetCurrentTokenOptions("").ToString());
            Assert.AreEqual("[BareValue] [ColumnName, Value]", qi.GetCurrentTokenOptions("BareValue").ToString());
            Assert.AreEqual("[] [BooleanOperator, CompareOperator, Term]", qi.GetCurrentTokenOptions("BareValue ").ToString());
            Assert.AreEqual("[:] [CompareOperator]", qi.GetCurrentTokenOptions("BareValue :").ToString());
            Assert.AreEqual("[] [Value]", qi.GetCurrentTokenOptions("BareValue : ").ToString());
            Assert.AreEqual("[V] [Value]", qi.GetCurrentTokenOptions("BareValue : V").ToString());
            Assert.AreEqual("[Value] [Value]", qi.GetCurrentTokenOptions("BareValue : Value").ToString());
            Assert.AreEqual("[] [BooleanOperator, Term]", qi.GetCurrentTokenOptions("BareValue : Value ").ToString());
            Assert.AreEqual("[An] [BooleanOperator, ColumnName, Value]", qi.GetCurrentTokenOptions("BareValue : Value An").ToString());
            Assert.AreEqual("[AnotherValue] [BooleanOperator, ColumnName, Value]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue").ToString());
            Assert.AreEqual("[] [BooleanOperator, CompareOperator, Term]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue ").ToString());
            Assert.AreEqual("[!] [BooleanOperator, CompareOperator]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !").ToString());
            Assert.AreEqual("[] [Term]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !(").ToString());
            Assert.AreEqual("[] [ColumnName]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([").ToString());
            Assert.AreEqual("[Analyzer] [ColumnName]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer").ToString());
            Assert.AreEqual("[] [CompareOperator]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer]").ToString());
            Assert.AreEqual("[] [CompareOperator]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] ").ToString());
            Assert.AreEqual("[=] [CompareOperator]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] =").ToString());
            Assert.AreEqual("[==] [CompareOperator]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] ==").ToString());
            Assert.AreEqual("[] [Value]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == ").ToString());
            Assert.AreEqual("[] [Value]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"").ToString());
            Assert.AreEqual("[tr] [Value]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"tr").ToString());
            Assert.AreEqual("[] [None]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\"").ToString());
            Assert.AreEqual("[] [BooleanOperator, Term]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" ").ToString());
            Assert.AreEqual("[|] [BooleanOperator]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" |").ToString());
            Assert.AreEqual("[] [Term]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" ||").ToString());
            Assert.AreEqual("[] [Term]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || ").ToString());
            Assert.AreEqual("[Some] [ColumnName, Value]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Some").ToString());
            Assert.AreEqual("[] [BooleanOperator, Term]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something)").ToString());
            Assert.AreEqual("[AN] [BooleanOperator, ColumnName, Value]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AN").ToString());
            Assert.AreEqual("[AND] [BooleanOperator, Term]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND").ToString());
            Assert.AreEqual("[] [Term]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND ").ToString());
            Assert.AreEqual("[] [Value]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND \"").ToString());
            Assert.AreEqual("[Target] [Value]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND \"Target").ToString());
            Assert.AreEqual("[Target ] [Value]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND \"Target ").ToString());
            Assert.AreEqual("[] [None]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND \"Target \"").ToString());
            Assert.AreEqual("[] [BooleanOperator, Term]", qi.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND \"Target \" ").ToString());
        }

        [TestMethod]
        public void QueryIntelliSense_TokenOptionBasics()
        {
            QueryIntelliSense p = new QueryIntelliSense();

            // "[An" -> ColumnName only [explicit column name escaping]
            Assert.AreEqual("[An] [ColumnName]", p.GetCurrentTokenOptions("[An").ToString());

            // "\"An" -> Value only [explicit value escaping]
            Assert.AreEqual("[An] [Value]", p.GetCurrentTokenOptions("\"An").ToString());

            // "[Analyzer]" -> CompareOperator [column name closed by ']']
            Assert.AreEqual("[] [CompareOperator]", p.GetCurrentTokenOptions("[Analyzer]").ToString());
            Assert.AreEqual("[] [CompareOperator]", p.GetCurrentTokenOptions("[Analyzer] ").ToString());

            // "\"Analysis\"" -> BooleanOperator, Term [a bare term explicitly closed, *only after space*]
            Assert.AreEqual("[] [None]", p.GetCurrentTokenOptions("\"Analysis\"").ToString());
            Assert.AreEqual("[] [BooleanOperator, Term]", p.GetCurrentTokenOptions("\"Analysis\" ").ToString());

            // "Analyzer:" -> Operator [suggest operators until space afterward]
            Assert.AreEqual("[:] [CompareOperator]", p.GetCurrentTokenOptions("Analyzer:").ToString());
            Assert.AreEqual("[::] [CompareOperator]", p.GetCurrentTokenOptions("Analyzer::").ToString());
            Assert.AreEqual("[] [Value]", p.GetCurrentTokenOptions("Analyzer: ").ToString());
            Assert.AreEqual("[V] [Value]", p.GetCurrentTokenOptions("Analyzer:V").ToString());

            // Multi-character operator prefixes *right at end* should still suggest operators
            // "Analyzer >" -> CompareOperator
            Assert.AreEqual("[>] [CompareOperator]", p.GetCurrentTokenOptions("Analyzer >").ToString());
            Assert.AreEqual("[>] [CompareOperator]", p.GetCurrentTokenOptions("[Analyzer] >").ToString());

            Assert.AreEqual("[!] [BooleanOperator, CompareOperator]", p.GetCurrentTokenOptions("Analyzer !").ToString());
            Assert.AreEqual("[!] [CompareOperator]", p.GetCurrentTokenOptions("[Analyzer] !").ToString());

            Assert.AreEqual("[|] [BooleanOperator, CompareOperator]", p.GetCurrentTokenOptions("Analyzer |").ToString());
            Assert.AreEqual("[|] [CompareOperator]", p.GetCurrentTokenOptions("[Analyzer] |").ToString());

            Assert.AreEqual("[&] [BooleanOperator]", p.GetCurrentTokenOptions("Analyzer &").ToString());
            Assert.AreEqual("[&] [BooleanOperator]", p.GetCurrentTokenOptions("[Analyzer] &").ToString());

            // Multi-character operator prefix with trailing space means it's not a comparison.
            Assert.AreEqual("[] [Value]", p.GetCurrentTokenOptions("Analyzer > ").ToString());
            Assert.AreEqual("[] [Value]", p.GetCurrentTokenOptions("[Analyzer] > ").ToString());

            // Trailing '|' will be parsed as 'OR' but no new term to indicate situation
            Assert.AreEqual("[] [Term]", p.GetCurrentTokenOptions("Analyzer | ").ToString());
            Assert.AreEqual("[] [Term]", p.GetCurrentTokenOptions("[Analyzer] | ").ToString());

            // Trailing '!' is a negation on the next term. Should suggest term starts
            Assert.AreEqual("[] [Term]", p.GetCurrentTokenOptions("Analyzer ! ").ToString());
            Assert.AreEqual("[] [Term]", p.GetCurrentTokenOptions("[Analyzer] ! ").ToString());

            // Trailing '&' will be parsed as 'AND' but no new term to indicate situation
            Assert.AreEqual("[] [Term]", p.GetCurrentTokenOptions("Analyzer & ").ToString());
            Assert.AreEqual("[] [Term]", p.GetCurrentTokenOptions("[Analyzer] & ").ToString());

            // Explicit column name without operator or value turns into [Column] != ""
            Assert.AreEqual("[BareTerm] [BooleanOperator, ColumnName, Value]", p.GetCurrentTokenOptions("[Analyzer] BareTerm").ToString());
            Assert.AreEqual("[] [None]", p.GetCurrentTokenOptions("[Analyzer] \"QuotedValue\"").ToString());
            Assert.AreEqual("[] [BooleanOperator, Term]", p.GetCurrentTokenOptions("[Analyzer] \"QuotedValue\" ").ToString());

            // Invalid Queries
            Assert.AreEqual("[] [None]", p.GetCurrentTokenOptions("\"Analysis\"=\"Interesting\"").ToString());
        }
    }
}

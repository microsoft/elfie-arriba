// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Model.Query;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model.Query
{
    [TestClass]
    public class QueryIntelliSenseProviderTests
    {
        [TestMethod]
        public void QueryIntelliSense_TokenOptionWalkthrough()
        {
            QueryIntelliSense p = new QueryIntelliSense();

            // This walkthrough checks what IntelliSense thinks while fully typing the query:
            //    "BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND \"Target\""
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
            //   - "||" still suggests boolean operator after "|" because it could be "||"
            //   - ")" terminates previous value
            //   - "AND" still suggests column name or value until complete with space
            //   - Explicit value without column name beforehand

            Assert.AreEqual("[] [ColumnName, Value]", p.GetCurrentTokenOptions("").ToString());
            Assert.AreEqual("[BareValue] [ColumnName, Value]", p.GetCurrentTokenOptions("BareValue").ToString());
            Assert.AreEqual("[] [BooleanOperator, CompareOperator, Term]", p.GetCurrentTokenOptions("BareValue ").ToString());
            Assert.AreEqual("[:] [CompareOperator]", p.GetCurrentTokenOptions("BareValue :").ToString());
            Assert.AreEqual("[] [Value]", p.GetCurrentTokenOptions("BareValue : ").ToString());
            Assert.AreEqual("[V] [Value]", p.GetCurrentTokenOptions("BareValue : V").ToString());
            Assert.AreEqual("[Value] [Value]", p.GetCurrentTokenOptions("BareValue : Value").ToString());
            Assert.AreEqual("[] [BooleanOperator, Term]", p.GetCurrentTokenOptions("BareValue : Value ").ToString());
            Assert.AreEqual("[An] [BooleanOperator, ColumnName, Value]", p.GetCurrentTokenOptions("BareValue : Value An").ToString());
            Assert.AreEqual("[AnotherValue] [BooleanOperator, ColumnName, Value]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue").ToString());
            Assert.AreEqual("[] [BooleanOperator, CompareOperator, Term]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue ").ToString());
            Assert.AreEqual("[!] [BooleanOperator, CompareOperator]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !").ToString());
            Assert.AreEqual("[] [Term]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !(").ToString());
            Assert.AreEqual("[] [ColumnName]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([").ToString());
            Assert.AreEqual("[Analyzer] [ColumnName]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer").ToString());
            Assert.AreEqual("[] [CompareOperator]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer]").ToString());
            Assert.AreEqual("[] [CompareOperator]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] ").ToString());
            Assert.AreEqual("[=] [CompareOperator]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] =").ToString());
            Assert.AreEqual("[==] [CompareOperator]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] ==").ToString());
            Assert.AreEqual("[] [Value]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == ").ToString());
            Assert.AreEqual("[] [Value]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"").ToString());
            Assert.AreEqual("[tr] [Value]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"tr").ToString());
            Assert.AreEqual("[] [BooleanOperator, Term]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\"").ToString());
            Assert.AreEqual("[|] [BooleanOperator]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" |").ToString());
            Assert.AreEqual("[] [Term]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" ||").ToString());
            Assert.AreEqual("[] [Term]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || ").ToString());
            Assert.AreEqual("[Some] [ColumnName, Value]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Some").ToString());
            Assert.AreEqual("[] [BooleanOperator, Term]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something)").ToString());
            Assert.AreEqual("[AN] [BooleanOperator, ColumnName, Value]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AN").ToString());
            Assert.AreEqual("[AND] [BooleanOperator, Term]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND").ToString());
            Assert.AreEqual("[] [Term]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND ").ToString());
            Assert.AreEqual("[] [Value]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND \"").ToString());
            Assert.AreEqual("[Target] [Value]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND \"Target").ToString());
            Assert.AreEqual("[] [BooleanOperator, Term]", p.GetCurrentTokenOptions("BareValue : Value AnotherValue !([Analyzer] == \"true\" || Something) AND \"Target\"").ToString());
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

            // "\"Analysis\"" -> BooleanOperator, ColumnName, Value [a bare term explicitly closed]
            Assert.AreEqual("[] [BooleanOperator, Term]", p.GetCurrentTokenOptions("\"Analysis\"").ToString());
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
            Assert.AreEqual("[] [BooleanOperator, Term]", p.GetCurrentTokenOptions("[Analyzer] \"QuotedValue\"").ToString());

            // Invalid Queries
            Assert.AreEqual("[] [None]", p.GetCurrentTokenOptions("\"Analysis\"=\"Interesting\"").ToString());
        }
    }
}

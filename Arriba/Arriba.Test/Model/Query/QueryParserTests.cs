// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

using Arriba.Model.Expressions;
using Arriba.Model.Query;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model.Query
{
    [TestClass]
    public class QueryParserTests
    {
        [TestMethod]
        public void QueryParser_Basic()
        {
            // Empty
            Assert.AreEqual("", QueryParser.Parse("").ToString());

            // Single Bare Term (implied * : value)
            Assert.AreEqual("[*]:editor", QueryParser.Parse("editor").ToString());

            // Single Bare Quoted Value, unterminated
            Assert.AreEqual("[*]:\"Scott Louv\"", QueryParser.Parse("\"Scott Louv").ToString());

            // Bare Terms (implied AND)
            Assert.AreEqual("[*]:editor AND [*]:ide", QueryParser.Parse("editor ide").ToString());

            // Column = Value
            Assert.AreEqual("[CreatedBy] = Scott", QueryParser.Parse("CreatedBy=Scott").ToString());

            // Column = Empty
            Assert.AreEqual("[CreatedBy] = \"\"", QueryParser.Parse("CreatedBy=\"\"").ToString());
            Assert.AreEqual("[CreatedBy] <> \"\"", QueryParser.Parse("CreatedBy<>\"\"").ToString());

            // Braced Column = Quoted Value
            Assert.AreEqual("[Created By] < \"Scott Louvau\"", QueryParser.Parse("[Created By]<\"Scott Louvau\"").ToString());

            // Braced Column with trailing spaces, requesting hint = Defaulted Value, no space in column name
            Assert.AreEqual("[Created By] <> \"\"", QueryParser.Parse("[Created By] ", true).ToString());

            // Incomplete Braced Column with trailing spaces, requesting hint include in partial
            Assert.AreEqual("[Created  ] <> \"\"", QueryParser.Parse("[Created  ", true).ToString());

            // Negated Bare Value
            Assert.AreEqual("NOT([*]:editor)", QueryParser.Parse("-editor").ToString());

            // Multiple Clauses
            Assert.AreEqual("[Created By] = \"Scott Louvau\" AND [Priority] <= 3 AND [*]:verified", QueryParser.Parse("[Created By]=\"Scott Louvau\" && Priority <= 3 && verified").ToString());

            // Nested Subqueries
            Assert.AreEqual("[Priority] <= 3 AND ([*]:verified OR [*]:checked OR [*]:scanned OR [Triage] = \"\")", QueryParser.Parse("Priority <= 3 && (verified || checked || scanned || Triage=\"\")").ToString());

            // Unclosed subquery
            Assert.AreEqual("[Priority] <= 3 AND ([*]:verified OR [*]:chec)", QueryParser.Parse("Priority <= 3 && (verified || chec").ToString());

            // Trailing operator - with hint, hint term
            Assert.AreEqual("[Priority] <= 3 AND [*]:\"\"", QueryParser.Parse("Priority <= 3 &&", true).ToString());
            Assert.AreEqual("[Priority] <= 3 OR [*]:\"\"", QueryParser.Parse("Priority <= 3 ||", true).ToString());

            // Trailing operator - no hint, no next clause
            Assert.AreEqual("[Priority] <= 3", QueryParser.Parse("Priority <= 3 &&").ToString());
            Assert.AreEqual("[Priority] <= 3", QueryParser.Parse("Priority <= 3 ||").ToString());

            // Non-identifier junk
            Assert.AreEqual("", QueryParser.Parse("<<<<>>>>").ToString());
        }

        [TestMethod]
        public void QueryParser_AllTokenForms()
        {
            Assert.AreEqual("[*]:a AND [*]:b AND [*]:c AND [*]:d", QueryParser.Parse("a && b & c AND d").ToString());
            Assert.AreEqual("[*]:a OR [*]:b OR [*]:c OR [*]:d", QueryParser.Parse("a || b | c OR d").ToString());
            Assert.AreEqual("NOT([*]:a) AND NOT([*]:b) AND NOT([*]:c)", QueryParser.Parse("!a -b NOT c").ToString());
            Assert.AreEqual("[t] = a AND [t] = b", QueryParser.Parse("t=a t==b").ToString());
            Assert.AreEqual("[t] <> a AND [t] <> b", QueryParser.Parse("t!=a t<>b").ToString());
            Assert.AreEqual("[t]:a AND [t]:b AND [t]:c AND [t]:d AND [t]:e", QueryParser.Parse("t:a AND t MATCH b AND t LIKE c AND t FREETEXT d AND t CONTAINS e").ToString());
            Assert.AreEqual("[t]::a AND [t]::b", QueryParser.Parse("t::a AND t MATCHEXACT b").ToString());
            Assert.AreEqual("[a] STARTSWITH t AND [b] STARTSWITH t AND [c] STARTSWITH t", QueryParser.Parse("a |> t && b STARTSWITH t && c UNDER t").ToString());
        }

        [TestMethod]
        public void QueryParser_ExpressionPrecedence()
        {
            // AND is higher precedence than OR
            Assert.AreEqual("(([*]:A AND [*]:B) OR [*]:C)", ToParenthesizedString(QueryParser.Parse("A AND B OR C")));
            Assert.AreEqual("([*]:A OR ([*]:B AND [*]:C))", ToParenthesizedString(QueryParser.Parse("A OR B AND C")));

            // NOT is higher precedence than AND or OR
            Assert.AreEqual("([*]:A OR (NOT([*]:B) AND [*]:C))", ToParenthesizedString(QueryParser.Parse("A OR NOT B AND C")));
            Assert.AreEqual("(([*]:A AND NOT([*]:B)) OR [*]:C)", ToParenthesizedString(QueryParser.Parse("A AND NOT B OR C")));

            // AND precedence works with bare terms
            Assert.AreEqual("(([*]:A AND [*]:B) OR [*]:C)", ToParenthesizedString(QueryParser.Parse("A B OR C")));
            Assert.AreEqual("([*]:A OR ([*]:B AND [*]:C))", ToParenthesizedString(QueryParser.Parse("A OR B C")));

            // Explicit Parenthesis correctly override default precedence
            Assert.AreEqual("([*]:A AND ([*]:B OR [*]:C))", ToParenthesizedString(QueryParser.Parse("A AND (B OR C)")));
            Assert.AreEqual("(([*]:A OR [*]:B) AND [*]:C)", ToParenthesizedString(QueryParser.Parse("(A OR B) AND C")));
        }

        /// <summary>
        ///  Return the query as a string with all parenthesis to clarify
        ///  precedence.
        /// </summary>
        /// <param name="query">IExpression to convert</param>
        /// <returns>Text query with all terms parenthesized</returns>
        public static string ToParenthesizedString(IExpression query)
        {
            StringBuilder result = new StringBuilder();
            ToParenthesizedString(query, result);
            return result.ToString();
        }

        private static void ToParenthesizedString(IExpression query, StringBuilder result)
        {
            if ((query is AndExpression) || (query is OrExpression))
            {
                result.Append("(");

                string joinWith = (query is AndExpression ? " AND " : " OR ");
                IList<IExpression> children = query.Children();
                for (int i = 0; i < children.Count; ++i)
                {
                    IExpression child = children[i];
                    if (i > 0) result.Append(joinWith);
                    ToParenthesizedString(child, result);
                }

                result.Append(")");
            }
            else
            {
                result.Append(query);
            }
        }

        [TestMethod]
        public void QueryParser_EscapedValues()
        {
            // Escaped quotes are whole value (no spacing, start and end)
            Assert.AreEqual("[Value] = \"\"\"\"\"\"", QueryParser.Parse("Value = \"\"\"\"\"\"").ToString());

            // Escaped Quotes in Value with spacing
            Assert.AreEqual("[Value] = \"Don't \"\"forget\"\"\"", QueryParser.Parse("Value = \"Don't \"\"forget\"\"\"").ToString());

            // Spacing between quotes in value - not an escaped quote
            Assert.AreEqual("[Value] = \"Don't \" AND [*]:\"forget\"\"\"", QueryParser.Parse("Value = \"Don't \" \"forget\"\"\"").ToString());

            // Braces in quotes not interpreted oddly
            Assert.AreEqual("[Value] = []", QueryParser.Parse("Value = \"[]\"").ToString());

            // Braces escaped are whole value
            Assert.AreEqual("[]]]]] = something", QueryParser.Parse("[]]]]] = something").ToString());

            // Braces escaped with spacing
            Assert.AreEqual("[Assigned [PM ]]] = something", QueryParser.Parse("[Assigned [PM ]]] = something").ToString());

            // Quoted Column Name causes error
            Assert.AreEqual("", QueryParser.Parse("\"Title\"=Something").ToString());

            // Braced Value worked; braces don't interrupt value
            Assert.AreEqual("[Title] = [Something]", QueryParser.Parse("Title=[Something]").ToString());
        }

        [TestMethod]
        public void QueryParser_CaseSensitivity()
        {
            Assert.AreEqual("[*]:louvau AND ([Priority] = 3 OR [Priority] = 2)", QueryParser.Parse("louvau and (Priority = 3 Or Priority = 2)").ToString());
        }

        [TestMethod]
        public void QueryParser_PartialQueries()
        {
            // Nothing - no query
            Assert.AreEqual("", QueryParser.Parse("").ToString());

            // Negation only - no query
            Assert.AreEqual("", QueryParser.Parse("!").ToString());

            // Paren only - no query
            Assert.AreEqual("", QueryParser.Parse("(").ToString());

            // Incomplete column name - empty string
            Assert.AreEqual("", QueryParser.Parse("[Starting").ToString());

            // Column name without operator - empty string
            Assert.AreEqual("", QueryParser.Parse("[Title]").ToString());

            // Column name and operator without value - empty string
            Assert.AreEqual("", QueryParser.Parse("[Title] =").ToString());
            Assert.AreEqual("", QueryParser.Parse("[Title] = ").ToString());
            Assert.AreEqual("", QueryParser.Parse("Title > ").ToString());

            // Incomplete quoted value - search value so far
            Assert.AreEqual("[*]:Starting", QueryParser.Parse("\"Starting").ToString());

            // Incomplete parenthesized group - search terms so far
            Assert.AreEqual("[*]:Starting OR [*]:Ending", QueryParser.Parse("(Starting || Ending").ToString());

            // Incomplete negate operator (it's a bare value until the operator is complete)
            Assert.AreEqual("[*]:Priority", QueryParser.Parse("Priority !").ToString());
        }

        [TestMethod]
        public void QueryParser_ValuesWithSpecialCharacters()
        {
            // Verify casing preserved in values with "AND", "OR", "NOT" in them
            Assert.AreEqual("[Area Path] = Product\\Feature AND [Resolution] = \"Not Repro\" AND [Title]:or", QueryParser.Parse("[Area Path] = \"Product\\Feature\" AND Resolution = \"Not Repro\" AND Title:or").ToString());

            // Negation Operators: Allowed in column names and values
            Assert.AreEqual("[background-color]:dark-blue", QueryParser.Parse("background-color:dark-blue").ToString());
            Assert.AreEqual("[background!color]:dark!blue", QueryParser.Parse("background!color:dark!blue").ToString());

            // Dots: Allowed in column names and values
            Assert.AreEqual("[background.color]:dark.blue", QueryParser.Parse("background.color:dark.blue").ToString());

            // Parens: Not allowed in values
            Assert.AreEqual("[background-image]:url AND [*]:something", QueryParser.Parse("background-image:url(something)").ToString());

            // Parens: Not allowed in column names (causes parse error here, column name interpreted as two values)
            Assert.AreEqual("", QueryParser.Parse("background(image):url(something)").ToString());

            // Boolean Operators: Allowed in column names or values
            Assert.AreEqual("[one&two|three&&four||five]:one&two|three&&four||five", QueryParser.Parse("one&two|three&&four||five:one&two|three&&four||five").ToString());

            // Compare Operators interrupt column names
            Assert.AreEqual("[a]::b", QueryParser.Parse("a::b").ToString());
            Assert.AreEqual("[a] <= b", QueryParser.Parse("a<=b").ToString());
            Assert.AreEqual("[a] >= b", QueryParser.Parse("a>=b").ToString());
            Assert.AreEqual("[a] = b", QueryParser.Parse("a==b").ToString());
            Assert.AreEqual("[a] <> b", QueryParser.Parse("a!=b").ToString());
            Assert.AreEqual("[a] <> b", QueryParser.Parse("a<>b").ToString());
            Assert.AreEqual("[a]:b", QueryParser.Parse("a:b").ToString());
            Assert.AreEqual("[a] < b", QueryParser.Parse("a<b").ToString());
            Assert.AreEqual("[a] > b", QueryParser.Parse("a>b").ToString());
            Assert.AreEqual("[a] = b", QueryParser.Parse("a=b").ToString());

            // Compare operators don't interrupt values
            Assert.AreEqual("[a]::b:<=>==<>c", QueryParser.Parse("a::b:<=>==<>c").ToString());
        }

        [TestMethod]
        public void QueryParser_ValuesContainingKeywords()
        {
            // Try terms which start with, contain, or end with keywords (NOT, AND, OR)
            Assert.AreEqual("[*]:noted AND [*]:corn AND [*]:sandwiches", QueryParser.Parse("noted corn sandwiches").ToString());
            Assert.AreEqual("[*]:org AND [*]:constructor", QueryParser.Parse("org constructor").ToString());
        }

        [TestMethod]
        public void QueryParser_EscapeAndUnescape()
        {
            Assert.AreEqual("", QueryParser.WrapColumnName(null));
            Assert.AreEqual("", QueryParser.WrapColumnName(""));
            Assert.AreEqual("[One]", QueryParser.WrapColumnName("One"));
            Assert.AreEqual("[Owner [Ops]]]", QueryParser.WrapColumnName("Owner [Ops]"));

            Assert.AreEqual("\"\"", QueryParser.WrapValue(null));
            Assert.AreEqual("\"\"", QueryParser.WrapValue(""));
            Assert.AreEqual("Simple", QueryParser.WrapValue("Simple"));
            Assert.AreEqual("\"Bilbo \"\"Ringbearer\"\" Baggins\"", QueryParser.WrapValue("Bilbo \"Ringbearer\" Baggins"));

            Assert.AreEqual("", QueryParser.UnwrapColumnName(null));
            Assert.AreEqual("", QueryParser.UnwrapColumnName(""));
            Assert.AreEqual("One", QueryParser.UnwrapColumnName("One"));
            Assert.AreEqual("One", QueryParser.UnwrapColumnName("[One]"));
            Assert.AreEqual("Owner [Ops]", QueryParser.UnwrapColumnName("[Owner [Ops]]]"));
            Assert.AreEqual("Owner [[Ops]]", QueryParser.UnwrapColumnName("[Owner [[Ops]]]]]"));

            Assert.AreEqual("", QueryParser.UnwrapValue(null));
            Assert.AreEqual("", QueryParser.UnwrapValue(""));
            Assert.AreEqual("", QueryParser.UnwrapValue("\"\""));
            Assert.AreEqual("Simple", QueryParser.UnwrapValue("Simple"));
            Assert.AreEqual("Simple", QueryParser.UnwrapValue("\"Simple\""));
            Assert.AreEqual("Bilbo \"Ringbearer\" Baggins", QueryParser.UnwrapValue("\"Bilbo \"\"Ringbearer\"\" Baggins\""));
            Assert.AreEqual("\"\"\"", QueryParser.UnwrapValue("\"\"\"\"\"\"\""));
        }
    }
}

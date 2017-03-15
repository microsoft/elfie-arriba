﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Model;
using Arriba.Model.Expressions;
using Arriba.Model.Query;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Arriba.Test.Model.Query
{
    public class IntelliSenseItem
    {
        public string Value { get; set; }
        public string Hint { get; set; }
        public string QueryWithCompletion { get; set; }

        public IntelliSenseItem(string value, string hint, string queryWithCompletion)
        {
            this.Value = value;
            this.Hint = hint;
            this.QueryWithCompletion = queryWithCompletion;
        }

        public override string ToString()
        {
            return this.Value;
        }
    }

    public class IntelliSenseResult
    {
        public IReadOnlyCollection<IntelliSenseItem> Suggestions;
        public IReadOnlyList<char> CompletionCharacters;
    }

    public class QueryIntelliSenseProvider
    {
        private static List<IntelliSenseItem> BooleanOperators = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem("AND", String.Empty, String.Empty),
            new IntelliSenseItem("OR", String.Empty, String.Empty),
            new IntelliSenseItem("&&", String.Empty, String.Empty),
            new IntelliSenseItem("||", String.Empty, String.Empty)
        };

        private static List<IntelliSenseItem> CompareOperators = new List<IntelliSenseItem>()
        {
            new IntelliSenseItem(":", "contains word starting with", String.Empty),
            new IntelliSenseItem("=", "equals [case sensitive]", String.Empty),
            new IntelliSenseItem("==", "equals [case sensitive]", String.Empty),
            new IntelliSenseItem("::", "contains exact word", String.Empty),
            new IntelliSenseItem("!=", "does not equal", String.Empty),
            new IntelliSenseItem("<>", "does not equal", String.Empty),
            new IntelliSenseItem("<", "less than", String.Empty),
            new IntelliSenseItem("<=", "less than or equal", String.Empty),
            new IntelliSenseItem(">", "greater than", String.Empty),
            new IntelliSenseItem(">=", "greater than or equal", String.Empty),
            new IntelliSenseItem("|>", "starts with", String.Empty)
        };



        // Table or Database? How do cross table queries work for IntelliSense? How are tables ranked relative to one another?
        // Or .. remove allCount query, put you in to quickly complete table name first? "As Na:OSGSMART" -> "Asset" | "Name:OSGSMART"

        //public IntelliSenseResult GetIntelliSenseItems(string queryBeforeCursor, ITable targetTable)
        //{
        //    string lastComponent;
        //    CurrentTokenOption validTokens = GetCurrentTokenOptions(queryBeforeCursor, out lastComponent);


        //}

        public IntelliSenseGuidance GetCurrentTokenOptions(string queryBeforeCursor)
        {
            IntelliSenseGuidance defaultGuidance = new IntelliSenseGuidance(String.Empty, CurrentTokenCategory.ColumnName | CurrentTokenCategory.Value);
            if (String.IsNullOrEmpty(queryBeforeCursor)) return defaultGuidance;

            IExpression query = QueryParser.Parse(queryBeforeCursor);
            TermExpression lastTerm = GetLastTerm(query);

            if (lastTerm == null)
            {
                return defaultGuidance;
            }
            else
            {
                return lastTerm.Guidance;
            }
        }

        private TermExpression GetLastTerm(IExpression query)
        {
            IList<IExpression> children = query.Children();

            if(children == null || children.Count == 0)
            {
                if (query is TermExpression) return (TermExpression)query;
                return null;
            }

            return GetLastTerm(children[children.Count - 1]);
        }
    }

    [TestClass]
    public class QueryIntelliSenseTests
    {
        [TestMethod]
        public void QueryIntelliSense_TokenOptionBasics()
        {
            QueryIntelliSenseProvider p = new QueryIntelliSenseProvider();

            // "" -> ColumnName or Value [no boolean operator if no previous term]
            Assert.AreEqual("[] [ColumnName, Value]", p.GetCurrentTokenOptions("").ToString());

            // "BareValue" -> ColumnName or Value
            Assert.AreEqual("[BareValue] [ColumnName, Value]", p.GetCurrentTokenOptions("BareValue").ToString());

            // "BareValue " -> CompareOperator [bare value or column name ended by space]
            Assert.AreEqual("[] [BooleanOperator, ColumnName, CompareOperator, Value]", p.GetCurrentTokenOptions("BareValue ").ToString());

            // "[An" -> ColumnName only [explicit column name escaping]
            Assert.AreEqual("[An] [ColumnName]", p.GetCurrentTokenOptions("[An").ToString());

            // "\"An" -> Value only [explicit value escaping]
            Assert.AreEqual("[An] [Value]", p.GetCurrentTokenOptions("\"An").ToString());

            // "[Analyzer]" -> CompareOperator [column name closed by ']']
            Assert.AreEqual("[] [CompareOperator]", p.GetCurrentTokenOptions("[Analyzer]").ToString());
            Assert.AreEqual("[] [CompareOperator]", p.GetCurrentTokenOptions("[Analyzer] ").ToString());

            // "\"Analysis\"" -> BooleanOperator, ColumnName, Value [a bare term explicitly closed]
            Assert.AreEqual("[] [BooleanOperator, ColumnName, Value]", p.GetCurrentTokenOptions("\"Analysis\"").ToString());
            Assert.AreEqual("[] [BooleanOperator, ColumnName, Value]", p.GetCurrentTokenOptions("\"Analysis\" ").ToString());

            // "Analyzer:" -> Operator [suggest operators until space afterward]
            Assert.AreEqual("[:] [CompareOperator]", p.GetCurrentTokenOptions("Analyzer:").ToString());
            Assert.AreEqual("[::] [CompareOperator]", p.GetCurrentTokenOptions("Analyzer::").ToString());
            Assert.AreEqual("[] [Value]", p.GetCurrentTokenOptions("Analyzer: ").ToString());
            Assert.AreEqual("[V] [Value]", p.GetCurrentTokenOptions("Analyzer:V").ToString());

            // "Analyzer >" -> CompareOperator

            // "[Analyzer] !" -> CompareOperator
            // "[Analyzer] |" -> CompareOperator


            // Invalid
            // \"Analysis\"=\"Interesting\"
            // [Analyzer] BareTerm
            // [Analyzer] \"QuotedValue\"
            // )


        }
    }
}

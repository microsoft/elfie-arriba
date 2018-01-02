// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Query;
using XForm.Data;
using XForm.Query.Expression;
using XForm.Extensions;

namespace XForm.Test.Query
{
    [TestClass]
    public class XqlParsingTests
    {
        [TestMethod]
        public void XqlParser_QueryParsing()
        {
            WorkflowContext context = SampleDatabase.WorkflowContext;
            IDataBatchEnumerator source = XqlParser.Parse("read WebRequest", null, context);

            // Single Term
            Assert.AreEqual("[ServerPort] = 80", Parse("[ServerPort] = 80", source, context).ToString());

            // Column to Column
            Assert.AreEqual("[ServerPort] < [RequestBytes]", Parse("[ServerPort] < [RequestBytes]", source, context).ToString());

            // Column to Function(Constant) => Resolved
            Assert.AreEqual("[ServerName] = WS-FRONT-4", Parse("[ServerName] = ToUpper(\"ws-front-4\")", source, context).ToString());

            // Column to Function(Column)
            Assert.AreEqual("[ServerName] = ToUpper([ServerName])", Parse("[ServerName] = ToUpper([ServerName])", source, context).ToString());

            // Multiple Clauses, explicit AND
            Assert.AreEqual("[ServerPort] = 80 AND [ResponseBytes] > 900", Parse("[ServerPort] = 80 AND [ResponseBytes] > 900", source, context).ToString());

            // Multiple Clauses, implicit AND
            Assert.AreEqual("[ServerPort] = 80 AND [ResponseBytes] > 900", Parse("[ServerPort] = 80 [ResponseBytes] > 900", source, context).ToString());

            // AND and OR with no parens, AND is tighter, parens omitted on ToString
            Assert.AreEqual("[ServerPort] = 80 AND [ResponseBytes] > 1200 OR [ResponseBytes] < 400", Parse("[ServerPort] = 80 AND [ResponseBytes] > 1200 OR [ResponseBytes] < 400", source, context).ToString());

            // AND and OR with AND parens, AND is tighter, parens omitted because same as default precedence
            Assert.AreEqual("[ServerPort] = 80 AND [ResponseBytes] > 1200 OR [ResponseBytes] < 400", Parse("([ServerPort] = 80 AND [ResponseBytes] > 1200) OR [ResponseBytes] < 400", source, context).ToString());
            
            // AND and OR with OR parens, parens on output to maintain evaluation order
            Assert.AreEqual("[ServerPort] = 80 AND ([ResponseBytes] > 1200 OR [ResponseBytes] < 400)", Parse("[ServerPort] = 80 AND ([ResponseBytes] > 1200 OR [ResponseBytes] < 400)", source, context).ToString());

            // NOT
            Assert.AreEqual("NOT([ServerPort] = 80)", Parse("NOT([ServerPort] = 80)", source, context).ToString());

            // Operators are case insensitive
            Assert.AreEqual("[ServerPort] = 80 AND [ResponseBytes] > 1200 OR NOT([ResponseBytes] < 400)", Parse("[ServerPort] = 80 aNd [ResponseBytes] > 1200 oR nOT [ResponseBytes] < 400", source, context).ToString());
        }

        private static IExpression Parse(string query, IDataBatchEnumerator source, WorkflowContext context)
        {
            XqlParser parser = new XqlParser(query, context);
            context.Parser = parser;
            return parser.NextExpression(source, context);
        }

        [TestMethod]
        public void XqlParser_QueryEvaluation()
        {
            WorkflowContext context = SampleDatabase.WorkflowContext;
            IDataBatchEnumerator source = XqlParser.Parse(@"
                read WebRequest
                cache all", null, context);

            // Results from WebRequest.20171202.r5.n1000, counts validated against Excel

            // Single Term
            Assert.AreEqual(423, RunAndCount("where [ServerPort] = 80", source, context));

            // OR
            Assert.AreEqual(1000, RunAndCount("where [ServerPort] = 80 OR [ServerPort] = 443", source, context));

            // AND
            Assert.AreEqual(278, RunAndCount("where [ServerPort] = 80 AND Cast([ResponseBytes], Int32) > 1000", source, context));
        }

        private static int RunAndCount(string query, IDataBatchEnumerator source, WorkflowContext context)
        {
            source.Reset();
            return (int)source.Query(query, context).RunWithoutDispose();
        }

        [TestMethod]
        public void XqlSplitter_Basics()
        {
            Assert.AreEqual("Simple", TestSplitAndJoin("Simple"));
            Assert.AreEqual("Simple", TestSplitAndJoin("  Simple "));
            Assert.AreEqual("Simple|settings", TestSplitAndJoin(" Simple   settings"));
            Assert.AreEqual(@"read|C:\Download\Sample.csv", TestSplitAndJoin(@"read ""C:\Download\Sample.csv"""));
            Assert.AreEqual(@"read|C:\Download\Sample.csv", TestSplitAndJoin(@"read ""C:\Download\Sample.csv"));
            Assert.AreEqual(@"read|C:\Download\Sample.csv", TestSplitAndJoin(@"read ""C:\Download\Sample.csv"" "));
            Assert.AreEqual(@"value|""Quoted""", TestSplitAndJoin(@"value """"""Quoted"""""""));
            Assert.AreEqual(@"Column [Name] Here", TestSplitAndJoin(@"[Column [Name]] Here]"));
            Assert.AreEqual(@"columns|One|Two|Three", TestSplitAndJoin(@"columns One,Two, Three"));
        }

        private static string TestSplitAndJoin(string xqlLine)
        {
            XqlScanner scanner = new XqlScanner(xqlLine);

            List<string> parts = new List<string>();
            while (scanner.Current.Type != TokenType.End)
            {
                parts.Add(scanner.Current.Value);
                scanner.Next();
            }

            return string.Join("|", parts);
        }
    }
}

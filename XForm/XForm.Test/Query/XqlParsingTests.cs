// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Elfie.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;
using XForm.Query.Expression;

namespace XForm.Test.Query
{
    [TestClass]
    public class XqlParsingTests
    {
        [TestMethod]
        public void XqlParsing_Invalid()
        {
            // Verify an empty query causes a UsageException
            Verify.Exception<UsageException>(() => SampleDatabase.XDatabaseContext.Query(""));

            // Verify all verbs passed alone as the first query text cause a UsageException
            foreach (string verb in XqlParser.SupportedVerbs)
            {
                if (verb.Equals("read", StringComparison.OrdinalIgnoreCase) || verb.Equals("readRange", StringComparison.OrdinalIgnoreCase)) continue;
                Trace.WriteLine(verb);
                Verify.Exception<UsageException>(() => SampleDatabase.XDatabaseContext.Query(verb));
            }
        }

        [TestMethod]
        public void XqlParser_QueryParsing()
        {
            XDatabaseContext context = SampleDatabase.XDatabaseContext;
            IXTable source = context.Query(@"read WebRequest.Typed");

            // Single Term
            Assert.AreEqual("[ServerPort] = 80", ParseExpression("[ServerPort] = 80", source, context).ToString());

            // Column to Column
            Assert.AreEqual("[ServerPort] < [RequestBytes]", ParseExpression("[ServerPort] < [RequestBytes]", source, context).ToString());

            // Column to Function(Constant)
            Assert.AreEqual("[ServerName] = ToUpper(\"ws-front-4\")", ParseExpression("[ServerName] = ToUpper(\"ws-front-4\")", source, context).ToString());

            // Column to Function(Column)
            Assert.AreEqual("[ServerName] = ToUpper([ServerName])", ParseExpression("[ServerName] = ToUpper([ServerName])", source, context).ToString());

            // Compare to null and empty
            Assert.AreEqual("[ServerName] = \"\"", ParseExpression("[ServerName] = \"\"", source, context).ToString());
            Assert.AreEqual("[ServerName] = null", ParseExpression("[ServerName] = null", source, context).ToString());

            // Multiple Clauses, explicit AND
            Assert.AreEqual("[ServerPort] = 80 AND [ResponseBytes] > 900", ParseExpression("[ServerPort] = 80 AND [ResponseBytes] > 900", source, context).ToString());

            // Multiple Clauses, implicit AND
            Assert.AreEqual("[ServerPort] = 80 AND [ResponseBytes] > 900", ParseExpression("[ServerPort] = 80 [ResponseBytes] > 900", source, context).ToString());

            // AND and OR with no parens, AND is tighter, parens omitted on ToString
            Assert.AreEqual("[ServerPort] = 80 AND [ResponseBytes] > 1200 OR [ResponseBytes] < 900", ParseExpression("[ServerPort] = 80 AND [ResponseBytes] > 1200 OR [ResponseBytes] < 900", source, context).ToString());

            // AND and OR with AND parens, AND is tighter, parens omitted because same as default precedence
            Assert.AreEqual("[ServerPort] = 80 AND [ResponseBytes] > 1200 OR [ResponseBytes] < 900", ParseExpression("([ServerPort] = 80 AND [ResponseBytes] > 1200) OR [ResponseBytes] < 900", source, context).ToString());

            // AND and OR with OR parens, parens on output to maintain evaluation order
            Assert.AreEqual("[ServerPort] = 80 AND ([ResponseBytes] > 1200 OR [ResponseBytes] < 900)", ParseExpression("[ServerPort] = 80 AND ([ResponseBytes] > 1200 OR [ResponseBytes] < 900)", source, context).ToString());

            // AND after OR [OrExpression parsing falls out correctly]
            Assert.AreEqual("[ServerPort] = 80 AND [ResponseBytes] > 1200 OR [ResponseBytes] < 900 AND [ServerPort] != 443", ParseExpression("[ServerPort] = 80 AND [ResponseBytes] > 1200 OR [ResponseBytes] < 900 AND [ServerPort] != 443", source, context).ToString());

            // NOT
            Assert.AreEqual("NOT([ServerPort] = 80)", ParseExpression("NOT([ServerPort] = 80)", source, context).ToString());

            // Operators are case insensitive
            Assert.AreEqual("[ServerPort] = 80 AND [ResponseBytes] > 1200 OR NOT([ResponseBytes] < 900)", ParseExpression("[ServerPort] = 80 aNd [ResponseBytes] > 1200 oR nOT [ResponseBytes] < 900", source, context).ToString());

            // Unclosed quotes shouldn't parse across lines
            Assert.AreEqual("[ServerName] != \"8\"", ParseExpression("[ServerName] != \"8\r\nschema", source, context).ToString());
            Assert.AreEqual("[ServerName] != \"\"", ParseExpression("[ServerName] != \"\nschema", source, context).ToString());
            Assert.AreEqual("[ServerName] != \"   \"", ParseExpression("[ServerName] != \"   \nschema", source, context).ToString());

            // Constant = Constant rule
            Verify.Exception<ArgumentException>(() => ParseExpression("80 = 80", source, context));

            // String = Quoted Only Constant rule
            Verify.Exception<ArgumentException>(() => ParseExpression("[ServerName] = 80", source, context));
            Assert.AreEqual("[ServerName] = \"80\"", ParseExpression("[ServerName] = \"80\"", source, context).ToString());

            // Cast found invalid value
            Verify.Exception<ArgumentException>(() => ParseExpression("Cast(80, Int32) = 5.5", source, context));
            Verify.Exception<ArgumentException>(() => ParseExpression("Cast(\"2018-05-22T12:30:30Z\", DateTime) >= \"2018-05|22T12:30:30Z\"", source, context));
            Verify.Exception<ArgumentException>(() => ParseExpression("Cast(\"14d\", TimeSpan) > \"55mega\"", source, context));
        }

        private static IExpression ParseExpression(string query, IXTable source, XDatabaseContext context)
        {
            XqlParser parser = new XqlParser(query, context);
            context.Parser = parser;
            return parser.NextExpression(source, context);
        }

        [TestMethod]
        public void XqlParser_QueryEvaluation()
        {
            XDatabaseContext context = SampleDatabase.XDatabaseContext;
            IXTable source = context.Query(@"
                read WebRequest
                cast [ServerPort], Int32
                cast [ResponseBytes], Int32, None, 0, InvalidOrNull
                ");

            // Results from WebRequest.20171202.r5.n1000, counts validated against Excel

            // Single Term
            Assert.AreEqual(423, RunAndCount("where [ServerPort] = 80", source, context));

            // OR
            Assert.AreEqual(1000, RunAndCount("where [ServerPort] = 80 OR [ServerPort] = 443", source, context));

            // AND
            Assert.AreEqual(278, RunAndCount("where [ServerPort] = 80 AND [ResponseBytes] > 1000", source, context));

            // Precedence: Counts are correct for precedence; default is 'AND' terms evaluate together first 
            Assert.AreEqual(55, RunAndCount("where [ServerPort] = 80 AND ([ResponseBytes] > 1200 OR [ResponseBytes] < 900)", source, context));
            Assert.AreEqual(22 + 95, RunAndCount("where ([ServerPort] = 80 AND [ResponseBytes] > 1200) OR [ResponseBytes] < 900", source, context));
            Assert.AreEqual(22 + 95, RunAndCount("where [ServerPort] = 80 AND [ResponseBytes] > 1200 OR [ResponseBytes] < 900", source, context));
        }

        private static int RunAndCount(string query, IXTable source, XDatabaseContext context)
        {
            source.Reset();
            return (int)context.Query(query, source).RunWithoutDispose().RowCount;
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

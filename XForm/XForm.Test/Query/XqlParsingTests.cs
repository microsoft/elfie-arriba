// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class XqlParsingTests
    {
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
            while(scanner.Current.Type != TokenType.End)
            {
                parts.Add(scanner.Current.Value);
                scanner.Next();
            }

            return string.Join("|", parts);
        }
    }
}

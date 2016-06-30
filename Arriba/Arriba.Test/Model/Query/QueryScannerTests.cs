// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Arriba.Model.Query;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class QueryScannerTests
    {
        [TestMethod]
        public void QueryScanner_Basic()
        {
            Assert.AreEqual(0, Tokenize(String.Empty).Count);
            Assert.AreEqual("simple", String.Join("|", Tokenize("simple")));
            Assert.AreEqual("prefix|:|value", String.Join("|", Tokenize("prefix:value")));
            Assert.AreEqual("column| <=| value", String.Join("|", Tokenize("column <= value")));
            Assert.AreEqual("column| >| value| AND| [|Braced| ]|]|Column|]|=|\"|Quoted| <=|=|>| Value|\"", String.Join("|", Tokenize("column > value AND [Braced ]]Column]=\"Quoted <==> Value\"")));
            Assert.AreEqual("[|BracedColumn|]|==|\"|Unterminat", String.Join("|", Tokenize("[BracedColumn]==\"Unterminat")));
        }

        private static List<Token> Tokenize(string value)
        {
            QueryScanner scanner = new QueryScanner(new StringReader(value));

            List<Token> tokens = new List<Token>();
            while (scanner.Next())
            {
                tokens.Add(scanner.Current);
            }

            return tokens;
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class PipelineTests
    {
        [TestMethod]
        public void ConfigurationLineParsing()
        {
            Assert.AreEqual("Simple", TestSplitAndJoin("Simple"));
            Assert.AreEqual("Simple", TestSplitAndJoin("  Simple "));
            Assert.AreEqual("Simple|settings", TestSplitAndJoin(" Simple   settings"));
            Assert.AreEqual(@"read|C:\Download\Sample.csv",TestSplitAndJoin(@"read ""C:\Download\Sample.csv"""));
            Assert.AreEqual(@"read|C:\Download\Sample.csv", TestSplitAndJoin(@"read ""C:\Download\Sample.csv"" "));
            Assert.AreEqual(@"value|""Quoted""", TestSplitAndJoin(@"value """"""Quoted"""""""));
            Assert.AreEqual(@"columns|One|Two|Three", TestSplitAndJoin(@"columns One,Two, Three"));
        }

        private static string TestSplitAndJoin(string xqlLine)
        {
            PipelineScanner scanner = new PipelineScanner(xqlLine);
            scanner.NextLine();

            List<string> parts = new List<string>();
            while(!scanner.IsLastPart)
            {
                scanner.NextPart();
                parts.Add(scanner.CurrentPart);
            }

            return string.Join("|", parts);
        }
    }
}

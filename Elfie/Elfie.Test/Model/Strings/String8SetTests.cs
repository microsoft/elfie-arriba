// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Elfie.Test;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model.Strings
{
    [TestClass]
    public class String8SetTests
    {
        [TestMethod]
        public void String8Set_Split()
        {
            Assert.AreEqual(string.Empty, SplitAndJoin(string.Empty, '.'));
            Assert.AreEqual("System|Collections|Generic|List<T>", SplitAndJoin("System.Collections.Generic.List<T>", '.'));
            Assert.AreEqual("A|B|C|D", SplitAndJoin("A.B.C.D", '.'));
            Assert.AreEqual("|A|B|C||D|", SplitAndJoin(".A.B.C..D.", '.'));
            Assert.AreEqual("No Delimiters", SplitAndJoin("No Delimiters", '.'));
        }

        private string SplitAndJoin(string value, char delimiter)
        {
            String8 value8 = String8.Convert(value, new byte[String8.GetLength(value)]);
            String8Set set = value8.Split(delimiter, new PartialArray<int>());
            String8 joined8 = set.Join('|', new byte[set.Value.Length]);
            return joined8.ToString();
        }

        [TestMethod]
        public void String8Set_SplitOutsideQuotes()
        {
            Assert.AreEqual(string.Empty, SplitOutsideQuotesAndJoin(string.Empty, (byte)','));
            Assert.AreEqual("Nothing to Split", SplitOutsideQuotesAndJoin("Nothing to Split", (byte)','));

            Assert.AreEqual("\"Quotes Around Everything\"", SplitOutsideQuotesAndJoin("\"Quotes Around Everything\"", (byte)' '));
            Assert.AreEqual("\"Unclosed Quotes ", SplitOutsideQuotesAndJoin("\"Unclosed Quotes ", (byte)' '));

            Assert.AreEqual("One|Two|Three", SplitOutsideQuotesAndJoin("One,Two,Three", (byte)','));
            Assert.AreEqual("|One|Two||Three|", SplitOutsideQuotesAndJoin(",One,Two,,Three,", (byte)','));
            Assert.AreEqual("One|\"Here, Commas\"|Three", SplitOutsideQuotesAndJoin("One,\"Here, Commas\",Three", (byte)','));
            Assert.AreEqual("\"Quotes, to start\"|Two|\"And, to end\"", SplitOutsideQuotesAndJoin("\"Quotes, to start\",Two,\"And, to end\"", (byte)','));
        }

        private string SplitOutsideQuotesAndJoin(string value, byte delimiter)
        {
            String8 value8 = String8.Convert(value, new byte[String8.GetLength(value)]);
            String8Set set = value8.SplitOutsideQuotes(delimiter, new PartialArray<int>());
            String8 joined8 = set.Join('|', new byte[set.Value.Length]);
            return joined8.ToString();
        }

#if !DEBUG
        [TestMethod]
#endif
        public void String8Set_Split_Performance()
        {
            String8 list = String8.Convert("System.Collections.Generic.List<T>", new byte[50]);
            String8 noDelimiters = String8.Convert("No Delimiters", new byte[25]);
            PartialArray<int> partBuffer = new PartialArray<int>(10, false);

            // Goal: 256MB/sec
            Verify.PerformanceByBytes(256 * LongExtensions.Megabyte, () =>
            {
                int iterations = 200000;
                for (int iteration = 0; iteration < iterations; ++iteration)
                {
                    list.Split('.', partBuffer);
                    noDelimiters.Split('.', partBuffer);
                }

                return iterations * (list.Length + noDelimiters.Length);
            });
        }
    }
}

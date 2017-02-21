// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Text;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model.Strings
{
    [TestClass]
    public class String8SetTests
    {
        [TestMethod]
        public void String8Set_Split()
        {
            Assert.AreEqual(string.Empty, SplitAndJoin(string.Empty, UTF8.Period));
            Assert.AreEqual("System|Collections|Generic|List<T>", SplitAndJoin("System.Collections.Generic.List<T>", UTF8.Period));
            Assert.AreEqual("A|B|C|D", SplitAndJoin("A.B.C.D", UTF8.Period));
            Assert.AreEqual("|A|B|C||D|", SplitAndJoin(".A.B.C..D.", UTF8.Period));
            Assert.AreEqual("No Delimiters", SplitAndJoin("No Delimiters", UTF8.Period));
        }

        private string SplitAndJoin(string value, byte delimiter)
        {
            String8 value8 = String8.Convert(value, new byte[String8.GetLength(value)]);
            String8Set set = value8.Split(delimiter, new PartialArray<int>());
            String8 joined8 = set.Join(UTF8.Pipe, new byte[set.Value.Length]);
            return joined8.ToString();
        }

        [TestMethod]
        public void String8Set_SplitOutsideQuotes()
        {
            Assert.AreEqual(string.Empty, SplitOutsideQuotesAndJoin(string.Empty, UTF8.Comma));
            Assert.AreEqual("Nothing to Split", SplitOutsideQuotesAndJoin("Nothing to Split", UTF8.Comma));

            Assert.AreEqual("\"Quotes Around Everything\"", SplitOutsideQuotesAndJoin("\"Quotes Around Everything\"", (byte)' '));
            Assert.AreEqual("\"Unclosed Quotes ", SplitOutsideQuotesAndJoin("\"Unclosed Quotes ", (byte)' '));

            Assert.AreEqual("One|Two|Three", SplitOutsideQuotesAndJoin("One,Two,Three", UTF8.Comma));
            Assert.AreEqual("|One|Two||Three|", SplitOutsideQuotesAndJoin(",One,Two,,Three,", UTF8.Comma));
            Assert.AreEqual("One|\"Here, Commas\"|Three", SplitOutsideQuotesAndJoin("One,\"Here, Commas\",Three", UTF8.Comma));
            Assert.AreEqual("\"Quotes, to start\"|Two|\"And, to end\"", SplitOutsideQuotesAndJoin("\"Quotes, to start\",Two,\"And, to end\"", UTF8.Comma));
        }

        private string SplitOutsideQuotesAndJoin(string value, byte delimiter)
        {
            String8 value8 = String8.Convert(value, new byte[String8.GetLength(value)]);
            String8Set set = value8.SplitOutsideQuotes(delimiter, new PartialArray<int>());
            String8 joined8 = set.Join(UTF8.Pipe, new byte[set.Value.Length]);
            return joined8.ToString();
        }

        [TestMethod]
        public void String8Set_SplitAndDecodeCsvCells()
        {
            Assert.AreEqual(string.Empty, CsvSplitAndJoin(string.Empty));
            Assert.AreEqual("Single", CsvSplitAndJoin("Single"));
            Assert.AreEqual("One|Two", CsvSplitAndJoin("One,Two"));
            Assert.AreEqual("Quoted, Single", CsvSplitAndJoin("\"Quoted, Single\""));
            Assert.AreEqual("|One||Two|", CsvSplitAndJoin(",One,,Two,"));
            Assert.AreEqual("Empty||Quoted", CsvSplitAndJoin("Empty,\"\",Quoted"));

            // Escaped Quotes right at beginning and end of cell are properly preserved
            Assert.AreEqual("One|Escaped, \"Quotes\"|Three", CsvSplitAndJoin("One,\"Escaped, \"\"Quotes\"\"\",Three"));
            Assert.AreEqual("\"All Quoted\"", CsvSplitAndJoin("\"\"\"All Quoted\"\"\""));

            // Quote counting check - eight double quotes is a quoted cell with three literal quotes
            Assert.AreEqual("One|\"\"\"|Two", CsvSplitAndJoin("One,\"\"\"\"\"\"\"\",Two"));

            // Quoted cells are closed on EOF
            Assert.AreEqual("One|Two|Unterminated", CsvSplitAndJoin("\"One\",Two,\"Unterminated"));

            // Quotes in unquoted cells are ignored [assume bad writer didn't wrap and escape, stop on first ',']
            Assert.AreEqual("Unquoted with unescaped \"Quote", CsvSplitAndJoin("Unquoted with unescaped \"Quote"));

            // Unescaped quotes in quoted cells cause an abort
            Assert.AreEqual("Value|With|Unterminated", CsvSplitAndJoin("Value,With,Unterminated,\"Quote \" Here\",Following"));
        }

        private string CsvSplitAndJoin(string value)
        {
            String8 value8 = String8.Convert(value, new byte[String8.GetLength(value)]);
            String8Set set = value8.SplitAndDecodeCsvCells(new PartialArray<int>());
            String8 joined8 = set.Join(UTF8.Pipe, new byte[set.Value.Length]);
            return joined8.ToString();
        }


#if PERFORMANCE
        [TestMethod]
#endif
        public void String8Set_Split_Performance()
        {
            String8 list = String8.Convert("System.Collections.Generic.List<T>", new byte[50]);
            String8 noDelimiters = String8.Convert("No Delimiters", new byte[25]);
            PartialArray<int> partBuffer = new PartialArray<int>(10, false);

            // Goal: 256MB/sec [Surface Book i7]
            Verify.PerformanceByBytes(150 * LongExtensions.Megabyte, () =>
            {
                int iterations = 200000;
                for (int iteration = 0; iteration < iterations; ++iteration)
                {
                    list.Split(UTF8.Period, partBuffer);
                    noDelimiters.Split(UTF8.Period, partBuffer);
                }

                return iterations * (list.Length + noDelimiters.Length);
            });
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Serialization
{
    [TestClass]
    public class TsvWriterTests
    {
        [TestMethod]
        public void TsvWriter_Basics()
        {
            String8Block block = new String8Block();
            String8 valueNoEscaping = block.GetCopy("Sample Description");
            String8 valueEscaping = block.GetCopy("Value\tWith\nIssues");

            using (TsvWriter writer = new TsvWriter("TsvWriter.tsv"))
            {
                writer.SetColumns(new string[] { "LineNumber", "Count", "Description", "Source" });

                Assert.AreEqual(1, writer.RowCountWritten);

                int sum = 0;
                for (int i = 1; i <= 10; ++i)
                {
                    Assert.AreEqual(i, writer.RowCountWritten);

                    sum += i;
                    writer.Write(i);

                    writer.WriteValueStart();
                    writer.WriteValuePart((byte)'[');
                    writer.WriteValuePart(sum);
                    writer.WriteValuePart((byte)']');
                    writer.WriteValueEnd();

                    writer.Write(valueNoEscaping);
                    writer.Write(valueEscaping);

                    writer.NextRow();
                }
            }

            string tsvContent = File.ReadAllText("TsvWriter.tsv");

            // Verify header is as expected
            Assert.IsTrue(tsvContent.StartsWith("LineNumber\tCount\tDescription\tSource\r\n"));

            // Verify illegal characters are stripped
            Assert.IsTrue(tsvContent.Contains("ValueWithIssues"));

            // Verify the first row fully
            Assert.IsTrue(tsvContent.Contains("1\t[1]\tSample Description\tValueWithIssues\r\n"));
        }
    }
}

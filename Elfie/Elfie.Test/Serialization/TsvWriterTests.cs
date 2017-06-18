// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

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
            String8 t = block.GetCopy("T");

            using (TsvWriter writer = new TsvWriter("TsvWriter.tsv"))
            {
                writer.SetColumns(new string[] { "LineNumber", "IsEven", "Code", "When", "WhenDay", "Count", "Description", "Source" });

                Assert.AreEqual(0, writer.RowCountWritten);

                int sum = 0;
                for (int i = 0; i < 10; ++i)
                {
                    Assert.AreEqual(i, writer.RowCountWritten);

                    sum += i;

                    // Test write for non-String8 types
                    writer.Write(i);
                    writer.Write((i % 2 == 0));
                    writer.Write((byte)'Z');
                    writer.Write(new DateTime(2016, 02, 03, 04, 05, 06, DateTimeKind.Utc));
                    writer.Write(new DateTime(2016, 12, 23, 0, 0, 0, DateTimeKind.Utc));

                    // Test writing partial values
                    writer.WriteValueStart();
                    writer.WriteValuePart((byte)'[');
                    writer.WriteValuePart(sum);
                    writer.WriteValuePart(true);
                    writer.WriteValuePart(t);
                    writer.WriteValuePart((byte)']');
                    writer.WriteValueEnd();

                    // Test String8 writing with and without values to escape
                    writer.Write(valueNoEscaping);
                    writer.Write(valueEscaping);

                    writer.NextRow();
                }
            }

            string tsvContent = File.ReadAllText("TsvWriter.tsv");
            Trace.WriteLine(tsvContent);

            // Verify header is as expected
            Assert.IsTrue(tsvContent.StartsWith("LineNumber\tIsEven\tCode\tWhen\tWhenDay\tCount\tDescription\tSource\r\n"));

            // Verify illegal characters are stripped
            Assert.IsTrue(tsvContent.Contains("ValueWithIssues"));

            // Verify the first row fully
            Assert.IsTrue(tsvContent.Contains("1\tfalse\tZ\t2016-02-03T04:05:06Z\t2016-12-23\t[1trueT]\tSample Description\tValueWithIssues\r\n"));
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Xsv.Where;

namespace Xsv.Test.Where
{
    [TestClass]
    public class WhereMatcherTests
    {
        private static string s_sampleFilePath = "WhereSample.csv";

        [TestInitialize]
        public void Initialize()
        {
            String8Block block = new String8Block();
            String8[] names = new String8[] { block.GetCopy("Scott"), block.GetCopy("Mike"), block.GetCopy("Jeff"), block.GetCopy("Sophie") };

            using (ITabularWriter sample = TabularFactory.BuildWriter(s_sampleFilePath))
            {
                sample.SetColumns(new string[] { "ID", "IsEven", "Count", "WhenAdded", "Name" });

                int sum = 0;
                for (int i = 0; i < 1000; ++i)
                {
                    sum += i;

                    sample.Write(i);
                    sample.Write((i & 0x1) == 0);
                    sample.Write(sum);
                    sample.Write(new DateTime(2017, 05, 23).AddMinutes(i));
                    sample.Write(names[i % names.Length]);

                    sample.NextRow();
                }
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(s_sampleFilePath);
        }

        [TestMethod]
        public void WhereMatcher_Basics()
        {
            Assert.AreEqual(1000, WhereMatchCount(s_sampleFilePath, "0", ">=", "0"), "Should match all rows (by column index)");

            Assert.AreEqual(500, WhereMatchCount(s_sampleFilePath, "ID", ">=", "500"), "Should match half of rows (int)");
            Assert.AreEqual(500, WhereMatchCount(s_sampleFilePath, "IsEven", "==", "true"), "Should match half of rows (boolean)");
            Assert.AreEqual(90, WhereMatchCount(s_sampleFilePath, "WhenAdded", "<", "2017-05-23 01:30:00 AM"), "Should match 90 rows (DateTime)");
            Assert.AreEqual(250, WhereMatchCount(s_sampleFilePath, "Name", "=", "Sophie"), "Should match 250 rows (string)");

            Assert.AreEqual(750, WhereMatchCount(s_sampleFilePath, "Name", "!=", "Sophie"), "Should match 250 rows (string !=)");
            Assert.AreEqual(250, WhereMatchCount(s_sampleFilePath, "Name", "|>", "Sop"), "Should match 250 rows (string StartsWith)");
            Assert.AreEqual(250, WhereMatchCount(s_sampleFilePath, "Name", ":", "ophie"), "Should match 250 rows (string Contains)");
            Assert.AreEqual(250, WhereMatchCount(s_sampleFilePath, "Name", ">", "Scott"), "Should match 250 rows (string >)");
            Assert.AreEqual(500, WhereMatchCount(s_sampleFilePath, "Name", ">=", "Scott"), "Should match 500 rows (string >=)");
            Assert.AreEqual(500, WhereMatchCount(s_sampleFilePath, "Name", "<", "Scott"), "Should match 500 rows (string <)");
            Assert.AreEqual(750, WhereMatchCount(s_sampleFilePath, "Name", "<=", "Scott"), "Should match 750 rows (string <=)");

            Assert.AreEqual(0, WhereMatchCount(s_sampleFilePath, "Name", "!=", "false"), "Should match 0 rows (bool, can't convert type)");
            Assert.AreEqual(0, WhereMatchCount(s_sampleFilePath, "Name", "!=", "0"), "Should match 0 rows (int, can't convert type)");
            Assert.AreEqual(0, WhereMatchCount(s_sampleFilePath, "Name", "!=", "2017-01-01"), "Should match 0 rows (DateTime, can't convert type)");

            // Column name doesn't exist
            Verify.Exception<ColumnNotFoundException>(() => WhereMatchCount(s_sampleFilePath, "MissingColumn", "==", "Jeff"));

            // Column index out of range
            Verify.Exception<ColumnNotFoundException>(() => WhereMatchCount(s_sampleFilePath, "-1", "==", "Jeff"));

            // Unknown operator
            Verify.Exception<UsageException>(() => WhereMatchCount(s_sampleFilePath, "Name", "->", "Jeff"));

            // Try with output enabled
            using (ITabularReader reader = TabularFactory.BuildReader(s_sampleFilePath))
            {
                using (ITabularWriter writer = TabularFactory.BuildWriter("Sample.Under2.csv"))
                {
                    WhereMatcher.Where(reader, "ID", "<", "2", writer);
                    Assert.AreEqual(2, writer.RowCountWritten);
                }

                string content = File.ReadAllText("Sample.Under2.csv");
                Assert.IsTrue(content.Contains("\"0\""));
                Assert.IsTrue(content.Contains("\"1\""));
                Assert.IsFalse(content.Contains("\"2\""));
            }
        }

        private static int WhereMatchCount(string inputPath, string columnIdentifier, string op, string value)
        {
            using (ITabularReader reader = TabularFactory.BuildReader(inputPath))
            {
                return WhereMatcher.Where(reader, columnIdentifier, op, value, null).MatchCount;
            }
        }
    }
}

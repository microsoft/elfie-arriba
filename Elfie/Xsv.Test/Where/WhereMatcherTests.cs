using Elfie.Test;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using Xsv.Where;
using XsvConcat;

namespace Xsv.Test.Where
{
    [TestClass]
    public class WhereMatcherTests
    {
        private static string SampleFilePath = "WhereSample.csv";

        [TestInitialize]
        public void Initialize()
        {
            String8Block block = new String8Block();
            String8[] names = new String8[] { block.GetCopy("Scott"), block.GetCopy("Mike"), block.GetCopy("Jeff"), block.GetCopy("Sophie") };

            using (ITabularWriter sample = TabularFactory.BuildWriter(SampleFilePath))
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
            File.Delete(SampleFilePath);
        }

        [TestMethod]
        public void WhereMatcher_Basics()
        {
            Assert.AreEqual(1000, WhereMatchCount(SampleFilePath, "0", ">=", "0"), "Should match all rows (by column index)");

            Assert.AreEqual(500, WhereMatchCount(SampleFilePath, "ID", ">=", "500"), "Should match half of rows (int)");
            Assert.AreEqual(500, WhereMatchCount(SampleFilePath, "IsEven", "==", "true"), "Should match half of rows (boolean)");
            Assert.AreEqual(90, WhereMatchCount(SampleFilePath, "WhenAdded", "<", "2017-05-23 01:30:00 AM"), "Should match 90 rows (DateTime)");
            Assert.AreEqual(250, WhereMatchCount(SampleFilePath, "Name", "=", "Sophie"), "Should match 250 rows (string)");

            Assert.AreEqual(750, WhereMatchCount(SampleFilePath, "Name", "!=", "Sophie"), "Should match 250 rows (string !=)");
            Assert.AreEqual(250, WhereMatchCount(SampleFilePath, "Name", "|>", "Sop"), "Should match 250 rows (string StartsWith)");
            Assert.AreEqual(250, WhereMatchCount(SampleFilePath, "Name", ":", "ophie"), "Should match 250 rows (string Contains)");
            Assert.AreEqual(250, WhereMatchCount(SampleFilePath, "Name", ">", "Scott"), "Should match 250 rows (string >)");
            Assert.AreEqual(500, WhereMatchCount(SampleFilePath, "Name", ">=", "Scott"), "Should match 500 rows (string >=)");
            Assert.AreEqual(500, WhereMatchCount(SampleFilePath, "Name", "<", "Scott"), "Should match 500 rows (string <)");
            Assert.AreEqual(750, WhereMatchCount(SampleFilePath, "Name", "<=", "Scott"), "Should match 750 rows (string <=)");

            Assert.AreEqual(0, WhereMatchCount(SampleFilePath, "Name", "!=", "false"), "Should match 0 rows (bool, can't convert type)");
            Assert.AreEqual(0, WhereMatchCount(SampleFilePath, "Name", "!=", "0"), "Should match 0 rows (int, can't convert type)");
            Assert.AreEqual(0, WhereMatchCount(SampleFilePath, "Name", "!=", "2017-01-01"), "Should match 0 rows (DateTime, can't convert type)");

            // Column name doesn't exist
            Verify.Exception<ColumnNotFoundException>(() => WhereMatchCount(SampleFilePath, "MissingColumn", "==", "Jeff"));

            // Column index out of range
            Verify.Exception<ColumnNotFoundException>(() => WhereMatchCount(SampleFilePath, "-1", "==", "Jeff"));

            // Unknown operator
            Verify.Exception<UsageException>(() => WhereMatchCount(SampleFilePath, "Name", "->", "Jeff"));

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

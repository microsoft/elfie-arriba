// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Xsv.Sanitize;
using Elfie.Serialization;

namespace Xsv.Test
{
    [TestClass]
    public class SanitizerTests
    {
        [TestMethod]
        public void Sanitizer_Hashing_Basics()
        {
            // Verify Extract can extract 2x16 bits, 4x8 bits, 8x4 bits
            Assert.AreEqual(2, ExtractCount(uint.MaxValue, 65536));
            Assert.AreEqual(4, ExtractCount(uint.MaxValue, 256));
            Assert.AreEqual(8, ExtractCount(uint.MaxValue, 16));

            String8Block block = new String8Block();
            String8 one = block.GetCopy("One");
            String8 two = block.GetCopy("Two");

            // Verify hashes are stable
            Assert.AreEqual(Hashing.Hash(String8.Empty, 0), Hashing.Hash(String8.Empty, 0));
            Assert.AreEqual(Hashing.Hash(one, 0), Hashing.Hash(one, 0));
            Assert.AreEqual(Hashing.Hash(two, 0), Hashing.Hash(two, 0));

            // Verify hashes vary based on hashKey
            Assert.AreNotEqual(Hashing.Hash(String8.Empty, 1), Hashing.Hash(String8.Empty, 2));
            Assert.AreNotEqual(Hashing.Hash(one, 1), Hashing.Hash(one, 2));
            Assert.AreNotEqual(Hashing.Hash(two, 1), Hashing.Hash(two, 2));

            // Verify hashes vary based on value
            Assert.AreNotEqual(Hashing.Hash(String8.Empty, 3), Hashing.Hash(one, 3));
            Assert.AreNotEqual(Hashing.Hash(String8.Empty, 3), Hashing.Hash(two, 3));
            Assert.AreNotEqual(Hashing.Hash(one, 3), Hashing.Hash(two, 3));
        }

        private static int ExtractCount(uint hash, int countLimit)
        {
            int partsExtracted = 0;
            while (hash > 0)
            {
                int part = Hashing.Extract(ref hash, countLimit);
                partsExtracted++;
            }
            return partsExtracted;
        }

        [TestMethod]
        public void Sanitizer_Mapper_Basics()
        {
            SanitizerProvider p = new SanitizerProvider();

            // Verify each known mapper behaves as expected
            foreach (string mapperName in p.MapperTypes())
            {
                Trace.WriteLine(mapperName);
                Mapper_Basics(p.Mapper(mapperName));
            }

            // Verify extensibility added the working mapper and not the two invalid ones
            Assert.IsTrue(p.MapperTypes().Contains("Test"));
            Assert.IsFalse(p.MapperTypes().Contains("noEmptyCtor"));
            Assert.IsFalse(p.MapperTypes().Contains("typeNotFound"));

            // Verify exception when an unknown mapper is requested
            try
            {
                p.Mapper("MapperTypeWhichIsn'tDefined");
                Assert.Fail("SanitizerProvider.Mapper() should throw when asked for an unknown mapper.");
            }
            catch (UsageException)
            {
                // Pass
            }
        }

        private static void Mapper_Basics(ISanitizeMapper mapper)
        {
            // Verify mapper doesn't throw for min and max value and produces different values
            Assert.AreNotEqual(mapper.Generate(uint.MinValue), mapper.Generate(uint.MaxValue));

            // Verify no collisions for 10k values (using a consistent seed for repeatability)
            Random r = new Random(0);
            HashSet<uint> hashes = new HashSet<uint>();
            HashSet<string> results = new HashSet<string>();
            while (hashes.Count < 10000)
            {
                // If the hash isn't a duplicate, verify the result isn't either
                uint hash = (uint)r.Next();
                if (hashes.Add(hash))
                {
                    Assert.IsTrue(results.Add(mapper.Generate(hash)), $"Mapper {mapper.GetType().Name} produced collision for {hash}.");
                }
            }
        }

        [TestMethod]
        public void Sanitize_EndToEnd()
        {
            Assembly xsvTest = Assembly.GetExecutingAssembly();
            Resource.SaveStreamTo("Xsv.Test.Sanitize.SanitizeSampleSource.csv", "SanitizeSampleSource.csv", xsvTest);
            Resource.SaveStreamTo("Xsv.Test.Sanitize.SanitizeSampleSource.sanispec", "SanitizeSampleSource.sanispec", xsvTest);

            // Verify UsageException if no key is passed
            Assert.AreEqual(-2, Program.Main(new string[] { "sanitize", @"SanitizeSampleSource.csv", "SanitizeOutput.csv", @"SanitizeSampleSource.sanispec" }));

            // Verify success for base sanitize
            File.Delete("SanitizeOutput.csv");
            Assert.AreEqual(0, Program.Main(new string[] { "sanitize", @"SanitizeSampleSource.csv", "SanitizeOutput.csv", @"SanitizeSampleSource.sanispec", "Key1" }));

            // Validate the result
            using (ITabularReader r = TabularFactory.BuildReader("SanitizeOutput.csv"))
            {
                Assert.IsTrue(r.Columns.Contains("ID"), "ID column is kept (no spec line)");
                Assert.IsTrue(r.Columns.Contains("Path"), "Path column is kept (mapped)");
                Assert.IsTrue(r.Columns.Contains("IsEmptyPath"), "IsEmptyPath is kept (Keep line)");
                Assert.IsFalse(r.Columns.Contains("IsUnderXsv"), "IxUnderXsv column is dropped (Drop line)");

                int idColumnIndex = r.ColumnIndex("ID");
                int pathColumnIndex = r.ColumnIndex("Path");
                int isEmptyPathColumnIndex = r.ColumnIndex("IsEmptyPath");

                while (r.NextRow())
                {
                    int id = r.Current(idColumnIndex).ToInteger();
                    string path = r.Current(pathColumnIndex).ToString();

                    Assert.AreEqual(r.Current(isEmptyPathColumnIndex).ToBoolean(), String.IsNullOrEmpty(path), "IsEmptyPath condition matches whether mapped path is empty");

                    if (id == 5)
                    {
                        Assert.AreEqual("Elfie", path, "'Elfie' is echoed (Echo in spec)");
                    }
                    else if (!String.IsNullOrEmpty(path))
                    {
                        Assert.IsTrue(path.StartsWith("WarmBeggedTruth\\"), "Verify path is mapped in parts, and 'Elfie' is consistently mapped.");
                    }
                }

                Assert.IsTrue(r.RowCountRead < 7, "Verify sample excluded at least one row.");
            }

            // Run with another key
            Assert.AreEqual(0, Program.Main(new string[] { "sanitize", @"SanitizeSampleSource.csv", "SanitizeOutput2.csv", @"SanitizeSampleSource.sanispec", "Key2" }));

            // Verify mappings are different
            Assert.AreNotEqual(File.ReadAllText("SanitizeOutput2.csv"), File.ReadAllText("SanitizeOutput.csv"));
        }
    }
}

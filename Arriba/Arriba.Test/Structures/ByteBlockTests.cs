// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Structures
{
    [TestClass]
    public class ByteBlockTests
    {
        [TestMethod]
        public void ByteBlock_Null()
        {
            ByteBlock nullBlock = default(ByteBlock);
            ByteBlock nullBlock2 = (string)null;
            ByteBlock nullBlock3 = (byte[])null;
            ByteBlock valid = "valid";

            Assert.IsTrue(nullBlock.IsZero());
            Assert.IsTrue(nullBlock.Equals(nullBlock));
            Assert.IsFalse(nullBlock.Equals(valid));
            Assert.IsFalse(valid.Equals(nullBlock));

            Assert.IsTrue(nullBlock == nullBlock2);
            Assert.IsFalse(nullBlock == valid);
            Assert.IsFalse(valid == nullBlock);

            Assert.IsFalse(nullBlock != nullBlock2);
            Assert.IsTrue(nullBlock != valid);
            Assert.IsTrue(valid != nullBlock);

            Assert.IsTrue(nullBlock.CompareTo(nullBlock2) == 0);
        }

        [TestMethod]
        public void ByteBlock_StringConversion()
        {
            ByteBlock b = "Hello";
            Assert.AreEqual(5, b.Length);
            Assert.AreEqual("Hello", b.ToString());

            ByteBlock c = String.Empty;
            Assert.AreEqual(0, c.Length);
            Assert.AreEqual(String.Empty, c.ToString());
        }

        [TestMethod]
        public void ByteBlock_StringFunctions()
        {
            ByteBlock b = "HelLo";
            ByteBlock o = "other value";

            b.ToLowerInvariant();
            Assert.AreEqual("hello", b.ToString());

            b.ToLowerInvariant();
            Assert.AreEqual("hello", b.ToString());

            string nonAsciiString = "\u8A18\u8F09\u306B\u3064\u3044\u3066";
            ByteBlock c = nonAsciiString;
            c.ToLowerInvariant();
            Assert.AreEqual(nonAsciiString, c.ToString());

            ByteBlock d = "HELLO";

            // Verify case-insensitive compare matches when it should
            Assert.AreEqual(0, b.CaseInsensitiveCompareTo(d));
            Assert.AreEqual(0, d.CaseInsensitiveCompareTo(b));

            // Verify case sensitive doesn't match when case differences
            Assert.AreNotEqual(0, b.CompareTo(d));

            // Verify "not same type" doesn't match
            Assert.AreNotEqual(0, b.CompareTo(-1));

            // Verify totally different string doesn't match
            Assert.AreNotEqual(0, b.CaseInsensitiveCompareTo(o));
            Assert.AreNotEqual(0, o.CaseInsensitiveCompareTo(b));
            Assert.AreNotEqual(0, b.CompareTo(o));
            Assert.AreNotEqual(0, o.CompareTo(b));
        }

        [TestMethod]
        public void ByteBlock_EqualsAndStartsWith()
        {
            ByteBlock edi = "edi";

            Assert.IsTrue(edi.IsPrefixOf("") != 0);
            Assert.IsTrue(((ByteBlock)"").IsPrefixOf(edi) == 0);
            Assert.IsTrue(edi.Equals("edi"));
            Assert.IsFalse(edi.Equals(7));

            Assert.IsTrue(edi.IsPrefixOf("debug") > 0);
            Assert.IsTrue(edi.IsPrefixOf("ed") > 0);
            Assert.IsTrue(edi.CaseInsensitiveIsPrefixOf("ED") > 0);
            Assert.AreEqual(0, edi.IsPrefixOf("edi"));
            Assert.AreNotEqual(0, edi.IsPrefixOf("EDI"));
            Assert.AreEqual(0, edi.CaseInsensitiveIsPrefixOf("edi"));
            Assert.AreEqual(0, edi.IsPrefixOf("editor"));
            Assert.IsTrue(edi.IsPrefixOf("function") < 0);

            Assert.IsTrue(edi.CompareTo("debug") > 0);
            Assert.IsTrue(edi.CompareTo("ed") > 0);
            Assert.AreEqual(0, edi.CompareTo("edi"));
            Assert.AreEqual(0, edi.CompareTo((object)"edi"));
            Assert.IsTrue(edi.CompareTo("editor") < 0);
            Assert.IsTrue(edi.CompareTo("function") < 0);

            Assert.AreEqual(0, edi.CaseInsensitiveIsPrefixOf("EDITOR"));
            Assert.AreNotEqual(0, edi.CaseInsensitiveIsPrefixOf("ED"));
        }

        [TestMethod]
        public void ByteBlock_GetHashCode()
        {
            ByteBlock edi = "Edi";
            int hash = edi.GetHashCode();

            edi.ToLowerInvariant();
            int lowerHash = edi.GetHashCode();
            Assert.AreNotEqual(lowerHash, hash);

            Assert.AreEqual(lowerHash, edi.GetHashCode());
        }

        [TestMethod]
        public void ByteBlock_ArrayOperations()
        {
            ByteBlock editor = "editor";
            ByteBlock hello = "hello";

            hello.CopyTo(editor);
            Assert.AreEqual("hellor", editor);

            byte[] helloBytes = new byte[5];
            hello.CopyTo(helloBytes);
            Assert.AreEqual("hello", new ByteBlock(helloBytes).ToString());
        }

        [TestMethod]
        public void ByteBlockComparison()
        {
            ByteBlock editor = "editor";
            IComparable<ByteBlock> comparer;

            comparer = editor.GetExtendedIComparable(ByteBlock.Comparison.IsPrefixOf);
            Assert.AreNotEqual(0, comparer.CompareTo("EDITOR"));
            Assert.AreEqual(0, comparer.CompareTo("editors"));
            Assert.AreNotEqual(0, comparer.CompareTo("EDITORS"));
            Assert.AreNotEqual(0, comparer.CompareTo("edi"));

            comparer = editor.GetExtendedIComparable(ByteBlock.Comparison.CaseInsensitiveIsPrefixOf);
            Assert.AreEqual(0, comparer.CompareTo("EDITOR"));
            Assert.AreEqual(0, comparer.CompareTo("editors"));
            Assert.AreEqual(0, comparer.CompareTo("EDITORS"));
            Assert.AreNotEqual(0, comparer.CompareTo("edi"));

            comparer = editor.GetExtendedIComparable(ByteBlock.Comparison.CaseInsensitiveCompareTo);
            Assert.AreEqual(0, comparer.CompareTo("EDITOR"));
            Assert.AreNotEqual(0, comparer.CompareTo("editors"));
            Assert.AreNotEqual(0, comparer.CompareTo("EDITORS"));
            Assert.AreNotEqual(0, comparer.CompareTo("edi"));
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void ByteBlock_Performance_ToLowerInvariant()
        {
            // Goal: ToLower is ~5 instructions per character, so 2,500 instructions per iteration. At 2M instructions per ms, 800 iterations per ms. [Or 400kb/ms]
            // Length: 15b + 42b + 443b = 500b
            int iterations = 25000;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                ByteBlock b = "UpperCase Value";
                b.ToLowerInvariant();

                ByteBlock c = "this value is already completely lowercase";
                c.ToLowerInvariant();

                ByteBlock d = "Remove Arriba dependencies which aren't easily available in NuGet, so that it's easier for the open source community to consume. Remove Arriba dependencies which aren't easily available in NuGet, so that it's easier for the open source community to consume. Remove Arriba dependencies which aren't easily available in NuGet, so that it's easier for the open source community to consume. Anyway, we need 400 bytes here, so we need a few more...";
                d.ToLowerInvariant();
            }

            int operations = (3 * iterations);
            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = operations / milliseconds;
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.", operations, milliseconds, operationsPerMillisecond));

            Assert.IsTrue(operationsPerMillisecond > 400, "Not within 200% of goal.");
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void ByteBlock_Performance_IsPrefixOf()
        {
            // Goal: IsPrefixOf is ~100 instructions for typical single words (~10 char), so at 2M instructions per ms, 20,000 per ms.
            ByteBlock[] words = new ByteBlock[] { "sample", "editor", "values", "provided", "edi", "sam", "friendly", "extremely", "active", "actively" };

            int matchCount = 0;
            int iterations = 10000;
            Stopwatch w = Stopwatch.StartNew();

            for (int iteration = 0; iteration < iterations; ++iteration)
            {
                for (int i = 0; i < words.Length; ++i)
                {
                    for (int j = 0; j < words.Length; ++j)
                    {
                        int result = words[i].IsPrefixOf(words[j]);
                        if (result == 0) ++matchCount;
                    }
                }
            }

            // Verify 13 matches per pass - every value matches itself and "edi".IsPrefixOf("editor"); "sam".IsPrefixOf("sample"); "active".IsPrefixOf("actively")
            Assert.AreEqual(13 * iterations, matchCount);

            int operations = (100 * iterations);
            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = operations / milliseconds;
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.", operations, milliseconds, operationsPerMillisecond));
            Assert.IsTrue(operationsPerMillisecond > 10000, "Not within 200% of goal.");
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void ByteBlock_Performance_CaseInsensitiveIsPrefixOf()
        {
            // Goal: IsPrefixOf is ~100 instructions for typical single words (~10 char), so at 2M instructions per ms, 20,000 per ms.
            ByteBlock[] words = new ByteBlock[] { "saMple", "editor", "values", "provided", "Edi", "sam", "friendly", "extremely", "active", "actively" };

            int matchCount = 0;
            int iterations = 10000;
            Stopwatch w = Stopwatch.StartNew();

            for (int iteration = 0; iteration < iterations; ++iteration)
            {
                for (int i = 0; i < words.Length; ++i)
                {
                    for (int j = 0; j < words.Length; ++j)
                    {
                        int result = words[i].CaseInsensitiveIsPrefixOf(words[j]);
                        if (result == 0) ++matchCount;
                    }
                }
            }

            // Verify 13 matches per pass - every value matches itself and "edi".IsPrefixOf("editor"); "sam".IsPrefixOf("sample"); "active".IsPrefixOf("actively")
            Assert.AreEqual(13 * iterations, matchCount);

            int operations = (100 * iterations);
            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = operations / milliseconds;
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.", operations, milliseconds, operationsPerMillisecond));
            Assert.IsTrue(operationsPerMillisecond > 10000, "Not within 200% of goal.");
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void ByteBlock_Performance_CompareTo()
        {
            // Goal: CompareTo is ~100 instructions for typical single words (~10 char), so at 2M instructions per ms, 20,000 per ms.
            ByteBlock[] words = new ByteBlock[] { "sample", "editor", "values", "provided", "edi", "sam", "friendly", "extremely", "active", "actively" };

            int matchCount = 0;
            int iterations = 10000;
            Stopwatch w = Stopwatch.StartNew();

            for (int iteration = 0; iteration < iterations; ++iteration)
            {
                for (int i = 0; i < words.Length; ++i)
                {
                    for (int j = 0; j < words.Length; ++j)
                    {
                        int result = words[i].CompareTo(words[j]);
                        if (result == 0) ++matchCount;
                    }
                }
            }

            // Verify 10 matches per pass - every value matches itself only
            Assert.AreEqual(10 * iterations, matchCount);

            int operations = (100 * iterations);
            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = operations / milliseconds;
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.", operations, milliseconds, operationsPerMillisecond));
            Assert.IsTrue(operationsPerMillisecond > 10000, "Not within 200% of goal.");
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void ByteBlock_Performance_GetHashCode()
        {
            // Goal: GetHashCode is ~25 instructions per 8 bytes plus ~25 instructions per byte for the last <8 bytes.
            //  Average ~200 instructions (25 * 4) prefix + (25 * 4) suffix; at 2M/ms, expect 10k/ms
            ByteBlock[] words = new ByteBlock[] { "sample", "editor", "values", "provided", "edi", "sam", "friendly", "extremely", "active", "actively" };

            int iterations = 100000;
            Stopwatch w = Stopwatch.StartNew();

            for (int iteration = 0; iteration < iterations; ++iteration)
            {
                for (int i = 0; i < words.Length; ++i)
                {
                    int hash = words[i].GetHashCode();
                }
            }

            int operations = (10 * iterations);
            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = operations / milliseconds;
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.", operations, milliseconds, operationsPerMillisecond));
            Assert.IsTrue(operationsPerMillisecond > 5000, "Not within 200% of goal.");
        }
    }
}

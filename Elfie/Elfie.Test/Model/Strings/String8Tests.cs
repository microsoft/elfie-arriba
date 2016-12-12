// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model.Strings
{
    [TestClass]
    public class String8Tests
    {
        [TestMethod]
        public void String8_Basics()
        {
            // Create a shared byte buffer for String8 conversions
            byte[] buffer = new byte[1024 + 4];

            // Null and Empty conversion
            Assert.IsTrue(String8.Convert(null, buffer).IsEmpty());
            Assert.IsTrue(String8.Convert(String.Empty, buffer).IsEmpty());

            // Null and Empty comparison [both ways]
            Assert.AreEqual(0, new String8(null, 0, 0).CompareTo(String8.Convert(String.Empty, buffer)));
            Assert.AreEqual(0, String8.Convert(String.Empty, buffer).CompareTo(new String8(null, 0, 0)));

            // Sort sample strings by ordinal
            string[] samples = new string[] { "A", "AA", "AAA", "short", "four", "!@#$%^&*()", "A\U00020213C", "Diagnostics", "NonePadded", new string('z', 1024) };
            Array.Sort(samples, StringComparer.Ordinal);

            // Convert each string into a new array [allocation is obvious in API]
            String8[] converted = new String8[samples.Length];
            for (int i = 0; i < samples.Length; ++i)
            {
                int length = String8.GetLength(samples[i]);
                converted[i] = String8.Convert(samples[i], new byte[length]);
            }

            // Verify round-trip
            StringBuilder sb = new StringBuilder();
            using (StringWriter writer = new StringWriter(sb))
            {
                for (int i = 0; i < samples.Length; ++i)
                {
                    // Round-Trip via ToString()
                    Assert.AreEqual(samples[i], converted[i].ToString());

                    // Round-Trip via byte[]
                    int length = converted[i].WriteTo(buffer, 0);
                    Assert.AreEqual(samples[i], Encoding.UTF8.GetString(buffer, 0, length));

                    // Round-Trip via TextWriter
                    converted[i].WriteTo(writer);
                    Assert.AreEqual(samples[i], sb.ToString());
                    sb.Clear();
                }
            }

            // Verify non-empty and length
            for (int i = 0; i < samples.Length; ++i)
            {
                Assert.IsFalse(converted[i].IsEmpty());

                int expectedLength = Encoding.UTF8.GetByteCount(samples[i]);
                Assert.AreEqual(expectedLength, converted[i].Length);
            }

            // Verify they compare correctly
            for (int i = 1; i < samples.Length; ++i)
            {
                // This is less than the one after it
                Assert.IsTrue(converted[i - 1].CompareTo(converted[i]) < 0);

                // This is more than the one before it
                Assert.IsTrue(converted[i].CompareTo(converted[i - 1]) > 0);

                // This equals itself
                Assert.IsTrue(converted[i].CompareTo(converted[i]) == 0);

                // This equals a new instance of the same value
                Assert.IsTrue(converted[i].CompareTo(String8.Convert(samples[i], buffer)) == 0);

                // This is greater than a prefix of itself
                Assert.IsTrue(converted[i].CompareTo(String8.Convert(samples[i], 0, samples[i].Length - 1, buffer)) > 0);

                // This is less than itself plus another character
                Assert.IsTrue(converted[i].CompareTo(String8.Convert(samples[i] + "A", buffer)) < 0);
            }
        }

        [TestMethod]
        public void String8_CaseInsensitive()
        {
            // Verify simple ASCII case works both ways
            EnsureComparesConsistent("Array", "array");
            EnsureComparesConsistent("array", "Array");

            // Verify fully uppercase works
            EnsureComparesConsistent("Array", "ARRAY");

            // Verify longer value still is later
            EnsureComparesConsistent("Array", "ArrayA");

            // Verify "nothing to change" results work
            EnsureComparesConsistent("ARRAY", "ARRAY");

            // Verify non-letters aren't confused (before and after letters)
            EnsureComparesConsistent("\r", "\n");
            EnsureComparesConsistent("0", "1");
            EnsureComparesConsistent("@", "`");
            EnsureComparesConsistent("[", "{");
            EnsureComparesConsistent("}", "]");

            // Verify non-ASCII letters are consistent with .NET
            EnsureComparesConsistent("zed", "\u00C6ble");
            EnsureComparesConsistent("apple", "\u00C6ble");
            EnsureComparesConsistent("\u00E5ngstr\u00F6m", "zed");
            EnsureComparesConsistent("\u00E5ngstr\u00F6m", "apple");
        }

        [TestMethod]
        public void String8_IndexOf()
        {
            string binaryName = "System.Collections.Generic.List";
            String8 binaryName8 = String8.Convert(binaryName, new byte[String8.GetLength(binaryName)]);
            Assert.AreEqual(binaryName.IndexOf('.'), binaryName8.IndexOf((byte)'.'));
            Assert.AreEqual(binaryName.IndexOf('.', 18), binaryName8.IndexOf((byte)'.', 18));
            Assert.AreEqual(binaryName.LastIndexOf('.'), binaryName8.LastIndexOf((byte)'.'));
            Assert.AreEqual(binaryName.LastIndexOf('.', 18), binaryName8.LastIndexOf((byte)'.', 18));
        }

        [TestMethod]
        public void String8_Prefix()
        {
            String8 full = String8.Convert("One.Two.Three", new byte[13]);
            String8 start = String8.Convert("One", new byte[3]);
            String8 part = String8.Convert("Two", new byte[3]);
            String8 startInsensitive = String8.Convert("ONE", new byte[3]);

            Assert.AreEqual(0, start.CompareAsPrefixTo(full));
        }

        [TestMethod]
        public void String8_ToInteger()
        {
            Assert.AreEqual(-1, TryToInteger(null));
            Assert.AreEqual(-1, TryToInteger(String.Empty));
            Assert.AreEqual(5, TryToInteger("5"));
            Assert.AreEqual(12345, TryToInteger("12345"));
            Assert.AreEqual(-1, TryToInteger("-6"));
            Assert.AreEqual(int.MaxValue, TryToInteger(int.MaxValue.ToString()));
            Assert.AreEqual(-1, TryToInteger("123g"));
            Assert.AreEqual(-1, TryToInteger("9999999999"));
            Assert.AreEqual(-1, TryToInteger("12345678901234567890"));
        }

        [TestMethod]
        public void String8_FromInteger()
        {
            byte[] buffer = new byte[11];
            Assert.AreEqual("0", String8.FromInteger(0, buffer).ToString());
            Assert.AreEqual("9", String8.FromInteger(9, buffer).ToString());
            Assert.AreEqual("-1", String8.FromInteger(-1, buffer).ToString());
            Assert.AreEqual("-10", String8.FromInteger(-10, buffer).ToString());
            Assert.AreEqual("99", String8.FromInteger(99, buffer).ToString());
            Assert.AreEqual("100", String8.FromInteger(100, buffer).ToString());
            Assert.AreEqual("-999", String8.FromInteger(-999, buffer).ToString());
            Assert.AreEqual("123456789", String8.FromInteger(123456789, buffer).ToString());
            Assert.AreEqual(int.MaxValue.ToString(), String8.FromInteger(int.MaxValue, buffer).ToString());
            Assert.AreEqual(int.MinValue.ToString(), String8.FromInteger(int.MinValue, buffer).ToString());
        }

        private int TryToInteger(string value)
        {
            String8 value8 = String8.Convert(value, new byte[String8.GetLength(value)]);
            return value8.ToInteger();
        }

        [TestMethod]
        public void String8_ToUpper()
        {
            // Verify no exception
            String8.Empty.ToUpperInvariant();

            String8 sample = String8.Convert("abcABC", new byte[6]);
            sample.ToUpperInvariant();
            Assert.AreEqual("ABCABC", sample.ToString());
        }

        [TestMethod]
        public void String8_GetHashCode()
        {
            byte[] buffer = new byte[20];
            String8 value;

            HashSet<int> hashes = new HashSet<int>();
            int collisions = 0;

            for (int i = 0; i < 10000; ++i)
            {
                value = String8.Convert(i.ToString(), buffer);
                int valueHash = value.GetHashCode();
                if (!hashes.Add(valueHash)) collisions++;
            }

            Assert.AreEqual(0, collisions);
        }

        public enum CompareResult
        {
            Less,
            Equal,
            More
        }

        private CompareResult ToResult(int compareReturnValue)
        {
            if (compareReturnValue < 0) return CompareResult.Less;
            if (compareReturnValue == 0) return CompareResult.Equal;
            return CompareResult.More;
        }

        private void EnsureComparesConsistent(string left, string right)
        {
            String8 left8 = String8.Convert(left, new byte[String8.GetLength(left)]);
            String8 right8 = String8.Convert(right, new byte[String8.GetLength(right)]);

            CompareResult caseSensitiveExpected = ToResult(String.Compare(left, right, StringComparison.Ordinal));
            CompareResult caseInsensitiveExpected = ToResult(String.Compare(left, right, StringComparison.OrdinalIgnoreCase));

            Assert.AreEqual(caseSensitiveExpected, ToResult(left8.CompareTo(right8)), "Case sensitive comparison result incorrect.");
            Assert.AreEqual(caseInsensitiveExpected, ToResult(left8.CompareTo(right8, true)), "Case insensitive comparison result incorrect.");

            Assert.AreEqual(caseSensitiveExpected, ToResult(left8.CompareTo(right)), "Case sensitive String8 to string comparison result incorrect.");
            Assert.AreEqual(caseInsensitiveExpected, ToResult(left8.CompareTo(right, true)), "Case insensitive String8 to string comparison result incorrect.");

            // StartsWith and CompareAsPrefixTo
            Assert.AreEqual(left.StartsWith(right), left8.StartsWith(right8));
            Assert.AreEqual(left.StartsWith(right, StringComparison.OrdinalIgnoreCase), left8.StartsWith(right8, true));
            Assert.AreEqual(right.StartsWith(left), right8.StartsWith(left8));
            Assert.AreEqual(right.StartsWith(left, StringComparison.OrdinalIgnoreCase), right8.StartsWith(left8, true));

            // Case Insensitive Stable is the insensitive order, then the sensitive order for ties
            CompareResult caseInsensitiveStableExpected = (caseInsensitiveExpected == CompareResult.Equal ? caseSensitiveExpected : caseInsensitiveExpected);
            Assert.AreEqual(caseInsensitiveStableExpected, ToResult(left8.CompareCaseInsensitiveStableTo(right8)), "Case insensitive stable String8 to string comparison result incorrect.");
        }

#if !DEBUG
        [TestMethod]
#endif
        public void String8_ComparePerformance()
        {
            // Ten sample strings
            string[] strings = { null, "", "Array", "ArrayList", "Boolean", "Collections", "Dictionary", "Dictionary<string, int>", "System.Collections.Generic.Array", "System.Collections.Generic.ArrayList" };
            String8[] values = new String8[strings.Length];

            // Convert into two buffers, half to each
            byte[] buffer = new byte[1024];
            byte[] buffer2 = new byte[1024];
            int usedSpace = 0;
            for (int i = 0; i < strings.Length; ++i)
            {
                values[i] = String8.Convert(strings[i], (i % 2 == 0 ? buffer : buffer2), usedSpace);
                usedSpace += values[i].Length;
            }

            // Compare every combination of values (half within and half across buffers)

            // Goal: 500k/sec [case sensitive]
            int iterations = 100000;
            Verify.PerformanceByOperation(500 * LongExtensions.Thousand, () =>
            {
                RunAllComparisons(values, false, iterations);
                return iterations * values.Length * values.Length;
            });

            // Goal: 400k/sec [case insensitive]
            Verify.PerformanceByOperation(400 * LongExtensions.Thousand, () =>
            {
                RunAllComparisons(values, true, iterations);
                return iterations * values.Length * values.Length;
            });
        }

        private void RunAllComparisons(String8[] values, bool ignoreCase, int iterations)
        {
            for (int iteration = 0; iteration < iterations; ++iteration)
            {
                for (int i = 0; i < values.Length; ++i)
                {
                    for (int j = 0; j < values.Length; ++j)
                    {
                        // Case sensitive
                        int cmp = values[i].CompareTo(values[j], ignoreCase);
                    }
                }
            }
        }
    }
}

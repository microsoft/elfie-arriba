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

            string collections = "Collections";
            String8 collections8 = String8.Convert(collections, new byte[String8.GetLength(collections)]);
            Assert.AreEqual(binaryName.IndexOf(collections), binaryName8.IndexOf(collections8));
            Assert.AreEqual(binaryName.IndexOf(collections, 7), binaryName8.IndexOf(collections8, 7));
            Assert.AreEqual(binaryName.IndexOf(collections, 8), binaryName8.IndexOf(collections8, 8));

            string lists = "Lists";
            String8 lists8 = String8.Convert(lists, new byte[String8.GetLength(lists)]);
            Assert.AreEqual(binaryName.IndexOf(lists), binaryName8.IndexOf(lists8));
            Assert.AreEqual(binaryName.IndexOf(lists, 28), binaryName8.IndexOf(lists8, 28));

            string list = "List";
            String8 list8 = String8.Convert(list, new byte[String8.GetLength(list)]);
            Assert.AreEqual(binaryName.IndexOf(list), binaryName8.IndexOf(list8));
            Assert.AreEqual(binaryName.IndexOf(list, 20), binaryName8.IndexOf(list8, 20));
            Assert.AreEqual(binaryName.IndexOf(list, 28), binaryName8.IndexOf(list8, 28));
        }

        [TestMethod]
        public void String8_BeforeFirstAfterFirst()
        {
            string binaryName = "System.Collections.Generic.List!";
            String8 binaryName8 = String8.Convert(binaryName, new byte[String8.GetLength(binaryName)]);

            Assert.AreEqual("System", binaryName8.BeforeFirst((byte)'.').ToString());
            Assert.AreEqual("Collections.Generic.List!", binaryName8.AfterFirst((byte)'.').ToString());

            Assert.AreEqual(binaryName8, binaryName8.BeforeFirst((byte)'|').ToString());
            Assert.AreEqual(binaryName8, binaryName8.AfterFirst((byte)'|').ToString());

            Assert.AreEqual(string.Empty, String8.Empty.BeforeFirst((byte)'.').ToString());
            Assert.AreEqual(string.Empty, String8.Empty.AfterFirst((byte)'.').ToString());

            Assert.AreEqual(string.Empty, String8.Empty.BeforeFirst((byte)'S').ToString());
            Assert.AreEqual(string.Empty, String8.Empty.AfterFirst((byte)'!').ToString());
        }

        [TestMethod]
        public void String8_StartsWithEndsWith()
        {
            string collections = "Collections";
            String8 collections8 = String8.Convert(collections, new byte[String8.GetLength(collections)]);

            string collectionsCasing = "coLLecTionS";
            String8 collectionsCasing8 = String8.Convert(collectionsCasing, new byte[String8.GetLength(collectionsCasing)]);

            Assert.IsFalse(String8.Empty.StartsWith(UTF8.Space));
            Assert.IsFalse(String8.Empty.EndsWith(UTF8.Space));

            Assert.IsTrue(collections8.StartsWith((byte)'C'));
            Assert.IsFalse(collections8.StartsWith((byte)'c'));
            Assert.IsFalse(collections8.StartsWith(UTF8.Newline));

            Assert.IsTrue(collections8.EndsWith((byte)'s'));
            Assert.IsFalse(collections8.EndsWith((byte)'S'));
            Assert.IsFalse(collections8.EndsWith(UTF8.Newline));

            Assert.IsFalse(String8.Empty.StartsWith(collections8));
            Assert.IsFalse(String8.Empty.EndsWith(collections8));
            Assert.IsFalse(String8.Empty.StartsWith(collections8, true));
            Assert.IsFalse(String8.Empty.EndsWith(collections8, true));

            Assert.IsTrue(collections8.EndsWith(collections8));
            Assert.IsTrue(collections8.EndsWith(collections8.Substring(1)));
            Assert.IsTrue(collections8.EndsWith(collections8.Substring(8)));
            Assert.IsFalse(collections8.EndsWith(collectionsCasing8));
            Assert.IsTrue(collections8.EndsWith(collectionsCasing8, true));

            Assert.IsTrue(collections8.StartsWith(collections8));
            Assert.IsTrue(collections8.StartsWith(collections8.Substring(0, collections8.Length - 1)));
            Assert.IsTrue(collections8.StartsWith(collections8.Substring(0, 3)));
            Assert.IsFalse(collections8.StartsWith(collectionsCasing8));
            Assert.IsTrue(collections8.StartsWith(collectionsCasing8, true));
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
        public void String8_TryToBoolean()
        {
            Assert.AreEqual(null, TryToBoolean(null));
            Assert.AreEqual(null, TryToBoolean("-1"));
            Assert.AreEqual(null, TryToBoolean("2"));
            Assert.AreEqual(null, TryToBoolean("tru"));
            Assert.AreEqual(null, TryToBoolean("falsey"));

            Assert.AreEqual(false, TryToBoolean("0"));
            Assert.AreEqual(false, TryToBoolean("false"));
            Assert.AreEqual(false, TryToBoolean("False"));
            Assert.AreEqual(false, TryToBoolean("FALSE"));

            Assert.AreEqual(true, TryToBoolean("1"));
            Assert.AreEqual(true, TryToBoolean("true"));
            Assert.AreEqual(true, TryToBoolean("True"));
            Assert.AreEqual(true, TryToBoolean("TRUE"));
        }

        private bool? TryToBoolean(string value)
        {
            String8 value8 = String8.Convert(value, new byte[String8.GetLength(value) + 1], 1);

            bool? result = null;
            bool parsed = false;
            if (value8.TryToBoolean(out parsed))
            {
                result = parsed;
            }

            return result;
        }

        [TestMethod]
        public void String8_TryToInteger()
        {
            Assert.AreEqual(null, TryToInteger(null));
            Assert.AreEqual(null, TryToInteger(String.Empty));
            Assert.AreEqual(5, TryToInteger("5"));
            Assert.AreEqual(12345, TryToInteger("12345"));
            Assert.AreEqual(-6, TryToInteger("-6"));
            Assert.AreEqual(-1, TryToInteger("-1"));
            Assert.AreEqual(0, TryToInteger("0"));
            Assert.AreEqual(1, TryToInteger("1"));
            Assert.AreEqual(int.MaxValue, TryToInteger(int.MaxValue.ToString()));
            Assert.AreEqual(int.MinValue, TryToInteger(int.MinValue.ToString()));
            Assert.AreEqual(null, TryToInteger("123g"));
            Assert.AreEqual(null, TryToInteger("9999999999"));
            Assert.AreEqual(null, TryToInteger("12345678901234567890"));
        }

        private int? TryToInteger(string value)
        {
            String8 value8 = String8.Convert(value, new byte[String8.GetLength(value) + 1], 1);

            int? result = null;
            int parsed = 0;
            if (value8.TryToInteger(out parsed))
            {
                result = parsed;
            }

            return result;
        }

        [TestMethod]
        public void String8_FromInteger()
        {
            byte[] buffer = new byte[11];
            Assert.AreEqual("0", String8.FromInteger(0, buffer).ToString());
            Assert.AreEqual("00", String8.FromInteger(0, buffer, 0, 2).ToString());
            Assert.AreEqual("9", String8.FromInteger(9, buffer).ToString());
            Assert.AreEqual("-1", String8.FromInteger(-1, buffer).ToString());
            Assert.AreEqual("-10", String8.FromInteger(-10, buffer).ToString());
            Assert.AreEqual("99", String8.FromInteger(99, buffer).ToString());
            Assert.AreEqual("0099", String8.FromInteger(99, buffer, 0, 4).ToString());
            Assert.AreEqual("100", String8.FromInteger(100, buffer).ToString());
            Assert.AreEqual("-999", String8.FromInteger(-999, buffer).ToString());
            Assert.AreEqual("123456789", String8.FromInteger(123456789, buffer).ToString());
            Assert.AreEqual(int.MaxValue.ToString(), String8.FromInteger(int.MaxValue, buffer).ToString());
            Assert.AreEqual(int.MinValue.ToString(), String8.FromInteger(int.MinValue, buffer).ToString());
        }

        [TestMethod]
        public void String8_FromDateTime()
        {
            byte[] buffer = new byte[20];
            Assert.AreEqual("0001-01-01", String8.FromDateTime(DateTime.MinValue, buffer).ToString());
            Assert.AreEqual("2017-02-14", String8.FromDateTime(new DateTime(2017, 02, 14, 0, 0, 0, DateTimeKind.Utc), buffer).ToString());
            Assert.AreEqual("2017-02-14T01:02:03Z", String8.FromDateTime(new DateTime(2017, 02, 14, 1, 2, 3, DateTimeKind.Utc), buffer).ToString());
            Assert.AreEqual("9999-12-31T23:59:59Z", String8.FromDateTime(DateTime.MaxValue, buffer).ToString());
        }

        [TestMethod]
        public void String8_TryToDateTime()
        {
            // Null/Empty
            Assert.AreEqual(null, TryToDateTime(null));
            Assert.AreEqual(null, TryToDateTime(String.Empty));

            // Valid, ISO 8601
            Assert.AreEqual(new DateTime(2017, 02, 15, 0, 0, 0, DateTimeKind.Utc), TryToDateTime("2017-02-15"));
            Assert.AreEqual(new DateTime(2017, 02, 15, 11, 33, 54, DateTimeKind.Utc), TryToDateTime("2017-02-15T11:33:54Z"));
            Assert.AreEqual(new DateTime(2017, 02, 15, 11, 33, 54, DateTimeKind.Utc), TryToDateTime("2017-02-15 11:33:54Z"));
            Assert.AreEqual(new DateTime(1, 2, 3, 4, 5, 6, DateTimeKind.Utc), TryToDateTime("0001-02-03T04:05:06Z"));
            Assert.AreEqual(new DateTime(1, 2, 3, 4, 5, 6, DateTimeKind.Utc), TryToDateTime("0001-02-03T04:05:06"));

            // Valid, US format
            Assert.AreEqual(new DateTime(2017, 02, 15, 0, 0, 0, DateTimeKind.Utc), TryToDateTime("02/15/2017"));
            Assert.AreEqual(new DateTime(2017, 02, 15, 11, 33, 54, DateTimeKind.Utc), TryToDateTime("02/15/2017 11:33:54"));
            Assert.AreEqual(new DateTime(2017, 02, 15, 11, 33, 54, DateTimeKind.Utc), TryToDateTime("02/15/2017 11:33:54Z"));

            // Min/Max
            Assert.AreEqual(DateTime.MinValue, TryToDateTime("0001-01-01T00:00:00Z"));
            Assert.AreEqual(new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc), TryToDateTime("9999-12-31T23:59:59Z"));

            // Bad separators
            Assert.AreEqual(null, TryToDateTime("2017:02-15T11:33:54Z"));
            Assert.AreEqual(null, TryToDateTime("2017-02:15T11:33:54Z"));
            Assert.AreEqual(null, TryToDateTime("2017-02-15T11-33:54Z"));
            Assert.AreEqual(null, TryToDateTime("2017-02-15T11:33-54Z"));
            Assert.AreEqual(null, TryToDateTime("2017-02-15t11:33:54Z"));
            Assert.AreEqual(null, TryToDateTime("2017-02-15T11:33:54 "));
            Assert.AreEqual(null, TryToDateTime("2017|02-15"));
            Assert.AreEqual(null, TryToDateTime("2017-02|15"));
            Assert.AreEqual(null, TryToDateTime("02-15/2017"));
            Assert.AreEqual(null, TryToDateTime("02/15-2017"));
            Assert.AreEqual(null, TryToDateTime("02/15-2017 11:33:54"));
            Assert.AreEqual(null, TryToDateTime("02/15/2017 11-33:54Z"));
            Assert.AreEqual(null, TryToDateTime("02/15/2017 11:33-54Z"));
            Assert.AreEqual(null, TryToDateTime("02/15/2017 11:33-54 "));

            // Bad numbers
            Assert.AreEqual(null, TryToDateTime("-017-02-15T11:33:54Z"));
            Assert.AreEqual(null, TryToDateTime("2017-13-15T11:33:54Z"));
            Assert.AreEqual(null, TryToDateTime("2017-01-32T11:33:54Z"));
            Assert.AreEqual(null, TryToDateTime("2017-02-15T24:33:54Z"));
            Assert.AreEqual(null, TryToDateTime("2017-02-15T11:60:54Z"));
            Assert.AreEqual(null, TryToDateTime("2017-02-15T11:33:60Z"));
            Assert.AreEqual(null, TryToDateTime("mm/15/2017"));
            Assert.AreEqual(null, TryToDateTime("02/dd/2017"));
            Assert.AreEqual(null, TryToDateTime("02/15/yyyy"));
            Assert.AreEqual(null, TryToDateTime("15/02/2017 11:33:54"));
            Assert.AreEqual(null, TryToDateTime("02/32/2017 11:33:54"));
            Assert.AreEqual(null, TryToDateTime("02/15/2017 24:33:54"));
            Assert.AreEqual(null, TryToDateTime("02/15/2017 11:60:54"));
            Assert.AreEqual(null, TryToDateTime("02/15/2017 11:33:60"));

            // Leap year handling
            Assert.AreEqual(new DateTime(2016, 02, 29, 0, 0, 0, DateTimeKind.Utc), TryToDateTime("2016-02-29"));
            Assert.AreEqual(null, TryToDateTime("2017-02-29"));
        }

        private DateTime? TryToDateTime(string value)
        {
            String8 value8 = String8.Convert(value, new byte[String8.GetLength(value) + 1], 1);

            DateTime? result = null;
            DateTime parsed = DateTime.MinValue;
            if (value8.TryToDateTime(out parsed))
            {
                result = parsed;
            }

            return result;
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

#if PERFORMANCE
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

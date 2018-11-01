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
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model.Strings
{
    public static class StringExtensions
    {
        public static String8 TestConvert(this string value)
        {
            // Convert the string to a String8, *but not at the very start of the buffer*.
            // This is a common source of bugs.
            return String8.Convert(value, new byte[String8.GetLength(value) + 1], 1);
        }
    }

    [TestClass]
    public class String8Tests
    {
        [TestMethod]
        public void String8_Basics()
        {
            // Create a shared byte buffer for String8 conversions
            byte[] buffer = new byte[1024 + 4];

            // Null and Empty conversion
            Assert.IsTrue(((string)null).TestConvert().IsEmpty());
            Assert.IsTrue(string.Empty.TestConvert().IsEmpty());

            // Null and Empty comparison [both ways]
            Assert.AreEqual(0, new String8(null, 0, 0).CompareTo(string.Empty.TestConvert()));
            Assert.AreEqual(0, string.Empty.TestConvert().CompareTo(new String8(null, 0, 0)));

            // Sort sample strings by ordinal
            string[] samples = new string[] { "A", "AA", "AAA", "short", "four", "!@#$%^&*()", "A\U00020213C", "Diagnostics", "NonePadded", new string('z', 1024) };
            Array.Sort(samples, StringComparer.Ordinal);

            // Convert each string into a new array [allocation is obvious in API]
            String8[] converted = new String8[samples.Length];
            for (int i = 0; i < samples.Length; ++i)
            {
                int length = String8.GetLength(samples[i]);
                converted[i] = String8.Convert(samples[i], new byte[length + 1], 1);
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
                Assert.IsTrue(converted[i].CompareTo(samples[i].TestConvert()) == 0);

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
            String8 binaryName8 = binaryName.TestConvert();
            Assert.AreEqual(binaryName.IndexOf('.'), binaryName8.IndexOf((byte)'.'));
            Assert.AreEqual(binaryName.IndexOf('.', 18), binaryName8.IndexOf((byte)'.', 18));
            Assert.AreEqual(binaryName.LastIndexOf('.'), binaryName8.LastIndexOf((byte)'.'));
            Assert.AreEqual(binaryName.LastIndexOf('.', 18), binaryName8.LastIndexOf((byte)'.', 18));

            string collections = "Collections";
            String8 collections8 = collections.TestConvert();
            Assert.AreEqual(binaryName.IndexOf(collections), binaryName8.IndexOf(collections8));
            Assert.AreEqual(binaryName.IndexOf(collections, 7), binaryName8.IndexOf(collections8, 7));
            Assert.AreEqual(binaryName.IndexOf(collections, 8), binaryName8.IndexOf(collections8, 8));

            string lists = "Lists";
            String8 lists8 = lists.TestConvert();
            Assert.AreEqual(binaryName.IndexOf(lists), binaryName8.IndexOf(lists8));
            Assert.AreEqual(binaryName.IndexOf(lists, 28), binaryName8.IndexOf(lists8, 28));

            string list = "List";
            String8 list8 = list.TestConvert();
            Assert.AreEqual(binaryName.IndexOf(list), binaryName8.IndexOf(list8));
            Assert.AreEqual(binaryName.IndexOf(list, 20), binaryName8.IndexOf(list8, 20));
            Assert.AreEqual(binaryName.IndexOf(list, 28), binaryName8.IndexOf(list8, 28));

            Assert.AreEqual(-1, binaryName8.IndexOf(String8.Empty));
        }

        [TestMethod]
        public void String8_IndexOfAll()
        {
            // Three matches for 's' case sensitive
            Assert.AreEqual("2, 17, 29", IndexOfAll("System.Collections.Generic.List", "s", false));

            // Four matches case insensitive
            Assert.AreEqual("0, 2, 17, 29", IndexOfAll("System.Collections.Generic.List", "s", true));

            // Match right at end and after end
            Assert.AreEqual("27", IndexOfAll("System.Collections.Generic.List", "list", true));
            Assert.AreEqual("", IndexOfAll("System.Collections.Generic.List", "Lists", true));

            // Full Match
            Assert.AreEqual("0", IndexOfAll("System", "system", true));

            // Overlapping matches
            Assert.AreEqual("0, 1, 2, 3", IndexOfAll("AAAAAA", "aaa", true));
        }

        private static string IndexOfAll(string text, string value, bool ignoreCase)
        {
            String8 text8 = text.TestConvert();
            String8 value8 = value.TestConvert();

            StringBuilder result = new StringBuilder();

            int nextIndex = 0;
            int[] matches = new int[2];
            while (true)
            {
                int matchCount = text8.IndexOfAll(value8, nextIndex, ignoreCase, matches);

                for (int i = 0; i < matchCount; ++i)
                {
                    if (result.Length > 0) result.Append(", ");
                    result.Append(matches[i]);
                }

                if (matchCount < matches.Length) break;
                nextIndex = matches[matchCount - 1] + 1;
            }

            return result.ToString();
        }

        [TestMethod]
        public void String8_ContainsVariants()
        {
            Assert.AreEqual(0, "".TestConvert().IndexOfOrdinalIgnoreCase("".TestConvert()), "Empty always contains empty");
            Assert.AreEqual(1, "Simple".TestConvert().IndexOfOrdinalIgnoreCase("imp".TestConvert()), "Case sensitive match");
            Assert.AreEqual(1, "Simple".TestConvert().IndexOfOrdinalIgnoreCase("IMP".TestConvert()), "Case insensitive matching");
            Assert.AreEqual(-1, "Simple".TestConvert().IndexOfOrdinalIgnoreCase("imz".TestConvert()), "Non-match in last character only");
            Assert.AreEqual(0, "Simple".TestConvert().IndexOfOrdinalIgnoreCase("sim".TestConvert()), "Match at start, case insensitive");
            Assert.AreEqual(0, "Simple".TestConvert().IndexOfOrdinalIgnoreCase("simple".TestConvert()), "Full match, case insensitive");
            Assert.AreEqual(-1, "Simple".TestConvert().IndexOfOrdinalIgnoreCase("simpler".TestConvert()), "Non-match because too long");
            Assert.AreEqual(5, "Simple".TestConvert().IndexOfOrdinalIgnoreCase("e".TestConvert()), "Match at last character only");
            Assert.AreEqual(4, "Simple".TestConvert().IndexOfOrdinalIgnoreCase("le".TestConvert()), "Match at end");
            Assert.AreEqual(-1, "Simple".TestConvert().IndexOfOrdinalIgnoreCase("er".TestConvert()), "Non-match trailing off end");
            Assert.AreEqual(3, "bananas".TestConvert().IndexOfOrdinalIgnoreCase("anas".TestConvert()), "Overlapping match");

            Assert.AreEqual(0, "Simple things to match".TestConvert().Contains("simp".TestConvert()), "Match at beginning of string");
            Assert.AreEqual(7, "Simple things to match".TestConvert().Contains("thin".TestConvert()), "Match at beginning of word");
            Assert.AreEqual(-1, "Simple things to match".TestConvert().Contains("imp".TestConvert()), "Match, but not at word start");
            Assert.AreEqual(17, "Simple things to match".TestConvert().Contains("m".TestConvert()), "Match after first attempt");
            Assert.AreEqual(17, "Simple things to match".TestConvert().Contains("match".TestConvert()), "Match at end of string");
            Assert.AreEqual(-1, "Simple things to match".TestConvert().Contains("matche".TestConvert()), "Match off end of string");

            Assert.AreEqual(0, "Simple things to match".TestConvert().ContainsExact("simple".TestConvert()), "Match first word");
            Assert.AreEqual(7, "Simple things to match".TestConvert().ContainsExact("things".TestConvert()), "Match middle word");
            Assert.AreEqual(17, "Simple things to match".TestConvert().ContainsExact("match".TestConvert()), "Match last word");
            Assert.AreEqual(-1, "Simple things to match".TestConvert().ContainsExact("Simpl".TestConvert()), "Non-full-word match (not start)");
            Assert.AreEqual(-1, "Simple things to match".TestConvert().ContainsExact("imple".TestConvert()), "Non-full-word match (not end)");
            Assert.AreEqual(-1, "Simple things to match".TestConvert().ContainsExact("matc".TestConvert()), "Non-full-word match (not start)");
            Assert.AreEqual(-1, "Simple things to match".TestConvert().ContainsExact("atch".TestConvert()), "Non-full-word match (not end)");
        }

        [TestMethod]
        public void String8_BeforeFirstAfterFirst()
        {
            string binaryName = "System.Collections.Generic.List!";
            String8 binaryName8 = binaryName.TestConvert();

            Assert.AreEqual("System", binaryName8.BeforeFirst((byte)'.').ToString());
            Assert.AreEqual("Collections.Generic.List!", binaryName8.AfterFirst((byte)'.').ToString());

            Assert.AreEqual(binaryName8, binaryName8.BeforeFirst((byte)'|').ToString());
            Assert.AreEqual(binaryName8, binaryName8.AfterFirst((byte)'|').ToString());

            Assert.AreEqual(string.Empty, String8.Empty.BeforeFirst((byte)'.').ToString());
            Assert.AreEqual(string.Empty, String8.Empty.AfterFirst((byte)'.').ToString());

            Assert.AreEqual(string.Empty, String8.Empty.BeforeFirst((byte)'S').ToString());
            Assert.AreEqual(string.Empty, String8.Empty.AfterFirst((byte)'!').ToString());

            TrySplitOnFirst(String8.Empty, (byte)'.', "", "");
            TrySplitOnFirst(binaryName8, (byte)'@', "", "");
            TrySplitOnFirst(binaryName8, (byte)'.', "System", "Collections.Generic.List!");
            TrySplitOnFirst(binaryName8, (byte)'!', "System.Collections.Generic.List", "");
            TrySplitOnFirst(binaryName8, (byte)'S', "", "ystem.Collections.Generic.List!");
        }

        private static void TrySplitOnFirst(String8 value, byte splitter, string firstExpected, string secondExpected)
        {
            String8 first, second;
            Assert.AreEqual(!(firstExpected.Length == 0 && secondExpected.Length == 0), value.SplitOnFirst(splitter, out first, out second));
            Assert.AreEqual(firstExpected, first.ToString());
            Assert.AreEqual(secondExpected, second.ToString());
        }

        [TestMethod]
        public void String8_Trim()
        {
            Assert.AreEqual(String8.Empty, String8.Empty.Trim());

            String8 sample = " \t\r\nSample\t\n   ".TestConvert();
            Assert.AreEqual("Sample", sample.Trim().ToString());
            Assert.AreEqual("Sample", sample.Trim().Trim().ToString());
        }

        [TestMethod]
        public void String8_TrimEnd()
        {
            String8 sample = "Interesting   ".TestConvert();
            Assert.AreEqual("Interesting", sample.TrimEnd(UTF8.Space).ToString());
            Assert.AreEqual(sample.ToString(), sample.TrimEnd(UTF8.Tab).ToString());
            Assert.AreEqual(string.Empty, String8.Empty.TrimEnd(UTF8.Space).ToString());
            Assert.AreEqual(string.Empty, "   ".TestConvert().TrimEnd(UTF8.Space).ToString());
            Assert.AreEqual("A", "A   ".TestConvert().TrimEnd(UTF8.Space).ToString());
        }

        [TestMethod]
        public void String8_StartsWithEndsWith()
        {
            string collections = "Collections";
            String8 collections8 = collections.TestConvert();

            string collectionsCasing = "coLLecTionS";
            String8 collectionsCasing8 = collectionsCasing.TestConvert();

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
        public void String8_ShiftBack()
        {
            String8Block block = new String8Block();

            // Goal: Split on semi-colon, collapse semi-colon and spaces in-place
            String8 shiftable = "One; Two;Three; Four".TestConvert();
            int totalShift = 0;

            String8Set parts = shiftable.Split(UTF8.Semicolon, new PartialArray<int>(5, false));
            for (int i = 0; i < parts.Count; ++i)
            {
                String8 part = parts[i];

                totalShift++;
                if (part.StartsWith(UTF8.Space))
                {
                    part = part.Substring(1);
                    totalShift++;
                }

                String8 beforeShift = block.GetCopy(part);
                String8 shifted = part.ShiftBack(totalShift);
                Assert.AreEqual(beforeShift, shifted);
            }

            String8 result = shiftable.Substring(0, shiftable.Length - totalShift);
            Assert.AreNotEqual("OneTwoThreeFour", result.ToString());
        }

        [TestMethod]
        public void String8_Prefix()
        {
            String8 full = "One.Two.Three".TestConvert();
            String8 start = "One".TestConvert();
            String8 part = "Two".TestConvert();
            String8 startInsensitive = "ONE".TestConvert();

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
            String8 value8 = value.TestConvert();

            bool? result = null;
            bool parsed = false;
            if (value8.TryToBoolean(out parsed))
            {
                result = parsed;
            }

            return result;
        }

        [TestMethod]
        public void String8_NumberConversions()
        {
            TryNumberConversions("-0");

            TryNumberConversions(null);
            TryNumberConversions(string.Empty);
            TryNumberConversions("0");
            TryNumberConversions("-1");
            TryNumberConversions("1");
            TryNumberConversions("--1");
            TryNumberConversions("-");
            TryNumberConversions("123A");
            TryNumberConversions(int.MinValue.ToString());
            TryNumberConversions(int.MaxValue.ToString());
            TryNumberConversions(((long)int.MaxValue + 1).ToString());
            TryNumberConversions(long.MinValue.ToString());
            TryNumberConversions(long.MaxValue.ToString());
            TryNumberConversions(((ulong)long.MaxValue + 1).ToString());
            TryNumberConversions(ulong.MaxValue.ToString());
            TryNumberConversions("18446744073709551616"); // ulong.MaxValue + 1
            TryNumberConversions(ulong.MaxValue.ToString() + "0");
            TryNumberConversions(new string((char)(UTF8.Zero - 1), 19)); // Worst case for digit overflow; will convert as if each digit is 255
        }

        private static void TryNumberConversions(string value)
        {
            String8 value8 = value.TestConvert();

            // .NET Parses "-0" successfully as int and long, but not ulong.
            // I don't want "-0" to be considered valid on any parse.
            if (value == "-0") value = "Invalid";

            int expectedInt, actualInt;
            Assert.AreEqual(int.TryParse(value, out expectedInt), value8.TryToInteger(out actualInt));
            Assert.AreEqual(expectedInt, actualInt);

            long expectedLong, actualLong;
            Assert.AreEqual(long.TryParse(value, out expectedLong), value8.TryToLong(out actualLong));
            Assert.AreEqual(expectedLong, actualLong);

            ulong expectedULong, actualULong;
            Assert.AreEqual(ulong.TryParse(value, out expectedULong), value8.TryToULong(out actualULong));
            Assert.AreEqual(expectedULong, actualULong);
        }

        [TestMethod]
        public void String8_FloatConversions()
        {
            // TODO: XForm doesn't parse base and exponent notation yet; add tests when it does.

            TryFloatConversions(null);
            TryFloatConversions(string.Empty);
            TryFloatConversions("0");
            TryFloatConversions("-1.5");
            TryFloatConversions("12345.67890");
            TryFloatConversions("0.123456789");
            TryFloatConversions(".123456789");
            TryFloatConversions(int.MaxValue.ToString());
            TryFloatConversions(long.MinValue.ToString());
            TryFloatConversions(ulong.MaxValue.ToString());
        }

        private static void TryFloatConversions(string value)
        {
            String8 value8 = value.TestConvert();

            double expectedDouble, actualDouble;
            Assert.AreEqual(double.TryParse(value, out expectedDouble), value8.TryToDouble(out actualDouble));
            Assert.AreEqual(expectedDouble, actualDouble);
        }

        [TestMethod]
        public void String8_FromInteger()
        {
            byte[] buffer = new byte[20];
            Assert.AreEqual("0", String8.FromInteger(0, buffer).ToString());
            Assert.AreEqual("00", String8.FromInteger(0, buffer, 1, 2).ToString());
            Assert.AreEqual("9", String8.FromInteger(9, buffer).ToString());
            Assert.AreEqual("-1", String8.FromInteger(-1, buffer).ToString());
            Assert.AreEqual("-10", String8.FromInteger(-10, buffer, 1).ToString());
            Assert.AreEqual("99", String8.FromInteger(99, buffer).ToString());
            Assert.AreEqual("0099", String8.FromInteger(99, buffer, 0, 4).ToString());
            Assert.AreEqual("100", String8.FromInteger(100, buffer).ToString());
            Assert.AreEqual("-999", String8.FromInteger(-999, buffer).ToString());
            Assert.AreEqual("123456789", String8.FromInteger(123456789, buffer).ToString());
            Assert.AreEqual(int.MaxValue.ToString(), String8.FromInteger(int.MaxValue, buffer).ToString());
            Assert.AreEqual(int.MinValue.ToString(), String8.FromInteger(int.MinValue, buffer).ToString());
        }

        [TestMethod]
        public void String8_FromNumber_Double()
        {
            // TODO: XForm doesn't write to base and exponent notation yet; add tests when it does.

            byte[] buffer = new byte[21];
            Assert.AreEqual("0", String8.FromNumber(0.0, buffer).ToString());
            Assert.AreEqual("1000", String8.FromNumber(1000.0, buffer).ToString());
            Assert.AreEqual("1.5", String8.FromNumber(1.5, buffer).ToString());
            Assert.AreEqual("-1.5", String8.FromNumber(-1.5, buffer).ToString());
            Assert.AreEqual("0.666666666666666", String8.FromNumber((double)2 / (double)3, buffer).ToString());
            Assert.AreEqual("0.75", String8.FromNumber((double)3 / (double)4, buffer).ToString());
            Assert.AreEqual("0.0075", String8.FromNumber(0.0075, buffer).ToString());
            Assert.AreEqual(int.MaxValue.ToString(), String8.FromNumber((double)int.MaxValue, buffer).ToString());
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
            Assert.AreEqual(new DateTime(1, 2, 3, 4, 5, 6, DateTimeKind.Utc), TryToDateTime("0001-02-03T04:05:06.00"));
            Assert.AreEqual(new DateTime(1, 2, 3, 4, 5, 6, DateTimeKind.Utc), TryToDateTime("0001-02-03T04:05:06.00000"));
            Assert.AreEqual(new DateTime(1, 2, 3, 4, 5, 6, 789, DateTimeKind.Utc), TryToDateTime("0001-02-03T04:05:06.789"));

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
            String8 value8 = value.TestConvert();

            DateTime? result = null;
            DateTime parsed = DateTime.MinValue;
            if (value8.TryToDateTime(out parsed))
            {
                result = parsed;
            }

            return result;
        }

        [TestMethod]
        public void String8_FromTimeSpan()
        {
            byte[] buffer = new byte[21];
            Assert.AreEqual("00:00:00", String8.FromTimeSpan(TimeSpan.Zero, buffer).ToString());
            Assert.AreEqual("1.00:00:00", String8.FromTimeSpan(TimeSpan.Parse("1.00:00:00"), buffer).ToString());
            Assert.AreEqual("1.09:08:07", String8.FromTimeSpan(TimeSpan.Parse("1.09:08:07"), buffer).ToString());
            Assert.AreEqual("09:08:07.654", String8.FromTimeSpan(TimeSpan.Parse("09:08:07.654"), buffer).ToString());
            Assert.AreEqual("10675199.02:48:05.477", String8.FromTimeSpan(TimeSpan.MaxValue, buffer).ToString());
        }

        [TestMethod]
        public void String8_TryToTimeSpan()
        {
            // Null/Empty
            TryToTimeSpan(null);
            TryToTimeSpan(String.Empty);

            // Days only
            TryToTimeSpan("100");
            TryToTimeSpan("-50");

            // Some parts only
            TryToTimeSpan("12:40");
            TryToTimeSpan("12:40:50");
            TryToTimeSpan("7.12:40:50");
            TryToTimeSpan("12:40:50.750");

            // All parts
            TryToTimeSpan("7.12:30:31.500");
            TryToTimeSpan("-7.12:30:31.500");

            // Min/Max
            TryToTimeSpan(TimeSpan.MaxValue.ToString());
            TryToTimeSpan(TimeSpan.MinValue.ToString());

            // Max ticks length and too long
            TryToTimeSpan("7.12:30:31.1234567");
            TryToTimeSpan("7.12:30:31.12345678");

            // Bad Separators
            TryToTimeSpan("7|12:30:31.123");
            TryToTimeSpan("7.12|30:31.123");
            TryToTimeSpan("7.12:30|31.123");
            TryToTimeSpan("7.12:30:31|123");

            // Out of range numbers
            TryToTimeSpan("10675200");
            //TryToTimeSpan("24:00:00"); // BUG: TimeSpan.Parse("24:00:00") succeeds and returns 24 days.
            TryToTimeSpan("12:60:00");
            TryToTimeSpan("12:00:60");

            // Non-numeric
            TryToTimeSpan("a.12:30:31.123");
            TryToTimeSpan("7.a2:30:31.123");
            TryToTimeSpan("7.12:a0:31.123");
            TryToTimeSpan("7.12:30:a1.123");
            TryToTimeSpan("7.12:30:31.a23");
        }

        private void TryToTimeSpan(string value)
        {
            String8 value8 = value.TestConvert();

            TimeSpan expected;
            bool shouldSucceed = TimeSpan.TryParse(value, out expected);

            TimeSpan actual;
            bool didSucceed = value8.TryToTimeSpan(out actual);

            Assert.AreEqual(shouldSucceed, didSucceed, $"Success wrong for '{value}'");
            Assert.AreEqual(expected, actual, $"Result wrong for '{value}'");
        }

        [TestMethod]
        public void String8_TryToTimeSpanFriendly()
        {
            // Null/Empty
            TryToTimeSpanFriendly(null, null);
            TryToTimeSpanFriendly(null, String.Empty);

            // Passthrough to normal format
            TryToTimeSpanFriendly(TimeSpan.Parse("7.12:30:31.500"), "7.12:30:31.500");
            TryToTimeSpanFriendly(TimeSpan.Parse("-7.12:30:31.500"), "-7.12:30:31.500");

            // Try each scale
            TryToTimeSpanFriendly(TimeSpan.FromMilliseconds(500), "500ms");
            TryToTimeSpanFriendly(TimeSpan.FromSeconds(30), "30s");
            TryToTimeSpanFriendly(TimeSpan.FromHours(8), "8h");
            TryToTimeSpanFriendly(TimeSpan.FromDays(7), "7d");

            // Try negative values
            TryToTimeSpanFriendly(TimeSpan.FromDays(-60), "-60d");
        }

        private void TryToTimeSpanFriendly(TimeSpan? expected, string value)
        {
            String8 value8 = value.TestConvert();

            TimeSpan actual;
            bool succeeded = value8.TryToTimeSpanFriendly(out actual);

            Assert.AreEqual(expected.HasValue, succeeded, $"Success wrong for '{value}'");
            Assert.AreEqual(expected ?? TimeSpan.Zero, actual, $"Result wrong for '{value}'");
        }

        [TestMethod]
        public void String8_ToUpperToLower()
        {
            // Verify no exception
            String8.Empty.ToUpperInvariant();
            String8.Empty.ToLowerInvariant();

            String8 sample = "abcABC@[`{".TestConvert();
            sample.ToUpperInvariant();
            Assert.AreEqual("ABCABC@[`{", sample.ToString());

            sample = "abcABC@[`{".TestConvert();
            sample.ToLowerInvariant();
            Assert.AreEqual("abcabc@[`{", sample.ToString());
        }

        [TestMethod]
        public void String8_GetHashCode()
        {
            byte[] buffer = new byte[21];
            String8 value;

            HashSet<int> hashes = new HashSet<int>();
            int collisions = 0;

            for (int i = 0; i < 10000; ++i)
            {
                value = String8.Convert(i.ToString(), buffer, 1);
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
            String8 left8 = left.TestConvert();
            String8 right8 = right.TestConvert();

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

        // TryToLong is ~3x faster than the .NET default long.TryParse.
#if PERFORMANCE
        [TestMethod]
#endif
        public void String8_ToIntegerPerformance()
        {
            string one = "123456789";
            String8 one8 = one.TestConvert();

            long value = 0;
            long sum = 0;

            int iterations = 1 * 1000 * 1000;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                one8.TryToLong(out value);
                sum += value;
            }
            w.Stop();

            sum = 0;
            Stopwatch w2 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                long.TryParse(one, out value);
                sum += value;
            }
            w2.Stop();

            // Validate TryToLong is at least 2x as fast as long.TryParse
            Assert.IsTrue(w.ElapsedMilliseconds * 2 < w2.ElapsedMilliseconds);
            Trace.WriteLine($"{w.ElapsedMilliseconds:n0}ms String8.TryToLong; {w2.ElapsedMilliseconds:n0}ms long.TryParse.");
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

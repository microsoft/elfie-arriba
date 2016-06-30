// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Model.Index;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model.Index
{
    [TestClass]
    public class MemberIndexTests
    {
        [TestMethod]
        public void MemberIndex_Basic()
        {
            StringStore strings = new StringStore();
            MemberIndex index = new MemberIndex();

            // Add six sample strings to StringStore
            string[] testValues = new string[] { "Zero", "One", "Two", "Three", "Four", "Five" };
            int[] testValueIDs = new int[testValues.Length];
            for (int i = 0; i < testValues.Length; ++i)
            {
                testValueIDs[i] = strings.FindOrAddString(testValues[i]);
            }

            // Add 100 items to index - each item has the values it is evenly divisible by (10 has "Five" and "Two")
            for (int indexId = 1; indexId < 20; ++indexId)
            {
                for (int wordIndex = 1; wordIndex < testValueIDs.Length; ++wordIndex)
                {
                    if (indexId % wordIndex == 0) index.AddItem(testValueIDs[wordIndex], indexId);
                }
            }

            // Convert for search
            strings.ConvertToImmutable();
            index.ConvertToImmutable(strings);

            // Verify matches for three are correct
            Assert.AreEqual("3, 6, 9, 12, 15, 18", MatchesForWordToString(index, strings, strings[testValueIDs[3]]));
            Assert.AreEqual("3, 6, 9, 12, 15, 18", MatchesForPrefixToString(index, strings, String8.Convert("Three", new byte[String8.GetLength("Three")])));

            // Verify matches for five are correct
            Assert.AreEqual("5, 10, 15", MatchesForWordToString(index, strings, strings[testValueIDs[5]]));
            Assert.AreEqual("5, 10, 15", MatchesForPrefixToString(index, strings, String8.Convert("Five", new byte[String8.GetLength("Five")])));

            // Verify no matches for zero
            Assert.AreEqual("", MatchesForWordToString(index, strings, strings[testValueIDs[0]]));
            Assert.AreEqual("", MatchesForPrefixToString(index, strings, String8.Convert("Zero", new byte[String8.GetLength("Zero")])));

            // Verify "Four" and "Five" matches for "F"
            Assert.AreEqual("5, 10, 15, 4, 8, 12, 16", MatchesForPrefixToString(index, strings, String8.Convert("F", new byte[String8.GetLength("F")])));
        }

        [TestMethod]
        public void MemberIndex_CaseSensitivity()
        {
            StringStore strings = new StringStore();
            MemberIndex index = new MemberIndex();
            byte[] buffer = new byte[20];

            // Add strings to store (some differ only by casing), ten values
            string[] testValues = new string[] { "null", "bool", "Bool", "array", "ARRAY", "Collections", "Dictionary", "int", "Int", "friend" };
            int[] testValueIDs = new int[testValues.Length];
            for (int i = 0; i < testValues.Length; ++i)
            {
                testValueIDs[i] = strings.FindOrAddString(testValues[i]);
            }

            // Add 3 items per string to index [0, 10, 20 => "null", 1, 11, 21 => "bool", 2, 12, 22 => "Bool", ...]
            int indexId = 0;
            for (int countToIndex = 0; countToIndex < 3; ++countToIndex)
            {
                for (int wordIndex = 0; wordIndex < testValueIDs.Length; ++wordIndex)
                {
                    index.AddItem(testValueIDs[wordIndex], indexId++);
                }
            }

            // Convert index for search. Pages should be merged into case-insensitive groups in insertion (ID) order
            strings.ConvertToImmutable();
            index.ConvertToImmutable(strings);

            // Verify "BOOL" gets matches for "bool" and "Bool" in insertion order
            Assert.AreEqual("1, 2, 11, 12, 21, 22", MatchesForWordToString(index, strings, String8.Convert("BOOL", buffer)));

            // Verify "array" gets matches for "array" and "ARRAY" in insertion order
            Assert.AreEqual("3, 4, 13, 14, 23, 24", MatchesForWordToString(index, strings, String8.Convert("array", buffer)));

            // Verify "Dictionary" matches unmerged
            Assert.AreEqual("6, 16, 26", MatchesForWordToString(index, strings, String8.Convert("Dictionary", buffer)));
        }

        private string MatchesForPrefixToString(MemberIndex index, StringStore strings, String8 prefix)
        {
            Range matches;
            if (!strings.TryGetRangeStartingWith(prefix, out matches)) return String.Empty;
            return MatchesToString(index, matches);
        }

        private string MatchesForWordToString(MemberIndex index, StringStore strings, String8 word)
        {
            Range matches;
            if (!strings.TryFindString(word, out matches)) return String.Empty;
            return MatchesToString(index, matches);
        }

        private string MatchesToString(MemberIndex index, Range stringRange)
        {
            // Find matches for string range
            int[] matchBlock;
            int matchIndex, matchCount;
            bool success = index.TryGetMatchesInRange(stringRange, out matchBlock, out matchIndex, out matchCount);

            // Ensure success means there were matches
            Assert.AreEqual(success, matchCount > 0);

            // Convert results to comma delimited string
            StringBuilder results = new StringBuilder();
            for (int i = matchIndex; i < matchIndex + matchCount; ++i)
            {
                if (results.Length > 0) results.Append(", ");
                results.Append(matchBlock[i].ToString());
            }

            return results.ToString();
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Arriba.Indexing;
using Arriba.Serialization;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Structures
{
    [TestClass]
    public class WordIndexTests
    {
        [TestMethod]
        public void WordIndex_Basic()
        {
            WordIndex index = new WordIndex(new DefaultWordSplitter());

            // Verify no matches initially
            Assert.AreEqual("", GetMatchExact(index, "anything"));

            // Set a value
            index.Index(1, "", "this is the original value!");

            // Verify strings in word match
            Assert.AreEqual("1", GetMatchExact(index, "this"));
            Assert.AreEqual("1", GetMatchExact(index, "value"));

            // Verify partial words don't match exact, do match
            Assert.AreEqual("", GetMatchExact(index, "thi"));
            Assert.AreEqual("1", GetMatches(index, "thi"));

            // Change the value
            index.Index(1, "this is the original value!", "this is the updated value!");

            // Verify removed strings no longer match
            Assert.AreEqual("", GetMatches(index, "original"));

            // Verify maintained and added strings continue to match
            Assert.AreEqual("1", GetMatchExact(index, "value"));
            Assert.AreEqual("1", GetMatchExact(index, "updated"));

            // Index a few more items
            index.Index(2, "", "I have a different original value.");
            index.Index(3, "", "value, Yet a third");

            Assert.AreEqual("1, 2, 3", GetMatchExact(index, "value"));
            Assert.AreEqual("2", GetMatchExact(index, "original"));
        }

        [TestMethod]
        public void WordIndex_ResizeAndRemove()
        {
            WordIndex index = new WordIndex(new DefaultWordSplitter());

            // Verify index totally empty to start
            Assert.AreEqual("", GetIndexData(index));

            // Add words so that we have one with all items, two with half, and several unique words
            index.Index(0, "", "one sample original sample");
            index.Index(1, "", "two sample original sample");
            index.Index(2, "", "three sample original");
            index.Index(3, "", "four other original");
            index.Index(4, "", "five other original");
            index.Index(5, "", "six other original");

            Assert.AreEqual("0, 1, 2, 3, 4, 5", GetMatchExact(index, "original"));
            Assert.AreEqual("0, 1, 2", GetMatchExact(index, "sample"));
            Assert.AreEqual("0", GetMatchExact(index, "one"));

            // Remove a unique word. Add a second and fourth value. Keep a word unchanged.
            index.Index(0, "one sample original sample", "three other original");
            Assert.AreEqual("", GetMatchExact(index, "one"));
            Assert.AreEqual("0, 1, 2, 3, 4, 5", GetMatchExact(index, "original"));
            Assert.AreEqual("0, 3, 4, 5", GetMatchExact(index, "other"));
            Assert.AreEqual("1, 2", GetMatchExact(index, "sample"));

            // Remove all other values for 'sample', verify removed
            index.Index(1, "two sample original sample", "two other original");
            index.Index(2, "three sample original", "three other original");
            Assert.AreEqual("0, 1, 2, 3, 4, 5", GetMatchExact(index, "original"));
            Assert.AreEqual("", GetMatchExact(index, "sample"));
            Assert.AreEqual("0, 1, 2, 3, 4, 5", GetMatchExact(index, "other"));

            // Clear values. Verify index empties
            index.Index(0, "three other original", "");
            index.Index(1, "two other original", "");
            index.Index(2, "three other original", "");
            index.Index(3, "four other original", "");
            index.Index(4, "five other original", "");
            index.Index(5, "six other original", "");
            Assert.AreEqual("", GetIndexData(index));
        }

        [TestMethod]
        public void WordIndex_ValueRequiresSplit()
        {
            WordIndex index = new WordIndex(new DefaultWordSplitter());

            // Verify no matches initially
            Assert.AreEqual("", GetMatches(index, "will be split"));

            // Set a value
            index.Index(1, "", "this value will be split");

            // Verify the item matches (the search term must be split)
            Assert.AreEqual("1", GetMatches(index, "will be split"));

            // Verify the item only matches if all terms are found
            Assert.AreEqual("", GetMatches(index, "will be split also"));
        }

        [TestMethod]
        public void WordIndex_MultipleBlocks()
        {
            WordIndex index = new WordIndex(new DefaultWordSplitter());

            for (int i = 0; i < 70000; ++i)
            {
                string word = "Word" + i.ToString();
                index.AddWord(1, word);
                if (i % 5 == 0) index.AddWord(2, word);
            }

            Assert.AreEqual("1", GetMatchExact(index, "Word6"));
            Assert.AreEqual("1, 2", GetMatchExact(index, "Word5"));
            Assert.AreEqual("1, 2", GetMatchExact(index, "Word69000"));
        }

        [TestMethod]
        public void WordIndex_Serialization()
        {
            WordIndex index = new WordIndex(new DefaultWordSplitter());

            // Set a value
            index.Index(1, "", "this is the original value!");

            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                index.WriteBinary(context);
                context.Stream.Seek(0, SeekOrigin.Begin);

                WordIndex index2 = new WordIndex(new DefaultWordSplitter());
                index2.ReadBinary(context);

                Assert.AreEqual("1", GetMatchExact(index, "value"));
            }
        }

        private static string GetMatchExact(WordIndex index, string word)
        {
            ShortSet results = new ShortSet(ushort.MaxValue);
            index.WhereMatchExact(word, results);
            return String.Join(", ", results.Values);
        }

        private static string GetMatches(WordIndex index, string word)
        {
            ShortSet results = new ShortSet(ushort.MaxValue);
            index.WhereMatches(word, results);
            return String.Join(", ", results.Values);
        }

        private static string GetIndexData(WordIndex index)
        {
            Dictionary<string, List<ushort>> d = index.ConvertToDictionary();
            StringBuilder result = new StringBuilder();

            foreach (string word in d.Keys)
            {
                result.AppendLine(String.Format("{0}: [{1}]", word, String.Join(", ", d[word])));
            }

            return result.ToString();
        }
    }
}

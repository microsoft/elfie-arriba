// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Index;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model.Strings
{
    [TestClass]
    public class AlphanumericSplitterTests
    {
        [TestMethod]
        public void AlphanumericSplitter_IsAlphaNumeric()
        {
            for (byte i = 0; i < 128; ++i)
            {
                char c = (char)i;
                Assert.AreEqual(Char.IsLetterOrDigit(c), AlphanumericSplitter.IsAlphaNumeric(i));
            }

            for (byte i = 128; i < 255; ++i)
            {
                Assert.IsFalse(AlphanumericSplitter.IsAlphaNumeric(i));
            }
        }

        [TestMethod]
        public void AlphanumericSplitter_Basic()
        {
            Assert.AreEqual(String.Empty, SplitAndJoin(String.Empty));
            Assert.AreEqual("Singleton", SplitAndJoin("Singleton"));
            Assert.AreEqual("This|is|a|sample|string", SplitAndJoin("This is a sample string!"));
            Assert.AreEqual("this|is|problematic|see|comment|below", SplitAndJoin("this is problematic. see comment below "));
            Assert.AreEqual("in|the|absence|of|o|persist|everything|to|p|location|elfie|generated|directory", SplitAndJoin("in the absence of -o, persist everything to -p location .elfie generated directory"));
            Assert.AreEqual("0108fc58|6bd3b140", SplitAndJoin("0108fc58 6bd3b140"));
            Assert.AreEqual("VB3094|MSC2093", SplitAndJoin("VB3094: MSC2093"));
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void AlphanumericSplitter_Performance()
        {
            String8 code = AllCodeText.AllCode8;
            String8Set set = default(String8Set);
            PartialArray<int> matchContainer = new PartialArray<int>();

            int iterations = 10;
            int totalWordsSplit = 0;
            int validCount = 0;

            // Split Goal: 75k per millisecond [75 MB/sec]
            Verify.PerformanceByBytes(75 * LongExtensions.Megabyte, () =>
            {
                for (int iteration = 0; iteration < iterations; ++iteration)
                {
                    set = AlphanumericSplitter.Split(code, ref matchContainer);

                    totalWordsSplit += set.Count;
                    if (set.Count > 0)
                    {
                        int matchIndex = AlphanumericSplitter.IsAlphaNumeric(set[0][0]) ? 0 : 1;
                        for (; matchIndex < set.Count; matchIndex += 2)
                        {
                            String8 word = set[matchIndex];
                            if (word.Length > 2) validCount++;
                        }
                    }
                }

                return iterations * code.Length;
            });
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void AlphanumericSplitter_EndToEndPerformance()
        {
            String8 code = AllCodeText.AllCode8;
            String8Set set = default(String8Set);
            PartialArray<int> matchContainer = new PartialArray<int>(2048);

            HashSet<String8> uniqueWords = new HashSet<String8>();
            StringStore strings = new StringStore();
            MemberIndex index = new MemberIndex();

            int iterations = 10;
            int totalWordsSplit = 0;

            // Split, Add, Index Goal: 30k per millisecond [30 MB/sec]
            Verify.PerformanceByBytes(30 * LongExtensions.Megabyte, () =>
            {
                for (int iteration = 0; iteration < iterations; ++iteration)
                {
                    String8Set codeByLine = code.Split(UTF8.Newline, new PartialArray<int>());
                    for (int lineIndex = 0; lineIndex < codeByLine.Count; ++lineIndex)
                    {
                        // Convert and Split the line
                        String8 line = codeByLine[lineIndex];
                        set = AlphanumericSplitter.Split(line, ref matchContainer);

                        totalWordsSplit += set.Count;

                        if (set.Count > 0)
                        {
                            int matchIndex = AlphanumericSplitter.IsAlphaNumeric(set[0][0]) ? 0 : 1;
                            for (; matchIndex < set.Count; matchIndex += 2)
                            {
                                // If the word is long enough...
                                String8 word = set[matchIndex];
                                if (word.Length > 2)
                                {
                                    if (!uniqueWords.Contains(word))
                                    {
                                        int wordIdentifier = strings.FindOrAddString(word);
                                        uniqueWords.Add(strings[wordIdentifier]);
                                        index.AddItem(wordIdentifier, lineIndex);
                                    }
                                }
                            }
                        }
                    }
                }

                return iterations * code.Length;
            });
        }

        public static string SplitAndJoin(string value)
        {
            String8 value8 = String8.Convert(value, new byte[String8.GetLength(value)]);

            PartialArray<int> boundaryArray = new PartialArray<int>();
            String8Set set = AlphanumericSplitter.Split(value8, ref boundaryArray);

            bool firstPart = true;

            StringBuilder result = new StringBuilder();
            using (StringWriter writer = new StringWriter(result))
            {
                for (int i = 0; i < set.Count; ++i)
                {
                    String8 part = set[i];
                    if (!part.IsEmpty() && AlphanumericSplitter.IsAlphaNumeric(part[0]))
                    {
                        if (!firstPart) writer.Write("|");
                        firstPart = false;

                        part.WriteTo(writer);
                    }
                }
            }

            return result.ToString();
        }
    }
}

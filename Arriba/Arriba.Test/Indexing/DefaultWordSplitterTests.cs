// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Arriba.Extensions;
using Arriba.Indexing;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Indexing
{
    [TestClass]
    public class DefaultWordSplitterTests : WordSplitterTestBase
    {
        private const string SampleFilePath = @"..\..\Arriba\Model\Table.cs";

        public DefaultWordSplitterTests() :
            base(new DefaultWordSplitter())
        { }

        [TestMethod]
        public void WordSplitter_Basic()
        {
            Assert.AreEqual("This|is|a|sample|string", SplitAndJoin("This is a sample string!"));
            Assert.AreEqual("this|programmatic|programmatic.|see|comment|below", SplitAndJoin("this programmatic. see comment below "));
            Assert.AreEqual("0108fc58|6bd3b140", SplitAndJoin("0108fc58 6bd3b140"));
            Assert.AreEqual("VB3094|MSC2093", SplitAndJoin("VB3094: MSC2093"));
        }

        public void WordSplitter_Performance()
        {
            WordSplitter_ReadLinePerformance();
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void WordSplitter_ReadLinePerformance()
        {
            int totalLines = 0;
            int totalStrings = 0;

            Stopwatch w = Stopwatch.StartNew();
            Trace.WriteLine("Splitting all bug strings (compare)");

            using (System.IO.StreamReader reader = new System.IO.StreamReader(SampleFilePath))
            {
                // 100ms just reading
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    ByteBlock lineBlock = line;
                    RangeSet results = this.splitter.Split(lineBlock);

                    //HashSet<ByteBlock> words = new HashSet<ByteBlock>();

                    int count = 0;
                    int i;
                    for (i = 0; i < results.Count; ++i)
                    {
                        Range r = results.Ranges[i];
                        if (r.Length >= 2 && r.Length <= 50)
                        {
                            //ByteBlock word = new ByteBlock();
                            //word.Array = lineBlock.Array;
                            //word.Position = r.Index;
                            //word.Length = r.Length;
                            //words.Add(word);

                            count++;
                        }
                    }

                    totalStrings += count;
                    totalLines++;
                }
            }


            w.Stop();
            Trace.WriteLine(String.Format("{0:n0} words found on {1:n0} lines in {2:n0}ms.", totalStrings, totalLines, w.ElapsedMilliseconds));
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void WordSplitter_ReadBlockPerformance()
        {
            int totalLines = 0;
            int totalStrings = 0;

            Stopwatch w = Stopwatch.StartNew();
            Trace.WriteLine("Splitting all bug strings (compare)");

            using (System.IO.StreamReader reader = new System.IO.StreamReader(SampleFilePath))
            {
                byte[] buffer = new byte[8 * 1024];

                while (true)
                {
                    int length = reader.BaseStream.Read(buffer, 0, buffer.Length);
                    if (length == 0) break;

                    ByteBlock block = new ByteBlock(buffer);
                    RangeSet results = this.splitter.Split(block);

                    int count = 0;
                    int i;
                    for (i = 0; i < results.Count; ++i)
                    {
                        Range r = results.Ranges[i];
                        if (r.Length >= 2 && r.Length <= 50)
                        {
                            count++;
                        }
                    }

                    totalStrings += count;
                    totalLines++;
                }
            }


            w.Stop();
            System.Console.WriteLine("{0:n0} words found on {1:n0} lines in {2:n0}ms.", totalStrings, totalLines, w.ElapsedMilliseconds);
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void WordSplitter_ParallelBlockPerformance()
        {
            int totalLines = 0;
            int totalStrings = 0;

            object locker = new object();
            Stopwatch w = Stopwatch.StartNew();
            Trace.WriteLine("Splitting all bug strings (compare)");

            Parallel.ForEach(ReadBlocks(SampleFilePath), (buffer) =>
            {
                // Wrong - need to figure out how to tell.
                int length = 8 * 1024;//reader.BaseStream.Read(buffer, 0, buffer.Length);

                ByteBlock block = new ByteBlock(buffer, 0, length);
                RangeSet results = this.splitter.Split(block);

                int count = 0;
                int i;
                for (i = 0; i < results.Count; ++i)
                {
                    Range r = results.Ranges[i];
                    if (r.Length >= 2 && r.Length <= 50)
                    {
                        count++;
                    }
                }

                lock (locker)
                {
                    totalStrings += count;
                    totalLines++;
                }
            });


            w.Stop();
            System.Console.WriteLine("{0:n0} words found on {1:n0} lines in {2:n0}ms.", totalStrings, totalLines, w.ElapsedMilliseconds);
        }

        private static IEnumerable<byte[]> ReadBlocks(string path)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                byte[] buffer = new byte[8 * 1024];

                while (true)
                {
                    lock (reader)
                    {
                        int length = reader.BaseStream.Read(buffer, 0, buffer.Length);
                        if (length == 0) break;
                    }

                    yield return buffer;
                }
            }
        }
    }
}

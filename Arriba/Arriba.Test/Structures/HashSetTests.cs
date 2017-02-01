// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test
{
    /// <summary>
    ///  This is a rough copy of ShortSet tests to provide a runtime comparison to .NET's HashSet.
    /// </summary>
    public class HashSetTests
    {
#if PERFORMANCE
        [TestMethod]
#endif
        public void ShortSet_GetAndSetPerformance()
        {
            // Goal: set operations are <100 instructions, so at 2M instructions per millisecond, >20k per millisecond (Release build)
            //  Get and Set are used when evaluating ORDER BY for small sets and for determining aggregates each item should be included within.
            Random r = new Random();
            HashSet<ushort> s1 = BuildRandom(ushort.MaxValue, 10000, r);
            HashSet<ushort> s2 = BuildRandom(ushort.MaxValue, 1000, r);
            ushort[] getAndSetValues = s2.ToArray();

            // 2k values; 4k operations; 4M total operations
            int iterations = 1000;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                int length = getAndSetValues.Length;
                for (int j = 0; j < length; ++j)
                {
                    ushort value = getAndSetValues[j];
                    bool initial = s1.Contains(value);

                    if (!initial)
                    {
                        s1.Add(value);
                    }
                    else
                    {
                        s1.Remove(value);
                    }
                }
            }

            int operations = (2 * getAndSetValues.Length * iterations);
            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = operations / milliseconds;
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.", operations, milliseconds, operationsPerMillisecond));

            //Assert.IsTrue(operationsPerMillisecond > 10000, "Not within 200% of goal.");
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void ShortSet_SetPerformance()
        {
            // Goal: Set operations are <10k instructions, so at 2M instructions per millisecond, 200 per millisecond (Release build)
            //  Set operations are used to combine where clauses and sets for specific words when word searching.
            Random r = new Random();
            HashSet<ushort> s1 = BuildRandom(ushort.MaxValue, 1000, r);
            HashSet<ushort> s2 = BuildRandom(ushort.MaxValue, 10000, r);
            HashSet<ushort> s3 = BuildRandom(ushort.MaxValue, 50000, r);
            ushort[] s4 = { 1, 126, 950, 1024, 1025, 1670, 19240 };

            HashSet<ushort> scratch = new HashSet<ushort>();

            // 8 Operations x 10k iterations = 20k operations.
            // Goal is 100ms.
            int iterations = 2500;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                scratch.Clear();
                scratch.UnionWith(s1);
                scratch.UnionWith(s4);
                scratch.ExceptWith(s4);
                scratch.UnionWith(s2);
                scratch.IntersectWith(s3);
                scratch.ExceptWith(s2);
                //scratch.Not();
            }

            int operations = (8 * iterations);
            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = operations / milliseconds;
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.", operations, milliseconds, operationsPerMillisecond));

            //Assert.IsTrue(operationsPerMillisecond > 100, "Not within 200% of goal.");
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void ShortSet_EnumeratePerformance()
        {
            // Goal: Enumerate is <200k instructions, so at 2M instructions per millisecond, 10 per millisecond (Release build)
            //  Enumerate is used to walk set items when computing results in ORDER BY order. 
            HashSet<ushort> s1 = BuildRandom(ushort.MaxValue, 1000, new Random());
            HashSet<ushort> s2 = BuildRandom(ushort.MaxValue, 10000, new Random());

            int iterations = 500;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                ICollection<ushort> values = s1;
                Assert.AreEqual(1000, values.Count);

                ICollection<ushort> values2 = s2;
                Assert.AreEqual(10000, values2.Count);
            }

            int operations = (2 * iterations);
            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = operations / milliseconds;
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.", operations, milliseconds, operationsPerMillisecond));

            //Assert.IsTrue(operationsPerMillisecond > 5, "Not within 200% of goal.");
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void ShortSet_CountPerformance()
        {
            // Goal: Count is <10k instructions, so at 2M instructions per millisecond, 200 per millisecond (Release build)
            //  Count is used for COUNT(*) aggregate and to compute IntelliSense rank for words in the word index.
            HashSet<ushort> s2 = BuildRandom(ushort.MaxValue, 10000, new Random());

            int iterations = 10000;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                Assert.AreEqual(10000, s2.Count);
            }

            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = iterations / milliseconds;
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.", iterations, milliseconds, operationsPerMillisecond));

            //Assert.IsTrue(operationsPerMillisecond > 100, "Not within 200% of goal.");
        }

        private HashSet<ushort> BuildRandom(ushort capacity, ushort itemsToSet, Random r)
        {
            HashSet<ushort> s = new HashSet<ushort>();
            for (int i = 0; i <= capacity; ++i)
            {
                double chanceToIncludeThis = (double)itemsToSet / (capacity - i);
                if (r.NextDouble() < chanceToIncludeThis)
                {
                    s.Add((ushort)i);
                    itemsToSet--;
                }

                if (itemsToSet == 0) break;
            }

            return s;
        }
    }
}

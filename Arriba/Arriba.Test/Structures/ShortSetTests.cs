// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test
{
    [TestClass]
    public class ShortSetTests
    {
        [TestMethod]
        public void ShortSet_Basic()
        {
            // Constructor
            ShortSet s1 = new ShortSet(100);

            // Empty
            Assert.AreEqual("", String.Join(", ", s1.Values));

            // Set value and enumerate
            s1.Add(0);
            Assert.AreEqual("0", String.Join(", ", s1.Values));

            // Set additional values
            s1.Add(15);
            s1.Add(64);
            Assert.AreEqual("0, 15, 64", String.Join(", ", s1.Values));

            // Clear values
            s1.Remove(64);
            Assert.AreEqual("0, 15", String.Join(", ", s1.Values));

            // Or
            ShortSet s2 = new ShortSet(120);
            s2.Or(new ushort[] { 0, 1, 2 });
            s1.Or(s2);
            Assert.AreEqual("0, 1, 2, 15", String.Join(", ", s1.Values));
            Assert.AreEqual("0, 1, 2", String.Join(", ", s2.Values));
            Verify.Exception<ArgumentNullException>(() => s1.Or((ShortSet)null));
            Verify.Exception<ArgumentNullException>(() => s1.Or((IEnumerable<ushort>)null));

            // OrNot [only 15, 16 not set, so only they should be added]
            ShortSet s3 = new ShortSet(100);
            s3.Not();
            s3.Remove(15);
            s3.Remove(16);
            s1.OrNot(s3);
            Assert.AreEqual("0, 1, 2, 15, 16", String.Join(", ", s1.Values));
            Verify.Exception<ArgumentNullException>(() => s1.OrNot((ShortSet)null));

            // And
            s1.And(s2);
            s1.And(new ushort[] { 1, 2 });
            Assert.AreEqual("1, 2", String.Join(", ", s1.Values));
            s1.And(new ushort[] { 1 });
            Assert.AreEqual("1", String.Join(", ", s1.Values));
            Verify.Exception<ArgumentNullException>(() => s1.And((ShortSet)null));
            Verify.Exception<ArgumentNullException>(() => s1.And((IEnumerable<ushort>)null));

            // AndNot
            s1.Add(96);
            s1.Add(64);
            s1.AndNot(s2);
            s1.AndNot(new ushort[] { 96 });
            Assert.AreEqual("64", String.Join(", ", s1.Values));
            Verify.Exception<ArgumentNullException>(() => s1.AndNot((ShortSet)null));
            Verify.Exception<ArgumentNullException>(() => s1.AndNot((IEnumerable<ushort>)null));

            // Clear
            s1.Clear();
            Assert.AreEqual("", String.Join(", ", s1.Values));

            // From
            s1.From(s2);
            Assert.AreEqual("0, 1, 2", String.Join(", ", s1.Values));
            Verify.Exception<ArgumentNullException>(() => s1.From((ShortSet)null));

            // FromAnd
            ShortSet s4 = new ShortSet(100);
            s4.Or(new ushort[] { 1, 2, 3 });
            s1.Clear();
            s1.Not();
            s1.FromAnd(s2, s4);
            Assert.AreEqual("1, 2", String.Join(", ", s1.Values));
            Verify.Exception<ArgumentNullException>(() => s1.FromAnd((ShortSet)null, s2));
            Verify.Exception<ArgumentNullException>(() => s1.FromAnd(s2, (ShortSet)null));

            // ToString
            Assert.AreEqual("[1, 2]", s1.ToString());
        }

        [TestMethod]
        unsafe public void ShortSet_Unsafe()
        {
            ShortSet s1 = new ShortSet(64);

            // Set the first three values from a "sparse packed" set
            ushort[] values = new ushort[] { 1, 3, 5, 7, 9, 11 };
            fixed (ushort* vp = values)
            {
                s1.Or(vp, 3);
                Verify.Exception<ArgumentNullException>(() => s1.Or((ushort*)null, 1));
            }

            Assert.AreEqual("1, 3, 5", String.Join(", ", s1.Values));

            // Set the first 64 bits of values from a "dense packed" set [this is 0x3, so the last two bits are set; endianness specific]
            ulong[] bits = new ulong[] { 3 };
            fixed (ulong* bp = bits)
            {
                s1.Or(bp, 1);
                Verify.Exception<ArgumentNullException>(() => s1.Or((ulong*)null, 1));
            }

            Assert.AreEqual("1, 3, 5, 62, 63", String.Join(", ", s1.Values));
        }

        [TestMethod]
        public void ShortSet_LeadingZeros()
        {
            Assert.AreEqual(0, ShortSet.LeadingZeros(~(0UL)));
            Assert.AreEqual(1, ShortSet.LeadingZeros(~(0UL) >> 1));
            Assert.AreEqual(10, ShortSet.LeadingZeros(0x0038888888888888UL));
            Assert.AreEqual(62, ShortSet.LeadingZeros(0x3UL));
            Assert.AreEqual(64, ShortSet.LeadingZeros(0UL));
        }

        [TestMethod]
        public void ShortSet_CapacityZero()
        {
            ShortSet s1 = new ShortSet(0);
            Assert.AreEqual(0, s1.Count());

            s1.Not();
            Assert.AreEqual(0, s1.Count());

            ShortSet s2 = new ShortSet(10);
            s2.Not();
            s1.And(s2);
            s1.Or(s2);
            Assert.AreEqual(0, s1.Count());
        }

        [TestMethod]
        public void ShortSet_CapacityHandling()
        {
            // Verify values above capacity are not reported, even if there are bits for them
            ShortSet s1 = new ShortSet(10);
            s1.Not();
            Assert.AreEqual("0, 1, 2, 3, 4, 5, 6, 7, 8, 9", String.Join(", ", s1.Values));

            // Verify the last value is not truncated if the this set in an operation is larger,
            // and that values above the other capacity aren't involved in operations.
            ShortSet s2 = new ShortSet(120);
            s2.Not();

            ShortSet s3 = new ShortSet(64);
            s3.Not();

            s2.And(s3);
            Assert.IsTrue(s2.Contains(63));

            s2.Or(s3);
            Assert.IsTrue(s2.Contains(63));

            s3.Not();
            s2.AndNot(s3);
            Assert.IsTrue(s2.Contains(63));
        }

        [TestMethod]
        public void ShortSet_MismatchedCapacities()
        {
            ShortSet s1 = new ShortSet(10);
            s1.Add(1);
            s1.Add(3);

            ShortSet s2 = new ShortSet(20);
            s2.Add(2);
            s2.Add(4);
            s2.Add(10);

            // Verify values below common capacity are set, larger values not set.
            s1.Or(s2);
            Assert.AreEqual("1, 2, 3, 4", String.Join(", ", s1.Values));

            // Verify values above common capacity are left alone.
            s2.Or(s1);
            Assert.AreEqual("1, 2, 3, 4, 10", String.Join(", ", s2.Values));

            // Verify values below common capacity are cleared, values above not unexpectedly set.
            s1.AndNot(s2);
            Assert.AreEqual("", String.Join(", ", s1.Values));

            // Verify values above common capacity are left set
            s1.Clear();
            s1.Add(1);
            s1.Add(3);
            s2.AndNot(s1);
            Assert.AreEqual("2, 4, 10", String.Join(", ", s2.Values));

            // Verify values above common capacity are not set
            s2.Or(s1);
            s1.And(s2);
            Assert.AreEqual("1, 3", String.Join(", ", s1.Values));

            // Verify values above common capacity *are* cleared)
            s2.And(s1);
            Assert.AreEqual("1, 3", String.Join(", ", s2.Values));
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void ShortSet_Performance_GetAndSet()
        {
            // Goal: set operations are <10 instructions, so at 2M instructions per millisecond, >200k per millisecond (Release build)
            //  Get and Set are used when evaluating ORDER BY for small sets and for determining aggregates each item should be included within.
            Random r = new Random();
            ShortSet s1 = BuildRandom(ushort.MaxValue, 10000, r);
            ShortSet s2 = BuildRandom(ushort.MaxValue, 1000, r);
            ushort[] getAndSetValues = s2.Values.ToArray();

            // 1k values; 2k operations; 20M total
            int iterations = 10000;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                int length = getAndSetValues.Length;
                for (int j = 0; j < length; ++j)
                {
                    ushort value = getAndSetValues[j];
                    //bool initial = s1[value];
                    //s1[value] = !initial;

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

            Assert.IsTrue(operationsPerMillisecond > 75000, "Not within 200% of goal.");
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void ShortSet_Performance_Set()
        {
            // Goal: Set operations are <10k instructions, so at 2M instructions per millisecond, 200 per millisecond (Release build)
            //  Set operations are used to combine where clauses and sets for specific words when word searching.
            Random r = new Random();
            ShortSet s1 = BuildRandom(ushort.MaxValue, 1000, r);
            ShortSet s2 = BuildRandom(ushort.MaxValue, 10000, r);
            ShortSet s3 = BuildRandom(ushort.MaxValue, 50000, r);
            ushort[] s4 = { 1, 126, 950, 1024, 1025, 1670, 19240 };

            ShortSet scratch = new ShortSet(ushort.MaxValue);

            // 9 Operations x 10k iterations = 90k operations.
            // Goal is 100ms.
            int iterations = 2500;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                // Singleton Operations / Reset
                scratch.Not();
                scratch.Clear();
                scratch.Or(s1);

                // Enumerable Operations
                scratch.And(s4);
                scratch.Or(s4);
                scratch.AndNot(s4);

                // ShortSet Operations
                scratch.Or(s2);
                scratch.And(s3);
                scratch.AndNot(s2);
            }

            int operations = (9 * iterations);
            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = operations / milliseconds;
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.", operations, milliseconds, operationsPerMillisecond));

            Assert.IsTrue(operationsPerMillisecond > 100, "Not within 200% of goal.");
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void ShortSet_Performance_Enumerate()
        {
            // Goal: Enumerate is <200k instructions, so at 2M instructions per millisecond, 10 per millisecond (Release build)
            //  Enumerate is used to walk set items when computing results in ORDER BY order. 
            ShortSet s1 = BuildRandom(ushort.MaxValue, 1000, new Random());
            ShortSet s2 = BuildRandom(ushort.MaxValue, 10000, new Random());

            int iterations = 500;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                ICollection<ushort> values1 = s1.Values;
                Assert.AreEqual(1000, values1.Count);

                ICollection<ushort> values2 = s2.Values;
                Assert.AreEqual(10000, values2.Count);
            }

            int operations = (2 * iterations);
            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = operations / milliseconds;
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.", operations, milliseconds, operationsPerMillisecond));

            Assert.IsTrue(operationsPerMillisecond > 5, "Not within 200% of goal.");
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void ShortSet_Performance_Count()
        {
            // Goal: Count is <10k instructions. 2GHz is 2B/sec, so 2M/ms, so 10k each means 200 iterations per ms (Release build)
            //  Count is used for COUNT(*) aggregate and to compute IntelliSense rank for words in the word index.
            ShortSet s0 = new ShortSet(ushort.MaxValue);

            ShortSet s1 = new ShortSet(ushort.MaxValue);
            s1.Not();

            ShortSet s2 = BuildRandom(ushort.MaxValue, 10000, new Random());

            int iterations = 10000;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                Assert.AreEqual(0, s0.Count());
                Assert.AreEqual(ushort.MaxValue, s1.Count());
                Assert.AreEqual(10000, s2.Count());
            }

            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = (3 * iterations) / milliseconds;
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.", iterations, milliseconds, operationsPerMillisecond));

            Assert.IsTrue(operationsPerMillisecond > 50, "Not within 200% of goal.");
        }

        public static ShortSet BuildRandom(ushort capacity, ushort itemsToSet, Random r)
        {
            ShortSet s = new ShortSet(capacity);
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

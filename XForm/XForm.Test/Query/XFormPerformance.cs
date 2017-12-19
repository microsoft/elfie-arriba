// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;

namespace XForm.Test.Query
{
    [TestClass]
    public class XFormPerformance
    {
        // ISSUE: Faster in Release only, and not enough iterations to be consistent.
        //[TestMethod]
        public void XFormVsLinqPerformance()
        {
            int[] array = new int[16 * 1024 * 1024];
            //int[] array2 = new int[16 * 1024 * 1024];
            Random r = new Random();
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = r.Next(1000);
                //array2[i] = 50;
            }

            int expectedCount = 0;
            using (new TraceWatch($"For Loop [==]"))
            {
                int count = 0;
                for (int i = 0; i < array.Length; ++i)
                {
                    if (array[i] < 50) count++;
                }

                expectedCount = count;
            }

            TimeSpan timeLimit;
            Stopwatch w = Stopwatch.StartNew();
            using (new TraceWatch($"Linq Count [==]"))
            {
                int count = array.Where((i) => i < 50).Count();
                Assert.AreEqual(expectedCount, count);
            }
            w.Stop();
            timeLimit = w.Elapsed;

            IDataBatchEnumerator query = XFormTable.FromArrays(array.Length)
                .WithColumn("Value", array)
                .Query("where [Value] < 50", new WorkflowContext());

            // Run once to force pre-allocation of buffers
            query.RunWithoutDispose();
            query.Reset();

            w = Stopwatch.StartNew();
            using (new TraceWatch($"XForm Count"))
            {
                long count = query.Count();

                w.Stop();
                query.Dispose();
                Assert.AreEqual(expectedCount, count);
                Assert.IsTrue(w.Elapsed < timeLimit, "XForm should be faster than Linq with int.Equals");
            }
        }
    }
}

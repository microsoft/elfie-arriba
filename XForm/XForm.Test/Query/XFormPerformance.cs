// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class XFormPerformance
    {
        private int[] _array;
        private int _value;

        [TestInitialize]
        public void Initialize()
        {
            _array = new int[16 * 1024 * 1024];
            Random r = new Random();
            for (int i = 0; i < _array.Length; ++i)
            {
                _array[i] = r.Next(1000);
            }

            _value = 500;
        }

        // ISSUE: Faster in Release only, and not enough iterations to be consistent.
        //[TestMethod]
        public void XFormVsLinqPerformance()
        {
            int expectedCount = 0;
            TimeSpan timeLimit;

            using (new TraceWatch($"For Loop [==]"))
            {
                int count = 0;
                for (int i = 0; i < _array.Length; ++i)
                {
                    if (_array[i] == _value) count++;
                }

                expectedCount = count;
            }

            Stopwatch w = Stopwatch.StartNew();
            using (new TraceWatch($"Linq Count [==]"))
            {
                int count = _array.Where((i) => i == _value).Count();
                Assert.AreEqual(expectedCount, count);
            }
            w.Stop();
            timeLimit = w.Elapsed;

            ArrayEnumerator arrayTable = new ArrayEnumerator(_array.Length);
            arrayTable.AddColumn("ID", _array);

            IDataBatchEnumerator query = XqlParser.Parse($@"
                where ID = {_value}
                count", arrayTable, new WorkflowContext());

            // Run once to force pre-allocation of buffers
            query.Run();
            query.Reset();

            w = Stopwatch.StartNew();
            using (new TraceWatch($"XForm Count"))
            {
                int count = query.RunAndGetSingleValue<int>();

                w.Stop();
                query.Dispose();
                Assert.AreEqual(expectedCount, count);
                Assert.IsTrue(w.Elapsed < timeLimit, "XForm should be faster than Linq with int.Equals");
            }
        }
    }
}

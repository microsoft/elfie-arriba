using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class XFormPerformance
    {
        private int[] array;
        private int value;

        [TestInitialize]
        public void Initialize()
        {
            array = new int[16 * 1024 * 1024];
            Random r = new Random();
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = r.Next(1000);
            }

            value = 500;
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
                for (int i = 0; i < array.Length; ++i)
                {
                    if (array[i] == value) count++;
                }

                expectedCount = count;
            }

            Stopwatch w = Stopwatch.StartNew();
            using (new TraceWatch($"Linq Count [==]"))
            {
                int count = array.Where((i) => i == value).Count();
                Assert.AreEqual(expectedCount, count);
            }
            w.Stop();
            timeLimit = w.Elapsed;

            ArrayEnumerator arrayTable = new ArrayEnumerator(array.Length);
            arrayTable.AddColumn("ID", array);

            IDataBatchEnumerator query = PipelineParser.BuildPipeline($@"
                where ID = {value}
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

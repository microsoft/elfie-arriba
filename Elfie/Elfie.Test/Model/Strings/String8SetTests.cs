// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model.Strings
{
    [TestClass]
    public class String8SetTests
    {
        [TestMethod]
        public void String8Set_Split_Basic()
        {
            Assert.AreEqual(string.Empty, SplitAndJoin(string.Empty, '.'));
            Assert.AreEqual("System|Collections|Generic|List<T>", SplitAndJoin("System.Collections.Generic.List<T>", '.'));
            Assert.AreEqual("A|B|C|D", SplitAndJoin("A.B.C.D", '.'));
            Assert.AreEqual("A|B|C|D", SplitAndJoin(".A.B.C..D.", '.'));
            Assert.AreEqual("No Delimiters", SplitAndJoin("No Delimiters", '.'));
        }

#if !DEBUG
        [TestMethod]
#endif
        public void String8Set_Split_Performance()
        {
            byte[] stringBuffer = new byte[128];
            int[] partBuffer = new int[10];
            StringBuilder result = new StringBuilder(128);

            using (StringWriter writer = new StringWriter(result))
            {
                Stopwatch w = Stopwatch.StartNew();
                int iterations = 200000;
                for (int iteration = 0; iteration < iterations; ++iteration)
                {
                    SplitAndJoin("System.Collections.Generic.List<T>", stringBuffer, '.', partBuffer, writer);
                    result.Clear();

                    SplitAndJoin("No Delimiters", stringBuffer, '.', partBuffer, writer);
                    result.Clear();
                }
                w.Stop();
                Trace.WriteLine(string.Format("{0:n0} splits took {1}", 2 * iterations, w.Elapsed.ToFriendlyString()));
                Assert.IsTrue(w.ElapsedMilliseconds < 250);
            }
        }

        private string SplitAndJoin(string value, char delimiter)
        {
            byte[] stringBuffer = new byte[String8.GetLength(value)];
            String8 value8 = String8.Convert(value, stringBuffer);

            int[] partBuffer = new int[String8Set.GetLength(value8, '.')];

            StringBuilder result = new StringBuilder();
            using (StringWriter writer = new StringWriter(result))
            {
                SplitAndJoin(value, stringBuffer, delimiter, partBuffer, writer);
            }

            return result.ToString();
        }

        private void SplitAndJoin(string value, byte[] stringBuffer, char delimiter, int[] partBuffer, StringWriter writer)
        {
            String8 value8 = String8.Convert(value, stringBuffer);
            String8Set set = String8Set.Split(value8, '.', partBuffer);

            bool firstPart = true;

            Assert.IsTrue(set.Count >= 0);
            for (int i = 0; i < set.Count; ++i)
            {
                String8 part = set[i];
                if (!part.IsEmpty())
                {
                    if (!firstPart) writer.Write("|");
                    firstPart = false;

                    part.WriteTo(writer);
                }
            }
        }
    }
}

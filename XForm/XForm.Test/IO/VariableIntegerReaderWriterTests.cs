// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.IO;
using XForm.Types;

namespace XForm.Test.IO
{
    [TestClass]
    public class VariableIntegerReaderWriterTests
    {
        [TestMethod]
        public void VariableIntegerReaderWriter_Basic()
        {
            // In byte[] range
            RoundTrip("InByteRange", Enumerable.Range(0, 15000).Select((i) => i % 256).ToArray());

            // Upconvert to ushort[]
            RoundTrip("InUshortRange", Enumerable.Range(0, 15000).ToArray());

            // Upconvert to int[]
            RoundTrip("IntRange", Enumerable.Range(64000, 15000).ToArray());
        }

        private static void RoundTrip(string columnName, int[] array, int batchSize = 128)
        {
            XDatabaseContext context = new XDatabaseContext();

            string columnPath = Path.Combine("VariableIntegerReaderWriterTests", columnName);
            string columnPrefix = Path.Combine(columnPath, "Vl");

            context.StreamProvider.Delete(columnPath);
            Directory.CreateDirectory(columnPath);

            XArray values = XArray.All(array, array.Length);

            using (IColumnWriter writer = new VariableIntegerWriter(context.StreamProvider, columnPrefix))
            {
                ArraySelector page = ArraySelector.All(0).NextPage(array.Length, batchSize);
                while (page.Count > 0)
                {
                    writer.Append(values.Reselect(page));
                    page = page.NextPage(array.Length, batchSize);
                }
            }

            XArray returned = default(XArray);

            using (IColumnReader reader = new VariableIntegerReader(context.StreamProvider, columnPrefix, CachingOption.AsConfigured))
            {
                returned = reader.Read(ArraySelector.All(array.Length));
            }

            TableTestHarness.AssertAreEqual(values, returned, array.Length);

            context.StreamProvider.Delete(columnPath);
        }
    }
}

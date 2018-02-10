// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;

namespace XForm.Test.IO
{
    [TestClass]
    public class EnumReaderWriterTests
    {
        [TestMethod]
        public void EnumReaderWriter_Basic()
        {
            // All a single value
            RoundTrip("EnumReaderWriter_Constant", Enumerable.Repeat(10, 15000).ToArray());

            // Under 256 values
            RoundTrip("EnumReaderWriter_Enum", Enumerable.Range(0, 15000).Select((i) => i % 256).ToArray());

            // Over 256 values (all unique)
            RoundTrip("EnumReaderWriter_Unique", Enumerable.Range(0, 15000).ToArray());

            // Over 256 values, but only after first 10,240 (DefaultPageCount; requires 'Convert()'
            RoundTrip("EnumReaderWriter_Converted", Enumerable.Range(0, 15000).Select((i) => (i < 11000 ? i % 256 : i)).ToArray());
        }

        private static void RoundTrip<T>(string tableName, T[] array)
        {
            XDatabaseContext context = new XDatabaseContext();

            context.StreamProvider.Delete($"Table\\{tableName}");

            IXTable expected = context
                .FromArrays(array.Length)
                .WithColumn("Column", array);

            expected.Save(tableName, context);

            using (IXTable actual = context.Load(tableName))
            {
                TableTestHarness.AssertAreEqual(expected, actual, 1024);
            }
        }
    }
}

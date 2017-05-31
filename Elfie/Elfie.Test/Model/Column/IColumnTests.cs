// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model.Column
{
    [TestClass]
    public class IColumnTests
    {
        [TestMethod]
        public void DateTimeColumn_Basic()
        {
            DateTime[] samples = new DateTime[] { DateTime.UtcNow, DateTime.UtcNow.AddDays(-10), DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime() };

            DateTimeColumn column = new DateTimeColumn();
            Assert.AreEqual(0, column.Count);

            Verify.Exception<ArgumentOutOfRangeException>(() => { DateTime value = column[0]; });

            // Set values and verify they are read back consistently (all UTC)
            for (int i = 0; i < samples.Length; ++i)
            {
                column.Add();
                column[i] = samples[i];
                Assert.AreEqual(samples[i], column[i]);
            }

            Assert.AreEqual(samples.Length, column.Count);

            // Add a local time value and verify it's UTC when it comes back
            DateTime localTime = new DateTime(2017, 3, 7, 10, 26, 56, DateTimeKind.Local);

            column.Add();
            column[column.Count - 1] = localTime;
            Assert.AreEqual(localTime.ToUniversalTime(), column[column.Count - 1]);

            // Round-Trip the column
            DateTimeColumn readColumn = new DateTimeColumn();
            Verify.RoundTrip<DateTimeColumn>(column, readColumn);
            Assert.AreEqual(column.Count, readColumn.Count);

            for (int i = 0; i < column.Count; ++i)
            {
                Assert.AreEqual(column[i], readColumn[i]);
            }

            // Verify clear works
            column.Clear();
            Assert.AreEqual(0, column.Count);
            Verify.Exception<ArgumentOutOfRangeException>(() => { DateTime value = column[0]; });

            // Verify SetCount works
            column.SetCount(2);
            Assert.AreEqual(2, column.Count);
            Assert.AreEqual(DateTime.MinValue, column[1]);
        }

        [TestMethod]
        public void String8Column_Basic()
        {
            StringStore store = new StringStore();
            String8Block block = new String8Block();
            String8[] samples = new String8[] { String8.Empty, block.GetCopy("One"), block.GetCopy("two"), block.GetCopy("three") };

            String8Column column = new String8Column(store);
            Assert.AreEqual(0, column.Count);

            Verify.Exception<ArgumentOutOfRangeException>(() => { String8 value = column[0]; });

            // Set values and verify they are read back consistently (all UTC)
            for (int i = 0; i < samples.Length; ++i)
            {
                column.Add();
                column[i] = samples[i];
                Assert.AreEqual(samples[i], column[i]);
            }

            Assert.AreEqual(samples.Length, column.Count);

            // Round-Trip the column
            column.ConvertToImmutable();

            StringStore readStore = new StringStore();
            Verify.RoundTrip(store, readStore);

            String8Column readColumn = new String8Column(readStore);
            Verify.RoundTrip<String8Column>(column, readColumn);

            Assert.AreEqual(column.Count, readColumn.Count);

            for (int i = 0; i < column.Count; ++i)
            {
                Assert.AreEqual(column[i], readColumn[i]);
            }

            // Verify asking for raw identifiers works
            int firstStringIdentifier = column.IdentifierFor(1);
            Assert.AreEqual(store[firstStringIdentifier], column[1]);

            // Verify clear works
            column.Clear();
            Assert.AreEqual(0, column.Count);
            Verify.Exception<ArgumentOutOfRangeException>(() => { String8 value = column[0]; });

            // Verify SetCount works
            column.SetCount(2);
            Assert.AreEqual(2, column.Count);
            Assert.AreEqual(String8.Empty, column[1]);
        }
    }
}

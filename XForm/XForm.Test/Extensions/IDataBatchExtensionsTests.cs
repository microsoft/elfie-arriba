// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;
using XForm.IO.StreamProvider;

namespace XForm.Test.Extensions
{
    [TestClass]
    public class IDataBatchExtensionsTests
    {
        [TestMethod]
        public void Enumerable_ToList()
        {
            int arraySize = 1024;
            int pageSize = 100;

            // Build an array of [0, n)
            int[] array = Enumerable.Range(0, arraySize).ToArray();

            int nextExpectedValue = 0;
            int totalCountSeen = 0;

            // Page over it in batches
            foreach (List<int> page in XFormTable.FromArrays(arraySize).WithColumn("Value", array).ToList<int>("Value", pageSize))
            {
                // Verify the desired count is returned each page (last one smaller)
                int countExpected = Math.Min(pageSize, arraySize - totalCountSeen);
                Assert.AreEqual(countExpected, page.Count);
                totalCountSeen += page.Count;

                // Verify the values are correct
                for (int i = 0; i < page.Count; ++i)
                {
                    Assert.AreEqual(nextExpectedValue, page[i]);
                    nextExpectedValue++;
                }
            }

            // Verify everything was paged over
            Assert.AreEqual(arraySize, totalCountSeen);
            Assert.AreEqual(arraySize, nextExpectedValue);
        }

        [TestMethod]
        public void Enumerable_SaveAndLoad()
        {
            // Make an array of integers
            int[] array = Enumerable.Range(0, 1024).ToArray();

            // Save it and verify it's saved
            WorkflowContext context = new WorkflowContext();
            Assert.AreEqual(array.Length, XFormTable.FromArrays(array.Length).WithColumn("ID", array).Save("Enumerable_SaveAndLoad_Sample", context));

            // Load it and verify it's re-loaded and matches
            List<int> reloaded = XFormTable.Load("Enumerable_SaveAndLoad_Sample", context).ToList<int>("ID", 10240).First();
            Assert.AreEqual(array.Length, reloaded.Count);
            for (int i = 0; i < array.Length; ++i)
            {
                Assert.AreEqual(array[i], reloaded[i]);
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class SampleQuery
    {
        [TestMethod]
        public void Sample()
        {
            SampleDatabase.EnsureBuilt();

            string xqlQuery = @"
                read WebRequest
                select [ServerPort], Cast([ResponseBytes], Int32)
                where [ServerPort] = ""80""
                limit 1000
                ";

            int desiredRowCountPerPage = 100;
            int maxResponseBytes = -1;

            // Build a Pipeline for the query. Wrap in a using statement to Dispose it when done.
            using (IXTable pipeline = SampleDatabase.XDatabaseContext.Query(xqlQuery))
            {
                // Identify the columns you're consuming by requesting and caching the getter functions for them.
                //  You must request the getters before the first call to Next().
                //  This tells the caller which columns you care about and builds hardcoded logic to get the data you want.
                Func<XArray> columnGetter = pipeline.Columns.Find("ResponseBytes").CurrentGetter();

                // Call Next() to get an XArray of rows. Ask for only as many as you need. Ask for an XArray size convenient to work with.
                // Next() may return fewer rows than you asked for, but will not return zero until the input has run out of rows.
                while (pipeline.Next(desiredRowCountPerPage) > 0)
                {
                    // Get the values for your desired column for this set of rows.
                    XArray responseBytesxarray = columnGetter();

                    // If you know the type of the column, you can safely cast the array to the right type.
                    // This allows you to write C# code hard-coded to the type, so there's no boxing and no interface calls.
                    int[] array = (int[])responseBytesxarray.Array;

                    // Loop from zero to the count the xarray says it returned
                    for (int i = 0; i < responseBytesxarray.Count; ++i)
                    {
                        // You need to look up the real index of each value in the array.
                        //  Index() allows callers to pass a whole array, and array slice, or lookup indices into the array.
                        //  The CPU figures out the pattern quickly so branch costs are minimal.
                        int responseBytes = array[responseBytesxarray.Index(i)];

                        // Run your own logic (in this case, track the Max of the value)
                        if (responseBytes > maxResponseBytes) maxResponseBytes = responseBytes;
                    }
                }
            }

            Assert.AreEqual(1335, maxResponseBytes);
        }
    }
}

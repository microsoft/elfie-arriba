// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Structures
{
    [TestClass]
    public class UniqueValueMergerTests
    {
        [TestMethod]
        public void UniqueValueMerger_Basic()
        {
            IUniqueValueMerger merger = new UniqueValueMerger<int>();
            merger.Add(new int[] { 0, 1, 2, 3 });
            merger.Add(new int[] { -1, 1, 3, 5 });
            merger.Add(new int[] { 0, 1, 2, 3 });
            merger.Add(new int[] { 8, 10 });
            merger.Add(new int[0]);
            merger.Add((IEnumerable<int>)null);

            // sort results because UniqueValueMerger does not garentee order
            int[] results = (int[])merger.GetUniqueValues(0);
            Array.Sort(results);

            Assert.AreEqual("-1, 0, 1, 2, 3, 5, 8, 10", String.Join(", ", results));
        }
    }
}

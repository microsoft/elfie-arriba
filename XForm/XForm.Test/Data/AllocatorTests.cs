// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;

namespace XForm.Test.Data
{
    [TestClass]
    public class AllocatorTests
    {
        [TestMethod]
        public void Allocator_Basics()
        {
            int[] buffer = null;
            int[] previous;

            // Verify allocation happens on first call
            Allocator.AllocateToSize(ref buffer, 10);
            Assert.IsNotNull(buffer);
            Assert.AreEqual(10, buffer.Length);

            // Verify no re-allocation if size already fine
            previous = buffer;
            Allocator.AllocateToSize(ref buffer, 5);
            Assert.AreEqual(10, buffer.Length);
            Assert.ReferenceEquals(buffer, previous);

            previous = buffer;
            Allocator.AllocateToSize(ref buffer, 10);
            Assert.AreEqual(10, buffer.Length);
            Assert.ReferenceEquals(buffer, previous);

            // Verify generic allocator works
            Array generic = Allocator.AllocateArray(typeof(int), 10);
            Assert.IsNotNull(generic as int[]);
            Assert.AreEqual(10, generic.Length);

            Array string8 = Allocator.AllocateArray(typeof(String8), 1);
            Assert.IsNotNull(string8 as String8[]);
            Assert.AreEqual(1, string8.Length);
        }
    }
}

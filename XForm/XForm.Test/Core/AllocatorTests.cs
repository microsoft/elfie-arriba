// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XForm.Test.Core
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
            Array generic = null;
            Allocator.AllocateToSize(ref generic, 10, typeof(int));
            Assert.IsNotNull(generic as int[]);
            Assert.AreEqual(10, generic.Length);

            Array string8 = null;
            Allocator.AllocateToSize(ref string8, 1, typeof(String8));
            Assert.IsNotNull(string8 as String8[]);
            Assert.AreEqual(1, string8.Length);

            // Verify generic object creators work
            object list;

            list = Allocator.ConstructGenericOf(typeof(List<>), typeof(int));
            Assert.AreEqual(typeof(List<int>), list.GetType());
            Assert.AreEqual(0, ((List<int>)list).Capacity);

            list = Allocator.ConstructGenericOf(typeof(List<>), typeof(int), 1000);
            Assert.AreEqual(typeof(List<int>), list.GetType());
            Assert.AreEqual(1000, ((List<int>)list).Capacity);
        }
    }
}

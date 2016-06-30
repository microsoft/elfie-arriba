// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Arriba.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Serialization
{
    [TestClass]
    public class CollectionFactoryTests
    {
        [TestMethod]
        public void CollectionFactory_BuildList()
        {
            Assert.AreEqual(typeof(List<int>), CollectionFactory.BuildList(typeof(int)).GetType());
            Assert.AreEqual(typeof(List<bool>), CollectionFactory.BuildList(typeof(bool)).GetType());
            Assert.AreEqual(typeof(List<string>), CollectionFactory.BuildList(typeof(string)).GetType());
            Assert.AreEqual(typeof(List<CollectionFactoryTests>), CollectionFactory.BuildList(typeof(CollectionFactoryTests)).GetType());
            Assert.AreEqual(typeof(List<List<int>>), CollectionFactory.BuildList(typeof(List<int>)).GetType());
            Assert.AreEqual(typeof(List<int[]>), CollectionFactory.BuildList(typeof(int[])).GetType());
        }

        [TestMethod]
        public void CollectionFactory_BuildArray()
        {
            BuildAndVerifyArray(typeof(int), 0);
            BuildAndVerifyArray(typeof(int), 10);
            BuildAndVerifyArray(typeof(bool), 10);
            BuildAndVerifyArray(typeof(string), 10);
            BuildAndVerifyArray(typeof(CollectionFactoryTests), 10);
            BuildAndVerifyArray(typeof(List<int>), 10);
            BuildAndVerifyArray(typeof(int[]), 10);
        }

        private static Array BuildAndVerifyArray(Type t, int length)
        {
            Array array = CollectionFactory.BuildArray(t, length);
            Assert.AreEqual(t.MakeArrayType(), array.GetType());
            Assert.AreEqual(length, array.Length);

            if (length > 0)
            {
                object firstValue = array.GetValue(0);
                if (firstValue != null)
                {
                    Assert.AreEqual(t, firstValue.GetType());
                }
            }

            return array;
        }
    }
}

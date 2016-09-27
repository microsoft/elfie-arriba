// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Extensions
{
    [TestClass]
    public class StringExtensionsTests
    {
        [TestMethod]
        public void StringExtensions_Basics()
        {
            // String.Join
            string[] set = { "One", "Two", null, "Three" };
            Assert.AreEqual("One, Two, <null>, Three", StringExtensions.Join(", ", ((IEnumerable<string>)set).GetEnumerator()));

            // ToSHA256
            byte[] hash = "One".ToSHA256();
            string hashString = "One".ToSHA256String();
            Assert.AreEqual("8b12507783d5becacbf2ebe5b01a60024d8728a8f86dcc818bce699e8b3320bc", hashString);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Extensions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Extensions
{
    [TestClass]
    public class StringExtensionsTests
    {
        [TestMethod]
        public void StringExtensions_CanonicalizeNewlines_Basic()
        {
            Assert.AreEqual(String.Empty, ((string)null).CanonicalizeNewlines());
            Assert.AreEqual(String.Empty, ((string)String.Empty).CanonicalizeNewlines());

            // Only \r or \n fixed
            Assert.AreEqual("\r\n", "\r".CanonicalizeNewlines());
            Assert.AreEqual("\r\n\r\n", "\r\r".CanonicalizeNewlines());
            Assert.AreEqual("\r\n", "\n".CanonicalizeNewlines());
            Assert.AreEqual("\r\n\r\n", "\n\n".CanonicalizeNewlines());

            // Correct newline left alone
            Assert.AreEqual("\r\n", "\r\n".CanonicalizeNewlines());
            Assert.AreEqual("\r\n\r\n", "\r\n\r\n".CanonicalizeNewlines());

            // \n\r ends up as two newlines
            Assert.AreEqual("\r\n\r\n", "\n\r".CanonicalizeNewlines());

            // Other characters not messed up
            Assert.AreEqual("\r\nSome\r\nSome\r\nSome\r\n", "\nSome\rSome\nSome\r\n".CanonicalizeNewlines());
        }

        [TestMethod]
        public void StringExtensions_Format()
        {
            DateTime? value = null;
            Assert.AreEqual("One: [" + StringExtensions.NullArgumentReplacement + "]", StringExtensions.Format("One: [{0}]", value));
            Assert.AreEqual("Property: Value", StringExtensions.Format("{0}: {1}", "Property", "Value"));
        }
    }
}

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
    public class UTF8Tests
    {
        [TestMethod]
        public void UTF8_IsSingleByte()
        {
            // All values below 128 are single byte; values larger are multi-byte character bytes.
            Assert.IsTrue(UTF8.IsSingleByte(UTF8.CR));
            Assert.IsTrue(UTF8.IsSingleByte(UTF8.DoubleQuote));
            Assert.IsTrue(UTF8.IsSingleByte(UTF8.A));
            Assert.IsTrue(UTF8.IsSingleByte(UTF8.Z));
            Assert.IsFalse(UTF8.IsSingleByte(0x85));
        }

        [TestMethod]
        public void UTF8_ToLowerInvariantCharacter()
        {
            Assert.AreEqual(UTF8.a, UTF8.ToLowerInvariant(UTF8.a));
            Assert.AreEqual(UTF8.a, UTF8.ToLowerInvariant(UTF8.A));
            Assert.AreEqual(UTF8.z, UTF8.ToLowerInvariant(UTF8.z));
            Assert.AreEqual(UTF8.z, UTF8.ToLowerInvariant(UTF8.Z));
            Assert.AreEqual(UTF8.CR, UTF8.ToLowerInvariant(UTF8.CR));
        }

        [TestMethod]
        public void UTF8_ToLowerInvariantArray()
        {
            byte[] sample;

            // Full Value
            sample = Encoding.UTF8.GetBytes("UpperCase");
            UTF8.ToLowerInvariant(sample, 0, sample.Length);
            Assert.AreEqual("uppercase", Encoding.UTF8.GetString(sample));

            // Already Lowercase
            sample = Encoding.UTF8.GetBytes("already lowercase");
            UTF8.ToLowerInvariant(sample, 0, sample.Length);
            Assert.AreEqual("already lowercase", Encoding.UTF8.GetString(sample));

            // Substring Requested
            sample = Encoding.UTF8.GetBytes("UpperCase");
            UTF8.ToLowerInvariant(sample, 1, sample.Length - 1);
            Assert.AreEqual("Uppercase", Encoding.UTF8.GetString(sample));
        }

        [TestMethod]
        public void UTF8_ToLowerInvariantArray_LengthZero()
        {
            // Expected: No exception. Nothing else can be verified.
            UTF8.ToLowerInvariant(new byte[0], 0, 0);
        }

        [TestMethod]
        public void UTF8_ToLowerInvariantArray_Null()
        {
            Verify.Exception<ArgumentNullException>(
                    () => { UTF8.ToLowerInvariant(null, 0, 0); }
                );
        }

        [TestMethod]
        public void UTF8_ToLowerInvariantArray_IndexSmall()
        {
            Verify.Exception<ArgumentOutOfRangeException>(
                    () => { UTF8.ToLowerInvariant(new byte[4], -1, 1); }
                );
        }

        [TestMethod]
        public void UTF8_ToLowerInvariantArray_IndexBig()
        {
            Verify.Exception<ArgumentOutOfRangeException>(
                    () => { UTF8.ToLowerInvariant(new byte[4], 4, 1); }
                );
        }

        [TestMethod]
        public void UTF8_ToLowerInvariantArray_LengthSmall()
        {
            Verify.Exception<ArgumentOutOfRangeException>(
                    () => { UTF8.ToLowerInvariant(new byte[4], 2, -1); }
                );
        }

        [TestMethod]
        public void UTF8_ToLowerInvariantArray_LengthBig()
        {
            Verify.Exception<ArgumentOutOfRangeException>(
                    () => { UTF8.ToLowerInvariant(new byte[4], 0, 5); }
                );
        }
    }
}

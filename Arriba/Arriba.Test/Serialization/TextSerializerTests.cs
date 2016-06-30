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
    public class TextSerializerTests
    {
        [TestMethod]
        public void TextSerializer_DateTime()
        {
            // Verify regular round trip
            DateTime now = DateTime.Now;
            TextSerializer.Write(now, "Sample.txt");
            DateTime roundTripped = TextSerializer.ReadDateTime("Sample.txt", DateTime.MinValue);
            Assert.AreEqual(now.Ticks, roundTripped.Ticks);

            // Verify default returned for missing files
            Assert.AreEqual(DateTime.MinValue.Ticks, TextSerializer.ReadDateTime("Missing.txt", DateTime.MinValue).Ticks);

            // Verify default returned for non DateTime values
            TextSerializer.Write("Not a DateTime", "WrongFormat.txt");
            Assert.AreEqual(DateTime.MinValue.Ticks, TextSerializer.ReadDateTime("WrongFormat.txt", DateTime.MinValue).Ticks);
        }

        [TestMethod]
        public void TextSerializer_String()
        {
            TextSerializer.Write("Sample Value", "Sample.txt");
            Assert.AreEqual("Sample Value", TextSerializer.ReadString("Sample.txt", "Default Value"));
            Assert.AreEqual("Default Value", TextSerializer.ReadString("Missing.txt", "Default Value"));
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Arriba.Extensions;
using Arriba.Serialization;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class ByteBlockColumnTests
    {
        [TestMethod]
        public void ByteBlockColumn_Basic()
        {
            ColumnTests.ColumnTest_Basics(() => new ByteBlockColumn(@"\"), "Hello", "There");
        }

        [TestMethod]
        public void ByteBlockColumn_RewriteValue()
        {
            ByteBlockColumn set = new ByteBlockColumn(ByteBlock.Zero);
            set.SetSize(3);

            // Set a value, and then set it to itself (should do nothing, but
            // clearing logic must know not to wipe out value).
            set[0] = "Hello";
            set[0] = set[0];
            Assert.AreEqual("Hello", set[0].ToString());
        }

        [TestMethod]
        public void ByteBlockColumn_PackingRules()
        {
            ByteBlockColumn set = new ByteBlockColumn(ByteBlock.Zero);
            Assert.AreEqual(0, set.Count);
            set.SetSize(3);

            // Append three values (should append to partial block)
            set[0] = "Hello";
            set[1] = "There";
            set[2] = "You";

            // Verify they can be read back
            Assert.AreEqual("Hello", set[0].ToString());
            Assert.AreEqual("There", set[1].ToString());
            Assert.AreEqual("You", set[2].ToString());

            // Change a value (should be in place)
            set[0] = "Now";
            Assert.AreEqual("Now", set[0].ToString());

            // Verify the unused suffix was cleared (security)
            ByteBlock b = set[0];
            b.Index += b.Length;
            b.Length = 2;
            Assert.AreEqual("\0\0", b.ToString());

            // Write larger values (should force a compaction)
            set[0] = "Hello.";
            set[1] = "There.";
            set[2] = "You.";

            // Verify they can be read back
            Assert.AreEqual("Hello.", set[0].ToString());
            Assert.AreEqual("There.", set[1].ToString());
            Assert.AreEqual("You.", set[2].ToString());

            // Write an empty value, and then back
            set[1] = String.Empty;
            Assert.AreEqual(String.Empty, set[1].ToString());
            set[1] = "Restored";
            Assert.AreEqual("Restored", set[1].ToString());

            // Write a null value
            set[2] = (string)null;
            Assert.AreEqual(String.Empty, set[2].ToString());
            set[2] = "Placed back";
            Assert.AreEqual("Placed back", set[2].ToString());

            // Write a huge value
            byte[] huge = new byte[8192];
            set[3] = huge;
            Assert.AreEqual(8192, set[3].Length);
            Assert.AreNotEqual(set[3].Array, set[2].Array, "Verify written to a separate array");

            // Shrink the huge value
            set[3] = "Small Value";
            Assert.AreEqual("Small Value", set[3].ToString());
        }

        [TestMethod]
        public void ByteBlockColumn_DefaultValue()
        {
            ByteBlock defaultValue = "...";
            ByteBlockColumn set = new ByteBlockColumn(defaultValue);
            set.SetSize(3);

            // Verify values are initialized to default
            Assert.AreEqual(defaultValue, set[0]);
            Assert.AreEqual(defaultValue, set[1]);
            Assert.AreEqual(defaultValue, set[2]);

            // Verify a changed value is set and doesn't disturb other defaults
            ByteBlock anotherValue = "Hello";
            set[1] = anotherValue;
            Assert.AreEqual(defaultValue, set[0]);
            Assert.AreEqual(anotherValue, set[1]);
            Assert.AreEqual(defaultValue, set[2]);

            // Verify shrink and re-expand resets values to default
            set.SetSize(0);
            set.SetSize(3);
            Assert.AreEqual(defaultValue, set[0]);
            Assert.AreEqual(defaultValue, set[1]);
            Assert.AreEqual(defaultValue, set[2]);
        }
    }
}

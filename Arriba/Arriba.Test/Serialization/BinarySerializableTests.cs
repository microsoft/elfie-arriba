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
    public class BinarySerializableTests
    {
        [TestMethod]
        public void BinarySerializable_BasicRoundTrip()
        {
            SampleSerializable sample = new SampleSerializable(12345);
            sample.Write("Sample");

            SampleSerializable sampleRead = new SampleSerializable(0);
            Assert.IsTrue(sampleRead.TryRead("Sample"));

            Assert.AreEqual(4, BinarySerializable.Size("Sample"));
            Assert.AreEqual(sample.Value, sampleRead.Value);
        }

        [TestMethod]
        public void BinarySerializable_EnumerateAndDelete()
        {
            IEnumerable<string> items;

            // Delete any pre-existing items
            foreach (string serializedItem in BinarySerializable.EnumerateUnder("."))
            {
                BinarySerializable.Delete(serializedItem);
            }

            // Add one item
            SampleSerializable sample = new SampleSerializable(12345);
            sample.Write("Sample");

            // Verify folder size finds one item only
            Assert.AreEqual(4, BinarySerializable.Size("."));

            // Verify it is found
            items = BinarySerializable.EnumerateUnder(".");
            Assert.AreEqual(1, items.Count());

            // Add another item
            sample.Write("Sample2");

            // Verify both are found
            items = BinarySerializable.EnumerateUnder(".");
            Assert.AreEqual(2, items.Count());

            // Verify folder size finds both items
            Assert.AreEqual(8, BinarySerializable.Size("."));

            // Delete first item; verify count drops
            BinarySerializable.Delete("Sample");
            items = BinarySerializable.EnumerateUnder(".");
            Assert.AreEqual(1, items.Count());
        }

        [TestMethod]
        public void BinarySerializable_FileMissing()
        {
            SampleSerializable sample = new SampleSerializable(0);
            Assert.IsFalse(sample.TryRead("FileNotFound"));
        }

        [TestMethod]
        public void BinarySerializable_WrongTypeRead()
        {
            SampleSerializable sample = new SampleSerializable(12345);
            sample.Write("Sample");

            SampleSerializable2 wrongType = new SampleSerializable2(0);
            Assert.IsFalse(wrongType.TryRead("Sample"));
        }

        private class SampleSerializable : IBinarySerializable
        {
            public int Value { get; set; }

            public SampleSerializable(int value)
            {
                this.Value = value;
            }

            public override string ToString()
            {
                return this.Value.ToString();
            }

            public void ReadBinary(ISerializationContext context)
            {
                this.Value = context.Reader.ReadInt32();
            }

            public void WriteBinary(ISerializationContext context)
            {
                context.Writer.Write(this.Value);
            }
        }

        private class SampleSerializable2 : IBinarySerializable
        {
            public double Value { get; set; }

            public SampleSerializable2(double value)
            {
                this.Value = value;
            }

            public override string ToString()
            {
                return this.Value.ToString();
            }

            public void ReadBinary(ISerializationContext context)
            {
                this.Value = context.Reader.ReadDouble();
            }

            public void WriteBinary(ISerializationContext context)
            {
                context.Writer.Write(this.Value);
            }
        }
    }
}

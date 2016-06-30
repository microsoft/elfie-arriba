// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Extensions
{
    [TestClass]
    public class BinaryReaderWriterExtensionsTests
    {
        [TestMethod]
        public void ReadWriteList()
        {
            List<SampleSerializable> simple = new List<SampleSerializable>() { 1, 2, 3, 4 };
            List<SampleSerializable> readValues;

            MemoryStream buffer = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(buffer))
            {
                // Write the array
                writer.Write(simple);
                long lengthWritten = buffer.Position;

                // Seek back and read it back
                buffer.Seek(0, SeekOrigin.Begin);

                BinaryReader reader = new BinaryReader(buffer);
                readValues = reader.ReadList<SampleSerializable>();
            }

            Assert.AreEqual(simple.Count, readValues.Count);
            for (int i = 0; i < simple.Count; ++i)
            {
                Assert.AreEqual(simple[i], readValues[i]);
            }
        }

        [TestMethod]
        public void ReadWritePrimitiveArray()
        {
            // Partial write of int[]
            RoundTripAndVerify(new int[] { 5, 19, 23, -56, 0, 14, 5 }, 1, 5);

            // Full write of byte[]
            RoundTripAndVerify(new byte[] { 5, 19, 23, 56, 0, 14, 5 }, 0, 7);

            // Empty array write
            RoundTripAndVerify(new int[] { }, 0, 0);
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void WritePrimitiveArray_NotPrimitive()
        {
            RoundTripAndVerify(new DateTime[] { DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today.AddDays(-1) }, 0, 3);
        }

        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void WritePrimitiveArray_IndexOutOfBounds()
        {
            RoundTripAndVerify(new int[] { 5, 19, 23, -56, 0, 14, 5 }, 7, 1);
        }

        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void WritePrimitiveArray_LengthOutOfBounds()
        {
            RoundTripAndVerify(new int[] { 5, 19, 23, -56, 0, 14, 5 }, 1, 7);
        }

        [TestMethod, ExpectedException(typeof(IOException))]
        public void ReadPrimitiveArray_WrongElementSize()
        {
            MemoryStream buffer = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(buffer))
            {
                // Write the array
                writer.WritePrimitiveArray(new long[] { 0, 1, 2, 3 }, 0, 4);
                long lengthWritten = buffer.Position;

                // Seek back and read it back
                buffer.Seek(0, SeekOrigin.Begin);

                BinaryReader reader = new BinaryReader(buffer);
                int[] readValues = reader.ReadPrimitiveArray<int>();
            }
        }

        private T[] RoundTrip<T>(T[] values, int index, int length)
        {
            T[] readValues;

            MemoryStream buffer = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(buffer))
            {
                // Write the array
                writer.WritePrimitiveArray(values, index, length);
                long lengthWritten = buffer.Position;

                // Seek back and read it back
                buffer.Seek(0, SeekOrigin.Begin);

                BinaryReader reader = new BinaryReader(buffer);
                readValues = reader.ReadPrimitiveArray<T>();

                // Verify same length was read back
                Assert.AreEqual(lengthWritten, buffer.Position, "ReadValueTypeArray should read the same number of bytes that WriteValueTypeArray wrote.");
            }

            return readValues;
        }

        private T[] RoundTripAndVerify<T>(T[] values, int index, int length)
        {
            T[] readValues = RoundTrip(values, index, length);

            // Verify the array itself
            Assert.IsNotNull(readValues);
            Assert.AreEqual(length, readValues.Length, "ReadValueTypeArray length different from length written.");

            for (int i = 0; i < length; ++i)
            {
                Assert.AreEqual(values[index + i], readValues[i], "ReadValueTypeArray value read different from value written.");
            }

            return readValues;
        }
    }
}

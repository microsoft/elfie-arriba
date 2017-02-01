// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using Arriba.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Serialization
{
    [TestClass]
    public class BinaryBlockSerializerTests
    {
        [TestMethod]
        public void BinaryBlockSerializer_Basic()
        {
            int[] sampleValueTypeArray = new int[] { 5, 7, 9, -1 };
            byte[] sampleByteArray = Encoding.UTF8.GetBytes("Hello");

            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                Assert.AreEqual(0, context.Stream.Length, "No data should be written before any write calls are made.");
                BinaryBlockSerializer.WriteArray(context, sampleValueTypeArray);
                BinaryBlockSerializer.WriteArray(context, sampleByteArray);

                context.Stream.Seek(0, SeekOrigin.Begin);
                int[] reloadedValueTypeArray = BinaryBlockSerializer.ReadArray<int>(context);
                byte[] reloadedByteArray = BinaryBlockSerializer.ReadArray<byte>(context);
                Assert.AreEqual(context.Stream.Position, context.Stream.Length, "Stream wasn't fully read when reloading values.");

                Assert.AreEqual(String.Join(", ", sampleValueTypeArray), String.Join(", ", reloadedValueTypeArray));
                Assert.AreEqual(Encoding.UTF8.GetString(sampleByteArray), Encoding.UTF8.GetString(reloadedByteArray));
            }
        }

        [TestMethod]
        public void BinaryBlockSerializer_TypeHandling()
        {
            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                RoundTrip(context, (byte)5);
                RoundTrip(context, (ushort)365);
                RoundTrip(context, (int)-2310);
                RoundTrip(context, (ulong)15);
                RoundTrip(context, 0.004f);
                RoundTrip(context, 0.004d);
                RoundTrip(context, DateTime.UtcNow);
                RoundTrip(context, Guid.NewGuid());
                RoundTrip(context, TimeSpan.FromMilliseconds(567));
            }
        }

        [TestMethod]
        public void BinaryBlockSerializer_ByteArrayAsSingle()
        {
            byte[] value = new byte[] { 0, 127, 128, 255 };

            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                int singleLength = BinaryBlockSerializer.Write(context, value);
                context.Stream.Seek(-singleLength, SeekOrigin.Current);
                byte[] readValue = BinaryBlockSerializer.Read<byte[]>(context);
                Assert.AreEqual(String.Join(", ", value), String.Join(", ", readValue));
            }
        }

        private static void RoundTrip<T>(SerializationContext context, T value)
        {
            // Test Array Reading
            int arrayLength = BinaryBlockSerializer.WriteArray<T>(context, new T[] { value });
            context.Stream.Seek(-arrayLength, SeekOrigin.Current);
            T[] readArray = BinaryBlockSerializer.ReadArray<T>(context);
            Assert.AreEqual(value, readArray[0]);

            // Test Single Reading
            int singleLength = BinaryBlockSerializer.Write(context, value);
            context.Stream.Seek(-singleLength, SeekOrigin.Current);
            T readValue = BinaryBlockSerializer.Read<T>(context);
            Assert.AreEqual(value, readValue);
        }

        [TestMethod]
        public void BinaryBlockSerializer_MismatchTypeReadAndWrite()
        {
            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                int singleLength = BinaryBlockSerializer.Write(context, (int)500);
                context.Stream.Seek(-singleLength, SeekOrigin.Current);
                Verify.Exception<IOException>(
                            () => { ushort wrongType = BinaryBlockSerializer.Read<ushort>(context); }
                        );
            }
        }

        [TestMethod]
        public void BinaryBlockSerializer_BadTerminatorDetection()
        {
            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                int singleLength = BinaryBlockSerializer.WriteArray(context, new int[] { 0, 1, 2, 3 });
                context.Stream.Seek(-1, SeekOrigin.Current);
                context.Stream.WriteByte(0xBD);
                context.Stream.Seek(-singleLength, SeekOrigin.Current);
                Verify.Exception<IOException>(
                            () => { int[] badTerminator = BinaryBlockSerializer.ReadArray<int>(context); }
                        );
            }
        }

        [TestMethod]
        public void BinaryBlockSerializer_Write_UnsupportedType()
        {
            Verify.Exception<IOException>(
                    () => { BinaryBlockSerializer.Write(new SerializationContext(new MemoryStream()), new BinaryBlockSerializerTests()); }
                );
        }

        [TestMethod]
        public void BinaryBlockSerializer_Read_UnsupportedType()
        {
            Verify.Exception<IOException>(
                    () => { BinaryBlockSerializerTests me = BinaryBlockSerializer.Read<BinaryBlockSerializerTests>(new SerializationContext(new MemoryStream())); }
                );
        }

        [TestMethod]
        public void BinaryBlockSerializer_WriteArray_UnsupportedType()
        {
            Verify.Exception<IOException>(
                    () => { BinaryBlockSerializer.WriteArray(new SerializationContext(new MemoryStream()), new BinaryBlockSerializerTests[1]); }
                );
        }

        [TestMethod]
        public void BinaryBlockSerializer_ReadArray_UnsupportedType()
        {
            Verify.Exception<IOException>(
                    () => { BinaryBlockSerializerTests[] us = BinaryBlockSerializer.ReadArray<BinaryBlockSerializerTests>(new SerializationContext(new MemoryStream())); }
                );
        }

#if PERFORMANCE
        [TestMethod]
#endif
        public void BinaryBlockSerializer_Performance()
        {
            // Goal: Block Read and Write are beyond maximum hard drive performance; >1GB per second.
            //  My sample array (a maximum common size we'll store) is 64k * 8 = 512KB, so we want to read and write it >2,000 times per second
            ulong[] largeArray = new ulong[ushort.MaxValue];
            for (int i = 0; i < ushort.MaxValue; ++i)
            {
                largeArray[i] = (ulong)i;
            }

            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                int iterations = 100;
                Stopwatch w = Stopwatch.StartNew();
                for (int i = 0; i < iterations; ++i)
                {
                    BinaryBlockSerializer.WriteArray(context, largeArray);
                    context.Stream.Seek(0, SeekOrigin.Begin);

                    ulong[] readArray = BinaryBlockSerializer.ReadArray<ulong>(context);
                    context.Stream.Seek(0, SeekOrigin.Begin);

                    Assert.AreEqual(largeArray.Length, readArray.Length);
                }

                int operations = (2 * iterations);
                double milliseconds = w.ElapsedMilliseconds;
                double operationsPerSecond = 1000 * operations / milliseconds;
                Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per second.", operations, milliseconds, operationsPerSecond));

                Assert.IsTrue(operationsPerSecond > 1000, "Not within 200% of goal.");
            }
        }
    }
}

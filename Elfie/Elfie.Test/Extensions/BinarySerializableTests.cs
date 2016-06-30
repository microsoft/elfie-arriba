// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Extensions
{
    [TestClass]
    public class BinarySerializableTests
    {
        [TestMethod]
        public void BinarySerializable_Basic()
        {
            // In-memory round trip
            SampleSerializable ss = new SampleSerializable(5);
            ss = RoundTrip(ss);
            Assert.AreEqual(5, ss.Value);

            // Write to file (verify exists, size, save, load)
            string serializationPath = "SampleSerializable.bin";
            ss.FileWrite(serializationPath);
            Assert.IsTrue(File.Exists(serializationPath));
            Assert.AreEqual(4, new FileInfo(serializationPath).Length);

            ss.Value = 2;
            ss.FileRead(serializationPath);
            Assert.AreEqual(5, ss.Value);

            // Write 'empty' to file - verify no file
            ss.Value = -1;
            ss.FileWrite(serializationPath);
            Assert.IsFalse(File.Exists(serializationPath));
            ss.Value = 5;

            // Write to subfolder - verify created
            string pathInSubfolder = @"Index\SampleSerializable.bin";
            string folderPath = @"Index";
            if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true);
            ss.FileWrite(pathInSubfolder);
            Assert.IsTrue(Directory.Exists(folderPath));
            Assert.IsTrue(File.Exists(pathInSubfolder));

            ss.Value = 2;
            ss.FileRead(pathInSubfolder);
            Assert.AreEqual(5, ss.Value);
            Directory.Delete(folderPath, true);
        }

        public static T RoundTrip<T>(T item, Action<BinaryWriter> change = null) where T : IBinarySerializable, new()
        {
            MemoryStream buffer = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(buffer))
            {
                // Write the array
                item.WriteBinary(writer);
                long lengthWritten = buffer.Position;

                // Seek back
                buffer.Seek(0, SeekOrigin.Begin);

                // Allow changes [if caller passed]
                if (change != null)
                {
                    change(writer);
                }

                // Seek back and read it back
                buffer.Seek(0, SeekOrigin.Begin);

                BinaryReader reader = new BinaryReader(buffer);

                T readValue = new T();
                readValue.ReadBinary(reader);

                // Verify same length was read back
                Assert.AreEqual(lengthWritten, buffer.Position, "ReadBinary didn't read back everything WriteBinary wrote.");

                return readValue;
            }
        }
    }
}

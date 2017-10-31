// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Extensions;
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
            SampleSerializable ss2 = new SampleSerializable(10);
            Verify.RoundTrip(ss, ss2);
            Assert.AreEqual(5, ss2.Value);

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
    }
}

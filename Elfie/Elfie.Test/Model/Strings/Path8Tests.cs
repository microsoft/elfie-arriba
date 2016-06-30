// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model.Strings
{
    [TestClass]
    public class Path8Tests
    {
        [TestMethod]
        public void Path8_Basics()
        {
            PackageDatabase db = PackageDatabaseTests.BuildDefaultSample();
            Symbol tryLog = PackageDatabaseTests.GetTryLogFromSample(db);

            Path8 fullName = tryLog.FullName;
            Assert.IsFalse(fullName.IsEmpty);
            Assert.IsFalse(fullName.IsRoot);
            Assert.AreEqual(32, fullName.Length);
            Assert.AreEqual("Sample.Diagnostics.Logger.TryLog", fullName.ToString());
            Assert.AreEqual("Sample.Diagnostics.Logger.TryLog", Write.ToString(fullName.WriteTo));
            Assert.AreEqual("TryLog", fullName.Name.ToString());
            Assert.AreEqual("Logger", fullName.Parent.Name.ToString());

            Path8 filePath = tryLog.FilePath;
            Assert.IsFalse(filePath.IsEmpty);
            Assert.IsFalse(filePath.IsRoot);
            Assert.AreEqual(PackageDatabaseTests.LOGGER_PATH_LIBNET20.Length, filePath.Length);
            Assert.AreEqual(PackageDatabaseTests.LOGGER_PATH_LIBNET20, filePath.ToString());
            Assert.AreEqual(PackageDatabaseTests.LOGGER_PATH_LIBNET20, Write.ToString(filePath.WriteTo));
            Assert.AreEqual("Logger.cs", filePath.Name.ToString());
            Assert.AreEqual(PackageDatabaseTests.NS_DIAGNOSTICS, filePath.Parent.Name.ToString());
        }

        [TestMethod]
        public void Path8_Empty()
        {
            Path8 empty = Path8.Empty;
            Assert.IsTrue(empty.IsEmpty);
            Assert.IsTrue(empty.IsRoot);
            Assert.AreEqual(0, empty.Length);
            Assert.AreEqual(String8.Empty, empty.Name);
            Assert.AreEqual(Path8.Empty, empty.Parent);
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void Path8_DisallowedDelimiter()
        {
            Path8 unconstructable = new Path8(new StringStore(), new ItemTree(), 0, '\u03B2');
        }
    }
}

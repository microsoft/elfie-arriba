// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Reflection;

using EI = Microsoft.CodeAnalysis.Elfie.Indexer;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Elfie.Indexer
{
    [TestClass]
    public class IndexerTests
    {
        private static readonly string s_indexerTestsNamespace = typeof(IndexerTests).Namespace;
        private PackageDatabase _cachedDB;

        private PackageDatabase DB
        {
            get
            {
                if (_cachedDB == null)
                {
                    // Index Elfie.Test with Elfie.Indexer
                    _cachedDB = EI.IndexCommand.Index(Assembly.GetExecutingAssembly().Location, true);
                    _cachedDB.ConvertToImmutable();
                }

                return _cachedDB;
            }
        }

        [TestMethod]
        public void Indexer_EndToEnd_Basic()
        {
            // Verify this class itself is represented
            MemberQuery q = new MemberQuery("IndexerTests", true, false);
            PartialArray<Symbol> results = new PartialArray<Symbol>(10);
            Assert.IsTrue(q.TryFindMembers(DB, ref results));

            Symbol first = results[0];
            Assert.AreEqual("Elfie.NonCore.Test", first.AssemblyName.ToString(), "Unexpected assembly name:" + first.AssemblyName.ToString());
            Assert.AreEqual("IndexerTests", first.Name.ToString());
            Assert.AreEqual(s_indexerTestsNamespace + ".IndexerTests", first.FullName.ToString());
            Assert.AreEqual(SymbolType.Class, first.Type);
            Assert.AreEqual(SymbolModifier.Public, first.Modifiers);
            Assert.AreEqual("IndexerTests.cs", first.FilePath.Name.ToString());
            Assert.AreEqual(18, first.CharInLine);
        }

        [TestMethod]
        public void Indexer_GenericSignature()
        {
            MemberQuery q = new MemberQuery("GenericSignature", true, false);
            PartialArray<Symbol> results = new PartialArray<Symbol>(10);

            Assert.IsTrue(q.TryFindMembers(DB, ref results));
            Symbol first = results[0];
            Assert.AreEqual("List<Q>", first.Parameters.ToString());
        }

        public int GenericSignature<Q>(List<Q> items)
        {
            return items.Count;
        }

        [TestMethod]
        public void Indexer_PrivateMethod()
        {
            MemberQuery q = new MemberQuery("PrivateMethod", true, false);
            PartialArray<Symbol> results = new PartialArray<Symbol>(10);

            // Verify private methods are indexed and modifiers are right
            Assert.IsTrue(q.TryFindMembers(DB, ref results));
            Symbol first = results[0];
            Assert.AreEqual(SymbolModifier.Private, first.Modifiers);
        }

        private string PrivateMethod()
        {
            return string.Empty;
        }

        [TestMethod]
        public void Indexer_NestedClass()
        {
            MemberQuery q = new MemberQuery("NestedClass", true, false);
            PartialArray<Symbol> results = new PartialArray<Symbol>(10);

            // Verify the nested class is found and listed under the class
            Assert.IsTrue(q.TryFindMembers(DB, ref results));
            Symbol first = results[0];
            Assert.AreEqual(s_indexerTestsNamespace + ".IndexerTests.NestedClass", first.FullName.ToString());

            // Verify a field within it is found
            q.SymbolName = "NestedClass.PublicStringField";
            Assert.IsTrue(q.TryFindMembers(DB, ref results));
            first = results[0];
            Assert.AreEqual(SymbolType.Field, first.Type);
            Assert.AreEqual(SymbolModifier.Public, first.Modifiers);
            Assert.AreEqual(s_indexerTestsNamespace + ".IndexerTests.NestedClass.PublicStringField", first.FullName.ToString());
        }

        internal class NestedClass
        {
            public string PublicStringField = string.Empty;
        }
    }
}

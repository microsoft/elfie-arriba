// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model
{
    [TestClass]
    public class MergedMembersDatabaseTests
    {
        public static readonly string DIAGNOSTICS_NAMESPACE_LIB20 = AddReferenceDatabaseTests.DIAGNOSTICS_NAMESPACE_IDX_LIB20;
        public static readonly string DIAGNOSTICS_NAMESPACE_REF35 = AddReferenceDatabaseTests.DIAGNOSTICS_NAMESPACE_IDX_REF35;
        public static readonly string TYPE_LOGGER = DIAGNOSTICS_NAMESPACE_LIB20 + PackageDatabaseTests.SEPARATOR + PackageDatabaseTests.TYPE_LOGGER;

        [TestMethod]
        public void MergedMembersDatabase_Merging()
        {
            MergedMembersDatabase db = new MergedMembersDatabase();
            DatabaseAddResult result;
            string lastAddResult;

            PackageDatabase source = PackageDatabaseTests.BuildDefaultSample();
            source.Identity.PackageName = "V1";
            result = db.Add(source, ArdbVersion.Current);
            lastAddResult = Write.ToString(result.WriteMemberResults);
            Trace.WriteLine("First Sample Import:\r\n" + lastAddResult);
            Trace.WriteLine(Write.ToString(result.WriteDuplicateComponents));

            // Sample has at least one unique thing
            Assert.IsTrue(result.WasMemberAdded[0].Value);

            // Memory class is included
            Assert.IsTrue(result.WasMemberAdded[ItemTreeTests.FindByPath(source.DeclaredMembers, source.StringStore, DIAGNOSTICS_NAMESPACE_REF35 + "|Memory", PackageDatabaseTests.SEPARATOR_CHAR)].Value);

            // FromGigabytes member is ignored (not a type)
            Assert.IsFalse(result.WasMemberAdded[ItemTreeTests.FindByPath(source.DeclaredMembers, source.StringStore, DIAGNOSTICS_NAMESPACE_REF35 + "|Memory|FromGigabytes", PackageDatabaseTests.SEPARATOR_CHAR)].HasValue);

            // Add the source again (a complete duplicate)
            source.Identity.PackageName = "V2";
            result = db.Add(source, ArdbVersion.Current);
            lastAddResult = Write.ToString(result.WriteMemberResults);
            Trace.WriteLine("Duplicate Sample Import:\r\n" + lastAddResult);
            Trace.WriteLine(Write.ToString(result.WriteDuplicateComponents));

            // Verify nothing is unique this time
            Assert.IsFalse(result.WasMemberAdded[0].Value);

            // Add a new public class to the sample (should be added)
            MutableSymbol diagnostics = source.MutableRoot.FindByFullName(DIAGNOSTICS_NAMESPACE_LIB20, PackageDatabaseTests.SEPARATOR_CHAR);
            diagnostics.AddChild(new MutableSymbol("TraceWatch", SymbolType.Class) { Modifiers = SymbolModifier.Public | SymbolModifier.Static });

            // Add a new method to Logger (no effect)
            MutableSymbol logger = source.MutableRoot.FindByFullName(DIAGNOSTICS_NAMESPACE_LIB20 + "|Logger", PackageDatabaseTests.SEPARATOR_CHAR);
            logger.AddChild(new MutableSymbol("LogTime", SymbolType.Method) { Modifiers = SymbolModifier.Public });

            // Add the source with additions, verify something is new
            source.Identity.PackageName = "V3";
            result = db.Add(source, ArdbVersion.Current);
            lastAddResult = Write.ToString(result.WriteMemberResults);
            Trace.WriteLine("Sample with additions Import:\r\n" + lastAddResult);
            Trace.WriteLine(Write.ToString(result.WriteDuplicateComponents));
            Assert.IsTrue(result.WasMemberAdded[0].Value);

            // Verify Diagnostics contains changes
            Assert.IsTrue(result.WasMemberAdded[ItemTreeTests.FindByPath(source.DeclaredMembers, source.StringStore, DIAGNOSTICS_NAMESPACE_LIB20, PackageDatabaseTests.SEPARATOR_CHAR)].Value);

            // Verify Logger wasn't considered changed
            Assert.IsFalse(result.WasMemberAdded[ItemTreeTests.FindByPath(source.DeclaredMembers, source.StringStore, TYPE_LOGGER, PackageDatabaseTests.SEPARATOR_CHAR)].Value);

            // Add a new private class to the sample (should not be added)
            diagnostics.AddChild(new MutableSymbol("SmartTimer", SymbolType.Class) { Modifiers = SymbolModifier.Internal });

            // Add the source again, verify nothing is new
            source.Identity.PackageName = "V4";
            result = db.Add(source, ArdbVersion.Current);
            lastAddResult = Write.ToString(result.WriteMemberResults);
            Trace.WriteLine("Sample with private class Import:\r\n" + lastAddResult);
            Trace.WriteLine(Write.ToString(result.WriteDuplicateComponents));
            Assert.IsFalse(result.WasMemberAdded[0].Value);

            Trace.WriteLine(Write.ToString((w) => db.WriteMergedTree(w)));
        }
    }
}

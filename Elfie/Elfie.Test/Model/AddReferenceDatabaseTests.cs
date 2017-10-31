// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.CodeAnalysis.Elfie.Test.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model
{
    [TestClass]
    public class AddReferenceDatabaseTests
    {
        // "SamplePackage|lib\net20|Sample.dll|Sample|Diagnostics"
        public static readonly string DIAGNOSTICS_NAMESPACE_IDX_LIB20 = BuildDiagnosticsNamespaceFor(PackageDatabaseTests.PACKAGE_NAME);
        public static readonly string DIAGNOSTICS_NAMESPACE_IDX_REF35 = BuildDiagnosticsNamespaceFor(PackageDatabaseTests.PACKAGE_NAME, PackageDatabaseTests.NET35);

        public static readonly string TYPE_LOGGER_IDX_LIB20 =
            DIAGNOSTICS_NAMESPACE_IDX_LIB20 + PackageDatabaseTests.SEPARATOR_CHAR + PackageDatabaseTests.TYPE_LOGGER;

        public static readonly string TYPE_MEMORY_IDX =
            PackageDatabaseTests.PACKAGE_NAME + PackageDatabaseTests.SEPARATOR_CHAR +
            PackageDatabaseTests.NET35 + PackageDatabaseTests.SEPARATOR_CHAR +
            PackageDatabaseTests.DLL_NAME + PackageDatabaseTests.SEPARATOR_CHAR +
            PackageDatabaseTests.NS_SAMPLE + PackageDatabaseTests.SEPARATOR_CHAR +
            PackageDatabaseTests.NS_DIAGNOSTICS;

        public static readonly string PRERELEASE_TYPE_LOGGER_IDX =
            PackageDatabaseTests.PACKAGE_NAME + PackageDatabaseTests.SEPARATOR_CHAR +
            PackageDatabaseTests.NET20 + PackageDatabaseTests.SEPARATOR_CHAR +
            PackageDatabaseTests.DLL_NAME + PackageDatabaseTests.SEPARATOR_CHAR +
            PackageDatabaseTests.NS_SAMPLE + PackageDatabaseTests.SEPARATOR_CHAR +
            PackageDatabaseTests.NS_DIAGNOSTICS;

        [TestMethod]
        public void AddReferenceDatabase_BasicV1()
        {
            AddReferenceDatabaseBasicHelper(ArdbVersion.V1);
        }

        public void AddReferenceDatabase_Basic()
        {
            AddReferenceDatabaseBasicHelper(ArdbVersion.Current);
        }

        private void AddReferenceDatabaseBasicHelper(ArdbVersion version)
        {
            AddReferenceDatabase ardb = new AddReferenceDatabase(version);
            DatabaseAddResult result;

            // Build and add the sample PackageDatabase
            PackageDatabase source = PackageDatabaseTests.BuildDefaultSample("V1");
            result = CallAddUniqueMembers(ardb, source);

            // Verify at least something was added
            int ardbCountFirstAdd = ardb.Count;
            Assert.IsTrue(result.WasMemberAdded[0].Value);

            // Add the sample again; verify nothing was added
            source = PackageDatabaseTests.BuildDefaultSample("V2");
            result = CallAddUniqueMembers(ardb, source);
            Assert.IsFalse(result.WasMemberAdded[0].Value);
            Assert.AreEqual(ardbCountFirstAdd, ardb.Count);

            // Add a namespace with a private class; verify nothing added
            source = PackageDatabaseTests.BuildDefaultSample("V3");
            MutableSymbol diagnostics = source.MutableRoot.FindByFullName(BuildDiagnosticsNamespaceFor(source.Identity.PackageName), PackageDatabaseTests.SEPARATOR_CHAR);
            MutableSymbol internalNs = diagnostics.AddChild(new MutableSymbol("Internal", SymbolType.Namespace));
            internalNs.AddChild(new MutableSymbol("Tracer", SymbolType.Class) { Modifiers = SymbolModifier.Internal });
            result = CallAddUniqueMembers(ardb, source);
            Assert.IsFalse(result.WasMemberAdded[0].Value);
            Assert.AreEqual(ardbCountFirstAdd, ardb.Count);

            // Add a new public class (existing namespace); verify it is added
            source = PackageDatabaseTests.BuildDefaultSample("V4");
            diagnostics = source.MutableRoot.FindByFullName(BuildDiagnosticsNamespaceFor(source.Identity.PackageName), PackageDatabaseTests.SEPARATOR_CHAR);
            diagnostics.AddChild(new MutableSymbol("TraceWatch", SymbolType.Class) { Modifiers = SymbolModifier.Public | SymbolModifier.Static });
            result = CallAddUniqueMembers(ardb, source);
            Assert.IsTrue(result.WasMemberAdded[0].Value);
            Assert.IsTrue(result.WasMemberAdded[result.WasMemberAdded.Length - 1].Value);
            Assert.AreNotEqual(ardbCountFirstAdd, ardb.Count);

            // Verify a query [expect Diagnostics. to match Logger, Memory, and TraceWatch
            ardb.ConvertToImmutable();
            VerifyQueryResults(ardb, version);

            // Double-convert ARDB. Verify queries still work correctly.
            ardb.ConvertToImmutable();
            VerifyQueryResults(ardb, version);

            // Round trip to string; verify query still right, count matches
            string sampleArdbFilePath = "Sample.ardb.txt";
            Write.ToFile(ardb.WriteText, sampleArdbFilePath);
            AddReferenceDatabase reloaded = new AddReferenceDatabase(version);
            Read.FromFile(reloaded.ReadText, sampleArdbFilePath);

            VerifyQueryResults(reloaded, version);
            Assert.AreEqual(ardb.Count, reloaded.Count);

            string sampleRewriteArdbFilePath = "Sample.Rewrite.ardb.txt";
            Write.ToFile(reloaded.WriteText, sampleRewriteArdbFilePath);
            Assert.AreEqual(File.ReadAllText(sampleArdbFilePath), File.ReadAllText(sampleRewriteArdbFilePath));
        }

        private DatabaseAddResult CallAddUniqueMembers(AddReferenceDatabase ardb, PackageDatabase source)
        {
            return ardb.AddUniqueMembers(source);
        }

        private static string BuildDiagnosticsNamespaceFor(string packageName, string frameworkTarget = PackageDatabaseTests.NET20)
        {
            return packageName + PackageDatabaseTests.SEPARATOR_CHAR +
                   PackageDatabaseTests.DLL_NAME + PackageDatabaseTests.SEPARATOR_CHAR +
                   frameworkTarget + PackageDatabaseTests.SEPARATOR_CHAR +
                   PackageDatabaseTests.NS_SAMPLE + PackageDatabaseTests.SEPARATOR_CHAR +
                   PackageDatabaseTests.NS_DIAGNOSTICS;
        }

        private void VerifyQueryResults(AddReferenceDatabase ardb, ArdbVersion version)
        {
            // "Diagnostics."
            MemberQuery query = new MemberQuery(PackageDatabaseTests.NS_DIAGNOSTICS + ".", false, false);
            PartialArray<Symbol> results = new PartialArray<Symbol>(10);
            query.TryFindMembers(ardb, ref results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("Logger, Memory, TraceWatch", PackageDatabaseTests.ResultNamesToString(results));

            if (version == ArdbVersion.V1)
            {
                // V1 has no TFM data. This call also verifies that most current client
                // can query older format without raising an exception.
                Assert.AreEqual(String8.Empty, ardb.GetFrameworkTargets(results[0].Index));
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                Symbol symbol = results[i];
                string fx = ardb.GetFrameworkTargets(symbol.Index).ToString();

                if (symbol.Name.ToString() == "TraceWatch")
                {
                    Assert.AreEqual(PackageDatabaseTests.NET20, fx);
                }
                else if (symbol.Name.ToString() == "Memory")
                {
                    Assert.AreEqual(PackageDatabaseTests.NET35, fx);
                }
                else
                {
                    Assert.AreEqual(@"<tfms><tfm>net20</tfm><tfm>net35</tfm></tfms>", fx);
                }
            }
        }

        [TestMethod]
        public void AddReferenceDatabase_PackagePrereleaseVersion()
        {
            AddReferenceDatabase ardb = new AddReferenceDatabase(ArdbVersion.Current);
            // Build and add the sample PackageDatabase
            PackageDatabase source = PackageDatabaseTests.BuildPreleaseSample();
            ardb.AddUniqueMembers(source);
            ardb.ConvertToImmutable();

            MemberQuery query = new MemberQuery(PackageDatabaseTests.TYPE_LOGGER, false, false);
            PartialArray<Symbol> results = new PartialArray<Symbol>(10);
            query.TryFindMembers(ardb, ref results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(PackageDatabaseTests.PRERELEASE_VERSION, ardb.GetPackageVersion(results[0].Index).ToString());
        }

        [TestMethod]
        public void AddReferenceDatabase_PackageReleaseVersion()
        {
            AddReferenceDatabase ardb = new AddReferenceDatabase(ArdbVersion.Current);
            // Build and add the sample PackageDatabase
            PackageDatabase source = PackageDatabaseTests.BuildDefaultSample();
            ardb.AddUniqueMembers(source);
            ardb.ConvertToImmutable();

            MemberQuery query = new MemberQuery(PackageDatabaseTests.TYPE_LOGGER, false, false);
            PartialArray<Symbol> results = new PartialArray<Symbol>(10);
            query.TryFindMembers(ardb, ref results);
            Assert.AreEqual(1, results.Count);

            // We shouldn't persist non-prerelease version details in the ARDB
            Assert.AreEqual(String.Empty, ardb.GetPackageVersion(results[0].Index).ToString());
        }

        [TestMethod]
        public void AddReferenceDatabase_DuplicateMerging()
        {
            int defaultArdbMemberCount, addedDefaultSampleDataMemberCount;
            AddReferenceDatabase ardb = new AddReferenceDatabase(ArdbVersion.Current);
            PackageDatabase db = PackageDatabaseTests.BuildDefaultSample();

            defaultArdbMemberCount = ardb.DeclaredMembers.Count;

            // Add the database once
            ardb.AddUniqueMembers(db);
            int memberCount = ardb.DeclaredMembers.Count;

            addedDefaultSampleDataMemberCount = (ardb.DeclaredMembers.Count - defaultArdbMemberCount);

            db = PackageDatabaseTests.BuildDefaultSample(Guid.NewGuid().ToString());

            // Change only the package name. We should see no change
            // in member count, since all package contents are duped.
            memberCount = ardb.DeclaredMembers.Count;
            ardb.AddUniqueMembers(db);
            Assert.AreEqual(memberCount, ardb.DeclaredMembers.Count);

            db = PackageDatabaseTests.BuildDefaultSample(Guid.NewGuid().ToString());

            // Add allowing duplicates should add the same number of
            // new members as fresh construction of the sample data
            // + 2 (to account for a duplicated type and for
            // an added assembly node).
            memberCount = ardb.DeclaredMembers.Count + addedDefaultSampleDataMemberCount;
            ardb.AddReferenceAssemblyTypes(db);
            Assert.AreEqual(memberCount + 2, ardb.DeclaredMembers.Count);

            // Write the ARDB tree [debuggability]
            string result = Write.ToString(ardb.WriteText);
            Trace.WriteLine(result);
        }

        [TestMethod]
        public void AddReferenceDatabase_NewlineFlexibility()
        {
            PackageDatabase db = PackageDatabaseTests.BuildDefaultSample();

            AddReferenceDatabase ardb = new AddReferenceDatabase(ArdbVersion.Current);
            ardb.AddUniqueMembers(db);
            string originalTextFormat = Write.ToString(ardb.WriteText);

            // Ensure non-Windows newlines don't trip up ReadText
            string modifiedNewlines = originalTextFormat.Replace("\r\n", "\n");
            AddReferenceDatabase ardb2 = new AddReferenceDatabase(ArdbVersion.Current);
            Read.FromString(ardb2.ReadText, modifiedNewlines);

            // Verify everything in the ARDB round-tripped to validate read success
            string roundTripped = Write.ToString(ardb2.WriteText);
            Assert.AreEqual(originalTextFormat, roundTripped);
        }

        [TestMethod]
        public void AddReferenceDatabase_TextFormat_FilterUnknown()
        {
            // Verify unknown SymbolTypes are excluded ('!'), 
            // descendants of excluded elements are added under parent (Logger), 
            // and later siblings are correct (Extensions).

            // We keep descendants rather than excluding them so that new types can be added higher in the tree (ex: Package Release, Assembly Hash Signature)
            string sampleTree = @"Elfie V2
20160126
P SamplePackage
    R 17
        A Sample.dll
            N Sample
                ! Diagnostics
                    C Logger
                        C TryLog
                    C Memory
                    C TraceWatch
                N Extensions
                    C IntExtensions
".TrimStart().Replace("    ", "\t");

            string expectedLoadResult = @"Elfie V2
20160126
P SamplePackage
    R 17
        A Sample.dll
            N Sample
                N Extensions
                    C IntExtensions
                C Logger
                    C TryLog
                C Memory
                C TraceWatch
".TrimStart().Replace("    ", "\t");

            AddReferenceDatabase db = new AddReferenceDatabase(ArdbVersion.Current);
            db.ReadText(new StringReader(sampleTree));

            string result = Write.ToString(db.WriteText);
            Assert.AreEqual(expectedLoadResult, result);
        }

        [TestMethod]
        public void AddReferenceDatabase_Prerelease()
        {
            // Verify unknown SymbolTypes are excluded ('!'), 
            // descendants of excluded elements are added under parent (Logger), 
            // and later siblings are correct (Extensions).
            // We keep descendants rather than excluding them so that new types can be added higher in the tree (ex: Package Release, Assembly Hash Signature)
            string sampleTree = @"Elfie V2
20160126
P SamplePackage
    V 1.2.3-beta1
        R 17
            A Sample.dll
                N Extensions
                    C IntExtensions
                N Sample
                    C Logger
                        C TryLog
                    C Memory
                    C TraceWatch
".TrimStart().Replace("    ", "\t");

            string expectedLoadResult = sampleTree;

            AddReferenceDatabase db = new AddReferenceDatabase(ArdbVersion.Current);
            db.ReadText(new StringReader(sampleTree));

            string result = Write.ToString(db.WriteText);
            Assert.AreEqual(expectedLoadResult, result);
        }

        [TestMethod, ExpectedException(typeof(IOException))]
        public void AddReferenceDatabase_BinaryWrongVersionLoading()
        {
            AddReferenceDatabase db = new AddReferenceDatabase(ArdbVersion.Current);
            db.AddUniqueMembers(PackageDatabaseTests.BuildDefaultSample());

            // Overwrite version with '9' and verify binary format won't load
            Verify.RoundTrip(db, new AddReferenceDatabase(), (w) => w.Write(9));
        }
    }
}

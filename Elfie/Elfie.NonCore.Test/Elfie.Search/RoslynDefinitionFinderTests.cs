// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Elfie.Search
{
    [TestClass]
    public class RoslynDefinitionFinderTests
    {
        private static readonly string s_assemblyNamespace = typeof(Microsoft.CodeAnalysis.Elfie.Indexer.Assembly).Namespace;
        private static readonly string s_rsDsSignatureNamespace = typeof(Microsoft.CodeAnalysis.Elfie.Indexer.RsDsSignature).Namespace;

        private const string s_sampleDefinitionsNamespace = "Microsoft.CodeAnalysis.Elfie.Test.Elfie.Search";

        private const string s_resourceStreamPath = "Elfie.NonCore.Test.Elfie.Search.SampleDefinitions.cs";
        private const string s_sampleDefinitionPath = "SampleDefinitions.cs";

        private string GetSampleDefinitionPath()
        {
            // Extract the sample content as an embedded resource and write it to the current directory
            if (!File.Exists(s_sampleDefinitionPath))
            {
                Assembly thisAssembly = Assembly.GetExecutingAssembly();
                foreach (string name in thisAssembly.GetManifestResourceNames())
                {
                    Trace.WriteLine(name);
                }
                using (Stream source = thisAssembly.GetManifestResourceStream(s_resourceStreamPath))
                {
                    using (Stream destination = File.Create(s_sampleDefinitionPath))
                    {
                        source.CopyTo(destination);
                    }
                }
            }

            return s_sampleDefinitionPath;
        }

        private string _sampleDefinitionsContent;

        public RoslynDefinitionFinderTests()
        {
            _sampleDefinitionsContent = File.ReadAllText(GetSampleDefinitionPath());
        }

        [TestMethod]
        public void RoslynDefinitionFinder_Basic()
        {
            // Add references in the MSTest directory itself
            List<string> referencePaths = new List<string>();
            referencePaths.Add(typeof(object).Assembly.Location);
            referencePaths.AddRange(Directory.GetFiles(".", "*.dll"));
            referencePaths.AddRange(Directory.GetFiles(".", "*.exe"));

            RoslynReferencesWrapper references = new RoslynReferencesWrapper(referencePaths);
            RoslynDefinitionFinder finder = new RoslynDefinitionFinder(GetSampleDefinitionPath(), references);

            // Return Type
            FindAndVerifyReference("private static RsDs|Signature GetAssemblyDebugSignature(string binaryFilePath)", s_rsDsSignatureNamespace + ".RsDsSignature", finder);

            // Method Argument Type
            FindAndVerifyReference("private static RsDsSignature GetAssemblyDebugSignature(st|ring binaryFilePath)", "System.String", finder);

            // var declaration
            FindAndVerifyReference("va|r stream = new FileStream(binaryFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)", "System.IO.FileStream", finder);

            // Constructor [on argument list]
            FindAndVerifyReference("var stream = new FileStream|(binaryFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)", "System.IO.FileStream.FileStream(string, FileMode, FileAccess, FileShare)", finder);

            // Constructor [on type name]
            FindAndVerifyReference("var stream = new FileStr|eam(binaryFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)", "System.IO.FileStream.FileStream(string, FileMode, FileAccess, FileShare)", finder);

            // Enum type
            FindAndVerifyReference("var stream = new FileStream(binaryFilePath, File|Mode.Open, FileAccess.Read, FileShare.ReadWrite)", "System.IO.FileMode", finder);

            // Enum item
            FindAndVerifyReference("var stream = new FileStream(binaryFilePath, FileMode.Op|en, FileAccess.Read, FileShare.ReadWrite)", "System.IO.FileMode.Open", finder);

            // Static Type in static call
            FindAndVerifyReference("return |Assembly.ReadRsDsSignature(stream);", s_assemblyNamespace + ".Assembly", finder);

            // Static Method in call
            FindAndVerifyReference("return Assembly.R|eadRsDsSignature(stream);", s_assemblyNamespace + ".Assembly.ReadRsDsSignature(Stream)", finder);

            // Instance type declaration
            FindAndVerifyReference("R|sDsSignature signature", s_rsDsSignatureNamespace + ".RsDsSignature", finder);

            // Instance call
            FindAndVerifyReference("signature.ToS|tring()", s_rsDsSignatureNamespace + ".RsDsSignature.ToString", finder);

            // Instance property call
            FindAndVerifyReference("signature.Gu|id.ToString()", s_rsDsSignatureNamespace + ".RsDsSignature.Guid", finder);

            // Instance field reference
            FindAndVerifyReference("Path.Combine(_ca|chePath,", s_sampleDefinitionsNamespace + ".SampleDefinitions._cachePath", finder);

            // Statement [not a method with a return value]
            FindAndVerifyReference("SetCache|Path(Environment.ExpandEnvironmentVariables(", s_sampleDefinitionsNamespace + ".SampleDefinitions.SetCachePath(string)", finder);
        }

        public void FindAndVerifyReference(string searchTextWithCursor, string expectedFullyQualifiedName, RoslynDefinitionFinder finder)
        {
            string foundFullyQualifiedName = "";

            int lineNumber, charInLine;
            if (TryFindTextWithCursor(searchTextWithCursor, out lineNumber, out charInLine))
            {
                MemberQuery query = finder.BuildQueryForMemberUsedAt(lineNumber, charInLine);
                if (query != null)
                {
                    foundFullyQualifiedName = query.SymbolName;
                    if (!string.IsNullOrEmpty(query.Parameters))
                    {
                        foundFullyQualifiedName += "(" + query.Parameters + ")";
                    }
                }
            }

            Assert.AreEqual(expectedFullyQualifiedName, foundFullyQualifiedName);
        }

        private bool TryFindTextWithCursor(string searchTextWithCursor, out int lineNumber, out int charInLine)
        {
            lineNumber = 0;
            charInLine = 0;

            // Remove the '|' marking the cursor from the search text and track where it is relative to the search string
            int cursorRelativePosition = searchTextWithCursor.IndexOf('|');
            if (cursorRelativePosition == -1) return false;

            string rawSearchText = searchTextWithCursor.Remove(cursorRelativePosition, 1);

            // Find the cursor position within the file text
            int searchTextPosition = _sampleDefinitionsContent.IndexOf(rawSearchText);
            if (searchTextPosition == -1) return false;
            int cursorAbsolutePosition = searchTextPosition + cursorRelativePosition;

            // Translate to a line and character
            int line = 1;
            int lineStartIndex = 0;

            for (int i = 0; i < cursorAbsolutePosition; ++i)
            {
                if (_sampleDefinitionsContent[i] == '\n')
                {
                    line++;
                    lineStartIndex = i + 1;
                }
            }

            lineNumber = line;
            charInLine = cursorAbsolutePosition - lineStartIndex + 1;
            return true;
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Elfie.Search
{
    /// <summary>
    ///  Wrapper which contains a cached set of references in a Compilation
    ///  without exposing Roslyn types which the caller must reference.
    /// </summary>
    public class RoslynReferencesWrapper
    {
        private IEnumerable<string> ReferencePaths { get; set; }

        private CSharpCompilation _cachedCSharpCompilation;
        private VisualBasicCompilation _cachedVisualBasicCompilation;

        public RoslynReferencesWrapper() { }

        public RoslynReferencesWrapper(IEnumerable<string> referencePaths)
        {
            this.ReferencePaths = referencePaths;
        }

        private IEnumerable<MetadataReference> BuildReferences()
        {
            // Load all references
            List<MetadataReference> references = new List<MetadataReference>();

            if (this.ReferencePaths != null)
            {
                foreach (string binaryPath in this.ReferencePaths)
                {
                    if (binaryPath.Contains(".vshost.")) continue;
                    references.Add(MetadataReference.CreateFromFile(binaryPath));
                }
            }

            return references;
        }

        internal Compilation GetCompilationFor(SyntaxTree tree)
        {
            if (tree is CSharpSyntaxTree)
            {
                return this.CSharpCompilation;
            }
            else if (tree is VisualBasicSyntaxTree)
            {
                return this.VisualBasicCompilation;
            }
            else
            {
                throw new NotImplementedException(String.Format("RoslynReferencesWrapper doesn't know how to create Compilation type needed for SyntaxTree of type '{0}'.", tree.GetType().Name));
            }
        }

        internal CSharpCompilation CSharpCompilation
        {
            get
            {
                if (_cachedCSharpCompilation == null)
                {
                    // Ask for all members (not just publics) to be loaded
                    CSharpCompilationOptions compilationOptions = new CSharpCompilationOptions(
                        outputKind: OutputKind.ConsoleApplication,
                        reportSuppressedDiagnostics: false);

                    compilationOptions.SetMetadataImportOptions(MetadataImportOptions.All);

                    // Build a compilation to return
                    _cachedCSharpCompilation = CSharpCompilation.Create("Sample", references: BuildReferences(), options: compilationOptions);
                }

                return _cachedCSharpCompilation;
            }
        }

        internal VisualBasicCompilation VisualBasicCompilation
        {
            get
            {
                if (_cachedVisualBasicCompilation == null)
                {
                    // Ask for all members (not just publics) to be loaded
                    VisualBasicCompilationOptions compilationOptions = new VisualBasicCompilationOptions(OutputKind.ConsoleApplication);

                    compilationOptions.SetMetadataImportOptions(MetadataImportOptions.All);

                    // Build a compilation to return
                    _cachedVisualBasicCompilation = VisualBasicCompilation.Create("Sample", references: BuildReferences(), options: compilationOptions);
                }

                return _cachedVisualBasicCompilation;
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Indexer.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.PDB;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers
{
    /// <summary>
    ///  RoslynSyntaxLocationExpander finds additional member locations and improves found locations from a metadata/PDB crawl.
    ///  It does a syntax parse on the file found for each type which had at least one location.
    ///  It updates member locations to the syntax location, if the member signature was unique.
    /// </summary>
    public class RoslynSyntaxLocationExpander
    {
        private Dictionary<string, Compilation> _fileCompilations;

        public RoslynSyntaxLocationExpander()
        {
            _fileCompilations = new Dictionary<string, Compilation>(StringComparer.OrdinalIgnoreCase);
        }

        public void AddLocations(PackageDatabase db)
        {
            // TODO: Should avoid keeping all Roslyn documents around, but doesn't - need to understand why.

            // Process each assembly separately (can't create C# and VB merged compilation)
            Symbol assembly = db.QueryRoot.FirstChild();
            while (assembly.IsValid)
            {
                AddLocationsUsingSiblingLocations(db, assembly);
                assembly = assembly.NextSibling();
            }
        }

        public void AddLocationsUsingSiblingLocations(PackageDatabase db, Symbol assemblyRoot)
        {
            // Clear cached compilations between binaries (memory use)
            _fileCompilations = new Dictionary<string, Compilation>(StringComparer.OrdinalIgnoreCase);

            assemblyRoot.Walk((symbol) =>
            {
                if (symbol.Type == SymbolType.Class || symbol.Type == SymbolType.Struct) AddTypeLocations(db, symbol);
            });
        }

        public void AddLocationsUsingSingleCompilation(PackageDatabase db, Symbol assemblyRoot)
        {
            List<Symbol> typesToResolve = new List<Symbol>();
            HashSet<string> uniqueSourceFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Find all types without locations and all unique source files resolved
            assemblyRoot.Walk((symbol) =>
            {
                string sourceFilePath = symbol.FilePath.ToString();

                if (String.IsNullOrEmpty(sourceFilePath))
                {
                    // Add types with no file path to the list to resolve
                    if (symbol.Type == SymbolType.Class || symbol.Type == SymbolType.Struct)
                    {
                        typesToResolve.Add(symbol);
                    }
                }
                else
                {
                    // Add all found file paths to the list of sources
                    uniqueSourceFilePaths.Add(sourceFilePath);
                }
            });

            // If no locations were resolved, we won't be able to improve anything
            if (uniqueSourceFilePaths.Count == 0) return;

            // Parse everything and build a combined compilation
            string[] sourceFilePaths = new string[uniqueSourceFilePaths.Count];
            uniqueSourceFilePaths.CopyTo(sourceFilePaths);
            Compilation compilation = CreateCompilation(sourceFilePaths, assemblyRoot.AssemblyNameWithoutExtension + ".pdb");

            if (compilation == null) return;

            // Resolve all of the types without locations (and their members)
            for (int i = 0; i < typesToResolve.Count; ++i)
            {
                AddTypeLocations(db, typesToResolve[i], compilation);
            }
        }

        public void AddTypeLocations(IMemberDatabase db, Symbol current)
        {
            // If the type already has a location, it's already a source parse or fixed
            if (current.HasLocation) return;

            // Try to find a file path for any member within the type
            HashSet<string> typeFilePaths = new HashSet<string>();
            current.Walk((c) =>
            {
                if (c.HasLocation) typeFilePaths.Add(c.FilePath.ToString());
            });

            if (typeFilePaths.Count == 0) return;

            Compilation compilation = CreateCompilation(typeFilePaths.ToList(), current.AssemblyNameWithoutExtension + ".pdb");
            if (compilation == null) return;

            AddTypeLocations(db, current, compilation);
        }

        public void AddTypeLocations(IMemberDatabase db, Symbol current, Compilation compilation)
        {
            // Try to find the type itself
            INamedTypeSymbol typeSymbol = FindTypeSymbol(compilation, current.ContainerName.ToString(), current.Name.ToString());
            if (typeSymbol == null) return;

            // If found, add the type location
            AddLocation(db, current, typeSymbol);

            // Try to find member locations
            Symbol typeMember = current.FirstChild();
            while (typeMember.IsValid)
            {
                ISymbol memberSymbol = FindMember(typeSymbol, typeMember.Name.ToString(), typeMember.Parameters.ToString());
                if (memberSymbol != null) AddLocation(db, typeMember, memberSymbol);

                typeMember = typeMember.NextSibling();
            }
        }

        public Compilation CreateCompilation(IList<string> filePaths, string pdbPath)
        {
            SyntaxTree[] trees = new SyntaxTree[filePaths.Count];
            Parallel.For(0, filePaths.Count, (i) =>
            {
                trees[i] = ParseFile(filePaths[i], pdbPath);
            });

            IEnumerable<SyntaxTree> validTrees = trees.Where((t) => t != null);

            SyntaxTree first = validTrees.FirstOrDefault();
            if (first == null)
            {
                return null;
            }

            if (first is CSharpSyntaxTree)
            {
                return CSharpCompilation.Create("Sample", validTrees);
            }
            else if (first is VisualBasicSyntaxTree)
            {
                return VisualBasicCompilation.Create("Sample", validTrees);
            }
            else
            {
                return null;
            }
        }

        public Compilation CreateCompilation(string filePath, string pdbPath)
        {
            Compilation result;
            if (_fileCompilations.TryGetValue(filePath, out result)) return result;

            SyntaxTree tree = ParseFile(filePath, pdbPath);
            if (tree == null)
            {
                result = null;
            }
            else if (tree is CSharpSyntaxTree)
            {
                result = CSharpCompilation.Create("Sample").AddSyntaxTrees(tree);
            }
            else if (tree is VisualBasicSyntaxTree)
            {
                result = VisualBasicCompilation.Create("Sample").AddSyntaxTrees(tree);
            }
            else
            {
                result = null;
            }

            _fileCompilations[filePath] = result;
            return result;
        }

        public SyntaxTree ParseFile(string filePath, string pdbPath)
        {
            // Determine the local path to the file (if it's a URL)
            string localPath = filePath;
            if (filePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                localPath = SourceFileMap.ComputeCachedPath(pdbPath, filePath);
            }

            if (!File.Exists(localPath)) return null;

            // Much like Roslyn CSharpCompiler.CreateCompilation
            SourceText fileContent = null;
            using (var stream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1, options: FileOptions.None))
            {
                fileContent = SourceText.From(stream);
            }

            // Parse the file but keep the original path or URL as the path (so locations from Roslyn point to it)
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".cs":
                    return CSharpSyntaxTree.ParseText(fileContent, path: filePath);
                case ".vb":
                    return VisualBasicSyntaxTree.ParseText(fileContent, path: filePath);
                default:
                    return null;
            }
        }

        public INamedTypeSymbol FindTypeSymbol(Compilation compilation, string namespaceName, string typeName)
        {
            foreach (ISymbol symbol in compilation.GetSymbolsWithName((name) => name.Equals(typeName), SymbolFilter.Type))
            {
                INamedTypeSymbol typeSymbol = symbol as INamedTypeSymbol;
                if (typeSymbol != null || typeSymbol.ContainingNamespace.Name.Equals(namespaceName))
                {
                    return typeSymbol;
                }
            }

            return null;
        }

        public ISymbol FindMember(Compilation compilation, string namespaceName, string typeName, string memberName, string parameters)
        {
            INamedTypeSymbol typeSymbol = FindTypeSymbol(compilation, namespaceName, typeName);
            if (typeSymbol == null) return null;

            return FindMember(typeSymbol, memberName, parameters);
        }

        public ISymbol FindMember(INamedTypeSymbol typeSymbol, string memberName, string parameters)
        {
            ISymbol lastMatch = null;

            foreach (ISymbol member in typeSymbol.GetMembers())
            {
                string adjustedName = member.AdjustedName();
                if (String.Equals(memberName, adjustedName))
                {
                    string foundParameters = member.MinimalParameters();
                    if (String.Equals(parameters, foundParameters))
                    {
                        // If there is more than one match with the same name and signature, don't return anything (only return if we're sure)
                        if (lastMatch != null) return null;

                        lastMatch = member;
                    }
                }
            }

            return lastMatch;
        }

        private void AddLocation(IMemberDatabase db, Symbol current, ISymbol roslynSymbol)
        {
            // Get MutableSymbol declaration location from Roslyn, if available
            if (roslynSymbol.Locations.Length != 0)
            {
                if (roslynSymbol.Locations[0].IsInSource)
                {
                    // Roslyn locations are zero-based. Correct to normal positions.
                    FileLinePositionSpan location = roslynSymbol.Locations[0].GetLineSpan();
                    db.SetLocation(
                        current.Index,
                        location.Path,
                        (location.StartLinePosition.Line + 1).TrimToUShort(),
                        (location.StartLinePosition.Character + 1).TrimToUShort());
                }
            }
        }
    }
}

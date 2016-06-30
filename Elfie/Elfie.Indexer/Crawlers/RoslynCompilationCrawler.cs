// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Web;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Indexer.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers
{
    /// <summary>
    ///  RoslynCompilationCrawler generates a PackageDatabase for a binary, project, or solution
    ///  using the Roslyn semantic model found by getting the Compilation.GlobalNamespace and
    ///  then traversing it. When used on binary paths this is driven by the assembly metadata only.
    /// 
    ///  This is how NuGet.PackageIndex crawls.
    /// </summary>
    public class RoslynCompilationCrawler : ICrawler
    {
        public bool IncludeSignatures { get; set; }

        public bool IncludeMembers { get; set; }

        public bool IncludeNonPublicMembers { get; set; }

        public bool IncludeCodeLocations { get; set; }

        public bool IncludeFrameworkTargets { get; set; }

        private PdbSymbolProvider PDB { get; set; }

        public RoslynCompilationCrawler()
        {
            this.IncludeSignatures = true;
            this.IncludeMembers = true;
            this.IncludeNonPublicMembers = true;
            this.IncludeCodeLocations = true;
            this.IncludeFrameworkTargets = false;
        }

        internal static MSBuildWorkspace BuildWorkspace()
        {
            // Tell Roslyn to handle stub assemblies correctly (SourceBrowser
            ImmutableDictionary<string, string> workspaceOptions = ImmutableDictionary<string, string>.Empty;
            workspaceOptions = workspaceOptions.Add("CheckForSystemRuntimeDependency", "true");
            workspaceOptions = workspaceOptions.Add("VisualStudioVersion", "14.0");

            // Construct the base workspace
            MSBuildWorkspace workspace = MSBuildWorkspace.Create(workspaceOptions);

            // Tell Roslyn to load metadata for references so that it can resolve them
            workspace.LoadMetadataForReferencedProjects = true;

            return workspace;
        }

        public void Walk(string walkPath, MutableSymbol parent)
        {
            string encodedFrameworkNames = null;
            string[] walkPathTokens = walkPath.Split('\t');

            Debug.Assert(walkPathTokens.Length == 1 || walkPathTokens.Length == 2);

            walkPath = walkPathTokens[0];
            if (walkPathTokens.Length > 1)
            {
                encodedFrameworkNames = walkPathTokens[1];
            }

            // Index the directory|file list|solution|project|binary
            string extension = Path.GetExtension(walkPath).ToLowerInvariant();
            if (FileIO.IsManagedBinary(walkPath))
            {
                WalkBinary(walkPath, parent, encodedFrameworkNames);
            }
            else if (extension.Equals(".sln"))
            {
                WalkSolution(walkPath, parent);
            }
            else if (extension.EndsWith("proj"))
            {
                WalkProject(walkPath, parent);
            }
            else
            {
                throw new ArgumentException(String.Format("RoslynCompilationCrawler doesn't know how to walk item with extension '{0}'", extension));
            }
        }

        private void WalkProject(string projectPath, MutableSymbol parent)
        {
            using (MSBuildWorkspace workspace = BuildWorkspace())
            {
                // Open Project with Roslyn
                var project = workspace.OpenProjectAsync(projectPath).Result;
                WalkProject(project, parent);
            }
        }

        private void WalkSolution(string solutionPath, MutableSymbol parent)
        {
            using (MSBuildWorkspace workspace = BuildWorkspace())
            {
                // Open Solution with Roslyn
                var solution = workspace.OpenSolutionAsync(solutionPath).Result;

                // Build assembly symbols for each project in a shared tree
                foreach (Project project in solution.Projects)
                {
                    WalkProject(project, parent);
                }
            }
        }

        private void WalkProject(Project project, MutableSymbol parent)
        {
            // Get the consolidated assembly symbols and walk them
            Compilation rootCompilation = project.GetCompilationAsync().Result;
            IAssemblySymbol assembly = rootCompilation.Assembly;

            MutableSymbol assemblyRoot = parent.AddChild(new MutableSymbol(Path.GetFileName(project.OutputFilePath), SymbolType.Assembly));
            WalkNamespace(assembly.GlobalNamespace, assemblyRoot);
        }

        private void WalkBinary(string binaryPath, MutableSymbol parent, string encodedFrameworkNames)
        {
            // Add the binary as a reference to resolve symbols in it
            MetadataReference reference = MetadataReference.CreateFromFile(binaryPath);

            CSharpCompilationOptions compilationOptions = new CSharpCompilationOptions(
                outputKind: OutputKind.ConsoleApplication,
                reportSuppressedDiagnostics: false);

            compilationOptions.SetMetadataImportOptions(MetadataImportOptions.All);

            // Create an empty binary to 'host' the reference
            CSharpCompilation emptyCompilation = CSharpCompilation.Create("Empty.exe", references: new[] { reference }, options: compilationOptions);

            // Get the root of the reference specifically
            ISymbol referenceRootSymbol = emptyCompilation.GetAssemblyOrModuleSymbol(reference);

            // If this wasn't a managed assembly, don't add anything
            if (referenceRootSymbol == null) return;

            string assemblyName = null;
            INamespaceSymbol globalNamespace = null;

            if (referenceRootSymbol is IAssemblySymbol)
            {
                // NOTE: Use the Assembly.Identity.Name specifically as the root to allow VS to identify binaries (potentially) already referenced safely.
                assemblyName = ((IAssemblySymbol)referenceRootSymbol).Identity.Name;
                globalNamespace = ((IAssemblySymbol)referenceRootSymbol).GlobalNamespace;
            }
            else if (referenceRootSymbol is IModuleSymbol)
            {
                assemblyName = Path.GetFileName(binaryPath);
                globalNamespace = ((IModuleSymbol)referenceRootSymbol).GlobalNamespace;
            }
            else
            {
                // Unable to crawl if we didn't find an assembly or module
                Trace.WriteLine(String.Format("ERROR: Unable to crawl binary with root symbol type '{0}'", referenceRootSymbol.GetType().Name));
                return;
            }

            // Walk the binary
            MutableSymbol addUnderRoot = parent.AddChild(new MutableSymbol(assemblyName, SymbolType.Assembly));

            // Add the target framework [if requested and identifiable]
            if (this.IncludeFrameworkTargets)
            {
                if (!String.IsNullOrEmpty(encodedFrameworkNames))
                {
                    addUnderRoot = addUnderRoot.AddChild(new MutableSymbol(encodedFrameworkNames, SymbolType.FrameworkTarget));
                }
            }

            // PRIVATE ROSLYN: Attempt to build a PDB reader for the binary
            using (PdbSymbolProvider pdbProvider = PdbSymbolProvider.TryBuildProvider(binaryPath))
            {
                PDB = pdbProvider;

                WalkNamespace(globalNamespace, addUnderRoot);

                // Remove the PdbSymbolProvider
                PDB = null;
            }
        }

        private void WalkNamespace(INamespaceSymbol ns, MutableSymbol parent)
        {
            // Build a MutableSymbol for this namespace; collapse the global namespace right under the binary
            MutableSymbol thisSymbol = parent;
            if (!ns.IsGlobalNamespace)
            {
                thisSymbol = parent.AddChild(new MutableSymbol(ns.Name, SymbolType.Namespace));
            }

            // Enumerate child namespaces
            foreach (INamespaceSymbol childNamespace in ns.GetNamespaceMembers())
            {
                WalkNamespace(childNamespace, thisSymbol);
            }

            // Enumerate types
            foreach (INamedTypeSymbol childType in ns.GetTypeMembers())
            {
                WalkType(childType, thisSymbol);
            }
        }

        private void WalkType(INamedTypeSymbol symbol, MutableSymbol parent)
        {
            // Build this type
            MutableSymbol result = new MutableSymbol(symbol.Name, GetNamedTypeType(symbol));
            AddModifiers(symbol, result);
            AddLocation(symbol, result);

            // Stop if it should be excluded
            if (IsExcluded(symbol, result)) return;

            // Add the type itself
            MutableSymbol thisSymbol = parent.AddChild(result);

            // Recurse on members
            foreach (ISymbol child in symbol.GetMembers())
            {
                if (child is INamedTypeSymbol)
                {
                    WalkType((INamedTypeSymbol)child, thisSymbol);
                }
                else
                {
                    if (this.IncludeMembers)
                    {
                        if (child is IMethodSymbol)
                        {
                            WalkMethod((IMethodSymbol)child, thisSymbol);
                        }
                        else if (child is IPropertySymbol)
                        {
                            WalkProperty((IPropertySymbol)child, thisSymbol);
                        }
                        else if (child is IFieldSymbol)
                        {
                            WalkField((IFieldSymbol)child, thisSymbol);
                        }
                        else
                        {
                            // Other contents excluded
                        }
                    }
                }
            }
        }

        private void WalkMethod(IMethodSymbol method, MutableSymbol parent)
        {
            MutableSymbol result = new MutableSymbol(method.AdjustedName(), GetMethodType(method));
            AddModifiers(method, result);
            AddLocation(method, result);

            if (this.IncludeSignatures)
            {
                result.Parameters = method.MinimalParameters();
            }

            if (IsExcluded(method, result)) return;

            parent.AddChild(result);

            // Add the extended type under Extension Methods
            if (result.Type == SymbolType.ExtensionMethod && method.Parameters.Length > 0)
            {
                IParameterSymbol thisParameter = method.Parameters[0];
                ITypeSymbol thisType = thisParameter.Type;
                MutableSymbol extendedType = new MutableSymbol(thisType.NamespaceAndName(), SymbolType.ExtendedType);
                extendedType.Modifiers = result.Modifiers;
                result.AddChild(extendedType);
            }
        }

        private void WalkField(IFieldSymbol field, MutableSymbol parent)
        {
            MutableSymbol result = new MutableSymbol(field.Name, GetFieldType(field));
            AddModifiers(field, result);
            AddLocation(field, result);

            if (!IsExcluded(field, result)) parent.AddChild(result);
        }

        private void WalkProperty(IPropertySymbol property, MutableSymbol parent)
        {
            MutableSymbol result = new MutableSymbol(property.Name, GetPropertyType(property));
            AddModifiers(property, result);
            AddLocation(property, result);

            if (this.IncludeSignatures)
            {
                result.Parameters = property.MinimalParameters();
            }

            if (!IsExcluded(property, result)) parent.AddChild(result);
        }

        private void AddModifiers(ISymbol MutableSymbol, MutableSymbol result)
        {
            SymbolModifier modifiers = SymbolModifier.None;

            // Convert individual properties
            if (MutableSymbol.IsStatic) modifiers |= SymbolModifier.Static;

            // Not needed for scenarios.
            //if (MutableSymbol.IsAbstract) modifiers |= SymbolModifier.Abstract;
            //if (MutableSymbol.IsExtern) modifiers |= SymbolModifier.Extern;
            //if (MutableSymbol.IsOverride) modifiers |= SymbolModifier.Override;
            //if (MutableSymbol.IsSealed) modifiers |= SymbolModifier.Sealed;
            //if (MutableSymbol.IsVirtual) modifiers |= SymbolModifier.Virtual;

            // Convert accessibility
            switch (MutableSymbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    modifiers |= SymbolModifier.Public;
                    break;
                case Accessibility.Protected:
                    modifiers |= SymbolModifier.Protected;
                    break;
                case Accessibility.Private:
                    modifiers |= SymbolModifier.Private;
                    break;
                case Accessibility.Internal:
                    modifiers |= SymbolModifier.Internal;
                    break;
                case Accessibility.ProtectedAndInternal:
                case Accessibility.ProtectedOrInternal:
                    modifiers |= SymbolModifier.Protected | SymbolModifier.Internal;
                    break;
                default:
                    throw new ArgumentException("Accessibility unhandled: " + MutableSymbol.DeclaredAccessibility.ToString());
            }

            result.Modifiers = modifiers;
        }

        private void AddLocation(ISymbol symbol, MutableSymbol symbolToAdd)
        {
            if (this.IncludeCodeLocations)
            {
                // Get MutableSymbol declaration location from Roslyn, if available
                if (symbol.Locations.Length != 0)
                {
                    if (symbol.Locations[0].IsInSource)
                    {
                        // Roslyn locations are zero-based. Correct to normal positions.
                        FileLinePositionSpan location = symbol.Locations[0].GetLineSpan();
                        symbolToAdd.FilePath = location.Path;
                        symbolToAdd.Line = (location.StartLinePosition.Line + 1).TrimToUShort();
                        symbolToAdd.CharInLine = (location.StartLinePosition.Character + 1).TrimToUShort();
                        return;
                    }
                }

                // Get MutableSymbol declaration location from the PDB, if available
                if (PDB != null)
                {
                    MethodDefinitionHandle handle = symbol.GetMethodDefinitionHandle();
                    if (!handle.IsNil)
                    {
                        int token = MetadataTokens.GetToken(handle);

                        // If found, associate the member with the first location found
                        ILSequencePoint location;
                        if (PDB.TryGetFirstPointForMethod(token, out location))
                        {
                            symbolToAdd.FilePath = location.Document;
                            symbolToAdd.Line = location.StartLine.TrimToUShort();
                            symbolToAdd.CharInLine = location.StartCharInLine.TrimToUShort();
                        }
                    }
                }
            }
        }

        #region SymbolType Conversion
        private SymbolType GetMethodType(IMethodSymbol method)
        {
            switch (method.MethodKind)
            {
                case MethodKind.Constructor:
                    return SymbolType.Constructor;
                case MethodKind.StaticConstructor:
                    return SymbolType.StaticConstructor;
                case MethodKind.Destructor:
                    return SymbolType.Destructor;
                case MethodKind.Ordinary:
                case MethodKind.ExplicitInterfaceImplementation:
                    return (method.IsExtensionMethod ? SymbolType.ExtensionMethod : SymbolType.Method);
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                case MethodKind.EventAdd:
                case MethodKind.EventRaise:
                case MethodKind.EventRemove:
                    // Types I know I want to exclude
                    return SymbolType.Excluded;
                default:
                    // Types I don't expect to see during these traversals
                    return SymbolType.Excluded;
            }
        }

        private SymbolType GetFieldType(IFieldSymbol field)
        {
            return (field.CanBeReferencedByName ? SymbolType.Field : SymbolType.Excluded);
        }

        private SymbolType GetPropertyType(IPropertySymbol property)
        {
            return (property.IsIndexer ? SymbolType.Indexer : SymbolType.Property);
        }

        private SymbolType GetNamedTypeType(INamedTypeSymbol type)
        {
            // TypeKind is good for distinguishing interfaces but is not struct or enum for structs and enums
            if (type.TypeKind == TypeKind.Interface) return SymbolType.Interface;

            INamedTypeSymbol baseType = type.BaseType;

            if (baseType != null)
            {
                if (baseType.Name.Equals("Enum"))
                {
                    return SymbolType.Enum;
                }
                else if (baseType.Name.Equals("ValueType"))
                {
                    return SymbolType.Struct;
                }
            }

            return (type.IsValueType ? SymbolType.Struct : SymbolType.Class);
        }
        #endregion

        #region Per MutableSymbol Exclusion Control
        private bool IsExcluded(ISymbol MutableSymbol, MutableSymbol symbolToAdd)
        {
            //// Debugging: Uncomment to stop at MutableSymbol causing trouble
            //if (MutableSymbol.Name.EndsWith("BackingField"))
            //{
            //    Debugger.Break();
            //}

            // Exclude members we don't want indexed (backing fields, property get/set methods)
            if (symbolToAdd.Type == SymbolType.Excluded)
            {
                return true;
            }

            // Exclude types which are generated under the covers (IEnumerable worker classes)
            if (MutableSymbol.CanBeReferencedByName == false && MutableSymbol is INamedTypeSymbol)
            {
                return true;
            }

            // Exclude weird value__ fields appearing on enum types
            if (MutableSymbol.Name.Equals("value__"))
            {
                return true;
            }

            // Exclude constructors for enums
            if (symbolToAdd.Type == SymbolType.Constructor)
            {
                INamedTypeSymbol parentType = MutableSymbol.ContainingType.BaseType;
                if (parentType != null && parentType.Name.Equals("Enum"))
                {
                    return true;
                }
            }

            // Exclude non-public members *if configured to*
            if (this.IncludeNonPublicMembers == false)
            {
                if (!symbolToAdd.Modifiers.HasFlag(SymbolModifier.Public))
                {
                    return true;
                }
            }

            // Include everything else
            return false;
        }
        #endregion
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Elfie.Indexer.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Elfie.Search
{
    /// <summary>
    ///  RoslynDefinitionFinder implements F12 [Go To Definition]. It uses a
    ///  Compilation with all binaries in the output folder to enable resolving
    ///  symbol names even within dependency code. It creates Elfie queries to
    ///  find the declaration locations of the identified symbols, since metadata
    ///  references don't know the declaration locations.
    /// </summary>
    public class RoslynDefinitionFinder
    {
        private SyntaxTree SyntaxTree { get; set; }
        private SemanticModel SemanticModel { get; set; }
        private Compilation Compilation { get; set; }

        public RoslynDefinitionFinder(string sourceFilePath, RoslynReferencesWrapper wrapper)
        {
            if (wrapper == null) wrapper = new RoslynReferencesWrapper();

            this.SyntaxTree = ParseFile(sourceFilePath);
            this.Compilation = wrapper.GetCompilationFor(this.SyntaxTree);
            this.Compilation = this.Compilation.AddSyntaxTrees(this.SyntaxTree);
            this.SemanticModel = this.Compilation.GetSemanticModel(this.SyntaxTree, true);
        }

        private static SyntaxTree ParseFile(string sourceFilePath)
        {
            // Read the source file content
            SourceText fileContent = null;
            using (var stream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1, options: FileOptions.None))
            {
                fileContent = SourceText.From(stream);
            }

            // Build a SyntaxTree to return
            string extension = Path.GetExtension(sourceFilePath).ToLowerInvariant();
            switch (extension)
            {
                case ".cs":
                    return CSharpSyntaxTree.ParseText(fileContent, path: sourceFilePath);
                case ".vb":
                    return VisualBasicSyntaxTree.ParseText(fileContent, path: sourceFilePath);
                default:
                    throw new NotImplementedException(String.Format("Unable to build definition finder for unknown file extension of file '{0}'", sourceFilePath));
            }
        }

        public MemberQuery BuildQueryForMemberUsedAt(int lineNumber, int charInLine)
        {
            // Map the line and char to an absolute offset
            SourceText text = this.SyntaxTree.GetText();
            int absolutePosition = text.Lines[lineNumber - 1].Start + charInLine - 1;

            // Get the token and SyntaxNode at that position
            SyntaxNode root = this.SyntaxTree.GetRoot();
            SyntaxToken token = root.FindToken(absolutePosition, true);
            FileLinePositionSpan span = token.GetLocation().GetLineSpan();


            SyntaxNode node = token.Parent;

            // If this is the type name identifier in a constructor call [new FileStrea|m(...)], resolve to the constructor, not the type
            if (node is Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax)
            {
                if (node.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.ObjectCreationExpressionSyntax)
                {
                    node = node.Parent;
                }
            }
            else if (node is Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax)
            {
                if (node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.ObjectCreationExpressionSyntax)
                {
                    node = node.Parent;
                }
            }

            // Walk up looking for a node with a resolvable Symbol. If found, return a query for it
            while (node != null)
            {
                SymbolInfo info = this.SemanticModel.GetSymbolInfo(node);
                MemberQuery query = IdentifySymbol(info);
                if (query != null) return query;

                node = node.Parent;
            }

            return null;
        }

        private MemberQuery IdentifySymbol(SymbolInfo symbol)
        {
            // Try to get the Symbol successfully resolved to (if any)
            ISymbol resolvedSymbol = symbol.Symbol;

            // If the symbol wasn't uniquely resolved, get the first possible candidate
            if (resolvedSymbol == null && symbol.CandidateSymbols.Length > 0)
            {
                resolvedSymbol = symbol.CandidateSymbols[0];
            }

            // Construct a member query for the full namespace, name, and signature of the member referenced
            if (resolvedSymbol != null)
            {
                StringBuilder symbolName = new StringBuilder();

                symbolName.Append(resolvedSymbol.FullNamespace());

                if (resolvedSymbol.ContainingType != null)
                {
                    if (symbolName.Length > 0) symbolName.Append(".");
                    symbolName.Append(resolvedSymbol.ContainingType.AdjustedName());
                }

                if (symbolName.Length > 0) symbolName.Append(".");
                symbolName.Append(resolvedSymbol.AdjustedName());

                MemberQuery query = new MemberQuery(symbolName.ToString(), true, true);
                query.Parameters = resolvedSymbol.MinimalParameters();

                // Make query case sensitive, because we know we got exact casing from Roslyn
                query.IgnoreCase = false;

                return query;
            }

            return null;
        }
    }
}

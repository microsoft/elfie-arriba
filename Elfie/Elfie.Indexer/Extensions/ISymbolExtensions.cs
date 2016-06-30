// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Elfie.Indexer.Extensions
{
    public static class ISymbolExtensions
    {
        /// <summary>
        ///  Return the symbol name adjusted for search scenarios.
        ///  Currently this changes constructors, static constructors, and destructors
        ///  to use the type name as the name (like Ctrl+, does).
        /// </summary>
        /// <param name="symbol">ISymbol for which to return name</param>
        /// <returns>Name of symbol, adjusted for search</returns>
        public static string AdjustedName(this ISymbol symbol)
        {
            IMethodSymbol method = symbol as IMethodSymbol;

            if (method != null && (method.MethodKind == MethodKind.Constructor || method.MethodKind == MethodKind.StaticConstructor || method.MethodKind == MethodKind.Destructor))
            {
                return method.ContainingType.Name;
            }
            else
            {
                return symbol.Name;
            }
        }

        /// <summary>
        ///  Return the full namespace and name of a given symbol.
        /// </summary>
        /// <param name="symbol">ISymbol for which to get full name.</param>
        /// <returns>Full namespace and name of symbol [Elfie.Indexer.Extensions.ISymbolExtensions.NamespaceAndName]</returns>
        public static string NamespaceAndName(this ISymbol symbol)
        {
            StringBuilder result = new StringBuilder();
            BuildFullName(symbol, result);
            return result.ToString();
        }

        /// <summary>
        ///  Return the full namespace of a given symbol.
        /// </summary>
        /// <param name="symbol">ISymbol for which to get full name.</param>
        /// <returns>Full namespace and name of symbol [Elfie.Indexer.Extensions.ISymbolExtensions]</returns>
        public static string FullNamespace(this ISymbol symbol)
        {
            StringBuilder result = new StringBuilder();
            BuildFullName(symbol.ContainingNamespace, result);
            return result.ToString();
        }

        private static void BuildFullName(ISymbol symbol, StringBuilder result)
        {
            // Add prefixes first
            INamespaceSymbol container = symbol.ContainingNamespace;
            if (container != null) BuildFullName(container, result);

            // Add a '.' and this name
            if (result.Length > 0) result.Append(".");
            result.Append(symbol.Name);
        }

        /// <summary>
        ///  Return Parameters in MinimallyQualifiedFormat for the symbol, if 
        ///  it is a symbol type which has parameters.
        /// </summary>
        /// <param name="symbol">ISymbol to examine</param>
        /// <returns>Comma delimited parameter types or String.Empty if no parameters</returns>
        public static string MinimalParameters(this ISymbol symbol)
        {
            if (symbol is IMethodSymbol)
            {
                return MinimalParameters(((IMethodSymbol)symbol).Parameters);
            }
            else if (symbol is IPropertySymbol)
            {
                return MinimalParameters(((IPropertySymbol)symbol).Parameters);
            }
            else
            {
                return String.Empty;
            }
        }

        private static string MinimalParameters(ImmutableArray<IParameterSymbol> parameters)
        {
            if (parameters == null || parameters.Length == 0) return String.Empty;

            StringBuilder parameterString = new StringBuilder();
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (i > 0) parameterString.Append(", ");
                parameterString.Append(parameters[i].Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            }

            return parameterString.ToString();
        }
    }
}

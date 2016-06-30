// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers
{
    /// <summary>
    /// Specifies what symbols to import from metadata.
    /// </summary>
    public enum MetadataImportOptions : byte
    {
        /// <summary>
        /// Only import public and protected symbols.
        /// </summary>
        Public = 0,

        /// <summary>
        /// Import public, protected and internal symbols.
        /// </summary>
        Internal = 1,

        /// <summary>
        /// Import all symbols.
        /// </summary>
        All = 2,
    }

    public static class RoslynReflectionWorkarounds
    {
        // Temporary workaround while blocked on https://github.com/dotnet/roslyn/issues/6748
        // This method is only called once. No fancy tricks required for speed.
        public static void SetMetadataImportOptions(this CompilationOptions instance, MetadataImportOptions options)
        {
            FieldInfo field = typeof(CompilationOptions)
                              .GetField(
                                  "<MetadataImportOptions>k__BackingField",
                                   BindingFlags.NonPublic | BindingFlags.Instance);

            field.SetValue(instance, options);
        }

        // Temporary workaround while blocked on https://github.com/dotnet/roslyn/issues/6749
        // This method is called a lot so we invoke through a cached delegate to LCG shim
        public static MethodDefinitionHandle GetMethodDefinitionHandle(this ISymbol symbol)
        {
            if (symbol.GetType() != s_peMethodSymbolType)
            {
                return default(MethodDefinitionHandle);
            }

            return s_getMethodDefinitionHandle(symbol);
        }

        private static readonly Type s_peMethodSymbolType =
            typeof(CSharpCompilation).Assembly.GetType("Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.PEMethodSymbol");

        private static readonly Func<ISymbol, MethodDefinitionHandle> s_getMethodDefinitionHandle =
            CreateDelegateToGetMethodDefinitionHandle();

        private static Func<ISymbol, MethodDefinitionHandle> CreateDelegateToGetMethodDefinitionHandle()
        {
            var method = new DynamicMethod(
                name: "GetMethodDefinitionHandle",
                returnType: typeof(MethodDefinitionHandle),
                parameterTypes: new[] { typeof(ISymbol) },
                owner: typeof(RoslynReflectionWorkarounds),
                skipVisibility: true);

            var getter = s_peMethodSymbolType.GetMethod("get_Handle", BindingFlags.NonPublic | BindingFlags.Instance);
            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, s_peMethodSymbolType);
            il.Emit(OpCodes.Call, getter);
            il.Emit(OpCodes.Ret);

            return (Func<ISymbol, MethodDefinitionHandle>)method.CreateDelegate(
                typeof(Func<ISymbol, MethodDefinitionHandle>));
        }
    }
}

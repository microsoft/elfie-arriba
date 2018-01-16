// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using XForm.IO;
using XForm.IO.StreamProvider;

namespace XForm.Types
{
    public static class TypeProviderFactory
    {
        private static Dictionary<string, ITypeProvider> s_providersByName;
        private static Dictionary<Type, ITypeProvider> s_providersByType;

        public static ITypeProvider Get(string typeName)
        {
            ITypeProvider provider = TryGet(typeName);
            if (provider == null) throw new ArgumentException($"XForm doesn't have a registered Type Provider for type {typeName}.");
            return provider;
        }

        public static ITypeProvider TryGet(string typeName)
        {
            EnsureLoaded();

            ITypeProvider provider;
            if (!s_providersByName.TryGetValue(typeName, out provider)) provider = null;
            return provider;
        }

        public static ITypeProvider Get(Type type)
        {
            ITypeProvider provider = TryGet(type);
            if (provider == null) throw new ArgumentException($"XForm doesn't have a registered Type Provider for type {type.Name}.");
            return provider;
        }

        public static ITypeProvider TryGet(Type type)
        {
            EnsureLoaded();

            ITypeProvider provider;
            if (!s_providersByType.TryGetValue(type, out provider)) provider = null;
            return provider;
        }

        public static IColumnReader TryGetColumn(Type type, IStreamProvider streamProvider, string columnPath)
        {
            if (type == typeof(String8))
            {
                // Don't cache String8Columns whole; the inner int[] and byte[] are cached. This avoids String8 overhead in the cache.
                return new NullableReader(streamProvider, columnPath, Get(type).BinaryReader(streamProvider, columnPath));
            }
            else
            {
                return ColumnCache.Instance.GetOrBuild(columnPath, () =>
                {
                    IColumnReader column = Get(type).BinaryReader(streamProvider, columnPath);
                    column = new NullableReader(streamProvider, columnPath, column);
                    return column;
                });
            }
        }

        public static IEnumerable<string> SupportedTypes
        {
            get
            {
                EnsureLoaded();
                return s_providersByName.Keys;
            }
        }

        private static void EnsureLoaded()
        {
            if (s_providersByName != null) return;

            // Initialize lookup Dictionaries
            s_providersByName = new Dictionary<string, ITypeProvider>(StringComparer.OrdinalIgnoreCase);
            s_providersByType = new Dictionary<Type, ITypeProvider>();

            // Add providers for primitive types
            Add(new PrimitiveTypeProvider<bool>());

            Add(new PrimitiveTypeProvider<sbyte>());
            Add(new PrimitiveTypeProvider<short>());
            Add(new PrimitiveTypeProvider<ushort>());
            Add(new PrimitiveTypeProvider<int>());
            Add(new PrimitiveTypeProvider<uint>());
            Add(new PrimitiveTypeProvider<long>());
            Add(new PrimitiveTypeProvider<ulong>());
            Add(new PrimitiveTypeProvider<float>());
            Add(new PrimitiveTypeProvider<double>());

            // Add configured type providers
            foreach (ITypeProvider provider in InterfaceLoader.BuildAll<ITypeProvider>())
            {
                Add(provider);
            }
        }

        private static void Add(ITypeProvider provider)
        {
            s_providersByName[provider.Name] = provider;
            s_providersByType[provider.Type] = provider;
        }
    }
}

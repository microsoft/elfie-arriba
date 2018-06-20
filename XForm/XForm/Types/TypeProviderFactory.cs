// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
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

        public static IColumnReader TryGetColumnReader(IStreamProvider streamProvider, Type columnType, string columnPath, CachingOption option = CachingOption.AsConfigured, Type callingType = null)
        {
            // IColumnReaders are nested within each other. Each layer uses this factory method, which uses callingType to return the correct next layer down.
            //  EnumColumnReader -> NullableReader -> PrimitiveReader
            //  EnumColumnReader -> NullableReader -> String8Reader -> PrimitiveReader [byte and position]

            if (callingType == null)
            {
                return EnumReader.Wrap(streamProvider, columnType, columnPath, option);
            }
            else if (callingType == typeof(EnumReader))
            {
                return NullableReader.Wrap(streamProvider, columnType, columnPath, option);
            }
            else // typeof(NullableReader) || typeof(String8ColumnReader) || typeof(VariableIntegerReader)
            {
                return Get(columnType).BinaryReader(streamProvider, columnPath, option);
            }
        }

        public static IColumnReader GetColumnReader(IStreamProvider streamProvider, Type columnType, string columnPath, CachingOption option = CachingOption.AsConfigured, Type callingType = null)
        {
            IColumnReader reader = TryGetColumnReader(streamProvider, columnType, columnPath, option, callingType);
            if (reader == null) throw new ColumnDataNotFoundException($"Column data not found at '{columnPath}'.", columnPath);
            return reader;
        }

        public static IColumnWriter TryGetColumnWriter(IStreamProvider streamProvider, Type columnType, string columnPath)
        {
            IColumnWriter writer = null;

            // Build a direct writer for the column type, if available
            ITypeProvider columnTypeProvider = TryGet(columnType);
            if (columnTypeProvider != null) writer = columnTypeProvider.BinaryWriter(streamProvider, columnPath);

            // If the column type doesn't have a provider or writer, convert to String8 and write that
            if (writer == null)
            {
                Func<XArray, XArray> converter = TypeConverterFactory.GetConverter(columnType, typeof(String8));
                if (converter == null) return null;

                writer = TypeProviderFactory.TryGet(typeof(String8)).BinaryWriter(streamProvider, columnPath);
                writer = new ConvertingWriter(writer, converter);
            }

            // Wrap with a NullableWriter to handle null persistence
            writer = new NullableWriter(streamProvider, columnPath, writer);

            // Wrap with an EnumWriter to write as an EnumColumn while possible.
            // Try for *all types* [even bool, byte, ushort] because Enum columns can roll nulls into the column itself and accelerate groupBy
            writer = new EnumWriter(streamProvider, columnPath, columnType, writer);

            return writer;
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

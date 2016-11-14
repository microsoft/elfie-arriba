// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Extensions;
using Arriba.Indexing;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Structures;

namespace Arriba
{
    internal static class ColumnFactory
    {
        internal static SortedColumn<T> CreateSortedColumn<T>(IColumn<T> column, ushort initialCapacity) where T : IComparable<T>
        {
            return new FastAddSortedColumn<T>(column, initialCapacity);
        }

        /// <summary>
        ///  Construct a column given a TypeDescriptor. The column contains the base type
        ///  and may be wrapped in other columns which add additional functionality
        ///  (sorting, indexing, deduping).
        ///  
        ///  Descriptor Syntax: (WrapperType ('[' WrapperDescriptor ']')? ':')* BaseType
        ///    "int"
        ///    "Indexed:Sorted:string"
        ///    "Indexed[HtmlSplitter]:Sorted:string"
        ///   
        ///  Use "bare:type" to request only the base column. Ex: "bare:string"
        /// </summary>
        /// <param name="details">Name of column to create</param>
        /// <param name="initialCapacity">Initial storage capacity of the column; use to avoid resizes if the item count is known</param>
        /// <returns>IColumn of requested type</returns>
        public static IUntypedColumn Build(ColumnDetails details, ushort initialCapacity)
        {
            string[] columnComponents = details.Type.ToLowerInvariant().Split(':');
            string coreType = columnComponents[columnComponents.Length - 1];

            // Default: All columns are sorted, strings are indexed
            if (columnComponents.Length == 1)
            {
                if (coreType.Equals("html"))
                {
                    columnComponents = new string[] { "indexed[html]", "sorted", "string" };
                    coreType = "string";
                }
                else if(coreType.Equals("stringset"))
                {
                    columnComponents = new string[] { "indexed[set]", "sorted", "string" };
                    coreType = "string";
                }
                else if (coreType.Equals("string") || coreType.Equals("json"))
                {
                    columnComponents = new string[] { "indexed", "sorted", "string" };
                    coreType = "string";
                }
                else
                {
                    columnComponents = new string[] { "sorted", coreType };
                }
            }
            else if (columnComponents.Length == 2 && columnComponents[0].Equals("bare", StringComparison.OrdinalIgnoreCase))
            {
                // Specify x:bare to get *only* the base column
                columnComponents = new string[] { coreType };
            }

            switch (coreType)
            {
                case "bool":
                case "boolean":
                    Value defaultValue = Value.Create(details.Default);
                    bool defaultAsBoolean;
                    if (!defaultValue.TryConvert<bool>(out defaultAsBoolean)) defaultAsBoolean = false;

                    UntypedColumn<bool> utc = new UntypedColumn<bool>(new BooleanColumn(defaultAsBoolean));
                    utc.Name = details.Name;
                    return utc;
                case "byte":
                    return Build<byte>(details, columnComponents, initialCapacity);
                case "short":
                case "int16":
                    return Build<short>(details, columnComponents, initialCapacity);
                case "int":
                case "int32":
                    return Build<int>(details, columnComponents, initialCapacity);
                case "long":
                case "int64":
                    return Build<long>(details, columnComponents, initialCapacity);
                case "float":
                case "single":
                    return Build<float>(details, columnComponents, initialCapacity);
                case "double":
                    return Build<double>(details, columnComponents, initialCapacity);
                case "ushort":
                case "uint16":
                    return Build<ushort>(details, columnComponents, initialCapacity);
                case "uint":
                case "uint32":
                    return Build<uint>(details, columnComponents, initialCapacity);
                case "ulong":
                case "uint64":
                    return Build<ulong>(details, columnComponents, initialCapacity);
                case "datetime":
                    return Build<DateTime>(details, columnComponents, initialCapacity);
                case "guid":
                    return Build<Guid>(details, columnComponents, initialCapacity);
                case "timespan":
                    return Build<TimeSpan>(details, columnComponents, initialCapacity);
                case "string":
                    return BuildByteBlock(details, columnComponents);
                default:
                    throw new ArribaException(StringExtensions.Format("Column Type '{0}' is not currently supported.", coreType));
            }
        }

        private static IUntypedColumn Build<T>(ColumnDetails details, string[] typeComponents, ushort initialCapacity) where T : struct, IComparable<T>, IEquatable<T>
        {
            // Convert the default for the new column
            Value defaultValue = Value.Create(details.Default);
            T dAsT;
            if (!defaultValue.TryConvert<T>(out dAsT)) dAsT = default(T);

            // Build the raw column
            IColumn<T> columnSoFar = new ValueTypeColumn<T>(dAsT, initialCapacity);

            // Wrap the column as requested (the last component is the type itself)
            for (int i = typeComponents.Length - 2; i >= 0; --i)
            {
                switch (typeComponents[i])
                {
                    case "sorted":
                        columnSoFar = CreateSortedColumn(columnSoFar, initialCapacity);
                        break;
                    default:
                        throw new ArribaException(StringExtensions.Format("Column Type Wrapper '{0}' is not currently supported.", typeComponents[i]));
                }
            }

            // De-type the column for generic use
            var utc = new UntypedColumn<T>(columnSoFar);

            // Tell it the column name
            utc.Name = details.Name;

            return utc;
        }

        private static IUntypedColumn BuildByteBlock(ColumnDetails details, string[] typeComponents)
        {
            ByteBlock defaultValue;
            Value.Create(details.Default).TryConvert<ByteBlock>(out defaultValue);

            // Build the raw column
            IColumn<ByteBlock> columnSoFar = new ByteBlockColumn(defaultValue);

            // Wrap the column as requested (the last component is the type itself)
            for (int i = typeComponents.Length - 2; i >= 0; --i)
            {
                string fullComponent = typeComponents[i];
                string[] componentParts = fullComponent.Split('[', ']');

                switch (componentParts[0])
                {
                    case "sorted":
                        columnSoFar = CreateSortedColumn(columnSoFar, 0);
                        break;
                    case "indexed":
                        columnSoFar = new IndexedColumn(columnSoFar, BuildSplitter(componentParts.Length > 1 ? componentParts[1] : "default"));
                        break;
                    default:
                        throw new ArribaException(StringExtensions.Format("Column Type Wrapper '{0}' is not currently supported.", typeComponents[i]));
                }
            }

            // De-type the column for generic use
            var utc = new UntypedColumn<ByteBlock>(columnSoFar);

            // Tell it the column name
            utc.Name = details.Name;

            return utc;
        }

        private static IWordSplitter BuildSplitter(string descriptor)
        {
            switch (descriptor)
            {
                case "default":
                    return new DefaultWordSplitter();
                case "html":
                    return new HtmlWordSplitter(new DefaultWordSplitter());
                case "set":
                    return new SetSplitter();
                default:
                    throw new ArribaException(StringExtensions.Format("Word Splitter '{0}' is not currently supported.", descriptor));
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using COLUMN_CREATOR = System.Func<Arriba.Model.Column.ColumnDetails, string[], ushort, Arriba.Model.IUntypedColumn>;

using Arriba.Extensions;
using Arriba.Indexing;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Structures;

namespace Arriba
{
    public static class ColumnFactory
    {
        private static Dictionary<string, COLUMN_CREATOR> s_columnCreators;
        static ColumnFactory()
        {
            ResetColumnCreators();
        }

        internal static void ResetColumnCreators()
        {
            s_columnCreators = new Dictionary<string, COLUMN_CREATOR>();

            s_columnCreators["bool"] = (details, columnComponents, initialCapacity) =>
            {
                AdjustColumnComponents(ref columnComponents);

                Value defaultValue = Value.Create(details.Default);
                bool defaultAsBoolean;
                if (!defaultValue.TryConvert<bool>(out defaultAsBoolean)) defaultAsBoolean = false;

                UntypedColumn<bool> utc = new UntypedColumn<bool>(new BooleanColumn(defaultAsBoolean));
                utc.Name = details.Name;
                return utc;
            };

            s_columnCreators["boolean"] = s_columnCreators["bool"];

            s_columnCreators["byte"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<byte>(details, columnComponents, initialCapacity); };
            s_columnCreators["sbyte"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<sbyte>(details, columnComponents, initialCapacity); };

            s_columnCreators["short"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<short>(details, columnComponents, initialCapacity); };
            s_columnCreators["int16"] = s_columnCreators["short"];

            s_columnCreators["int"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<int>(details, columnComponents, initialCapacity); };
            s_columnCreators["int32"] = s_columnCreators["int"];

            s_columnCreators["long"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<long>(details, columnComponents, initialCapacity); };
            s_columnCreators["int64"] = s_columnCreators["long"];

            s_columnCreators["float"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<float>(details, columnComponents, initialCapacity); };
            s_columnCreators["single"] = s_columnCreators["float"];

            s_columnCreators["double"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<double>(details, columnComponents, initialCapacity); };

            s_columnCreators["ushort"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<ushort>(details, columnComponents, initialCapacity); };
            s_columnCreators["uint16"] = s_columnCreators["ushort"];

            s_columnCreators["uint"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<uint>(details, columnComponents, initialCapacity); };
            s_columnCreators["uint32"] = s_columnCreators["uint"];

            s_columnCreators["ulong"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<ulong>(details, columnComponents, initialCapacity); };
            s_columnCreators["uint64"] = s_columnCreators["ulong"];

            s_columnCreators["datetime"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<DateTime>(details, columnComponents, initialCapacity); };

            s_columnCreators["guid"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<Guid>(details, columnComponents, initialCapacity); };

            s_columnCreators["timespan"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return Build<TimeSpan>(details, columnComponents, initialCapacity); };

            s_columnCreators["string"] = (details, columnComponents, initialCapacity) => { AdjustColumnComponents(ref columnComponents); return BuildByteBlock(details, columnComponents); };
            s_columnCreators["json"] = s_columnCreators["string"];
            s_columnCreators["html"] = s_columnCreators["string"];
            s_columnCreators["stringset"] = s_columnCreators["string"];
        }

        public static void AddColumnCreator(string coreType, COLUMN_CREATOR creationFunc)
        {
            if (s_columnCreators.ContainsKey(coreType))
            {
                throw new ArribaException(StringExtensions.Format("Creation method for Column Type '{0}' is already registered", coreType));
            }

            s_columnCreators[coreType] = creationFunc;
        }

        public static SortedColumn<T> CreateSortedColumn<T>(IColumn<T> column, ushort initialCapacity) where T : IComparable<T>
        {
            return new FastAddSortedColumn<T>(column, initialCapacity);
        }

        public static string GetCanonicalTypeName(Type t)
        {
            if (t == typeof(short)) return "short";
            if (t == typeof(ushort)) return "ushort";
            if (t == typeof(int)) return "int";
            if (t == typeof(uint)) return "uint";
            if (t == typeof(long)) return "long";
            if (t == typeof(ulong)) return "ulong";
            if (t == typeof(float)) return "float";
            if (t == typeof(double)) return "double";

            return t.Name.ToLowerInvariant();
        }

        public static Type GetTypeFromTypeString(string columnDetailsType)
        {
            if (String.IsNullOrEmpty(columnDetailsType)) return null;

            switch(columnDetailsType.ToLowerInvariant())
            {
                case "bool":
                case "boolean":
                    return typeof(bool);
                case "byte":
                    return typeof(byte);
                case "short":
                case "int16":
                    return typeof(short);
                case "int":
                case "int32":
                    return typeof(int);
                case "long":
                case "int64":
                    return typeof(long);
                case "float":
                case "single":
                    return typeof(float);
                case "double":
                    return typeof(double);
                case "ushort":
                case "uint16":
                    return typeof(ushort);
                case "uint":
                case "uint32":
                    return typeof(uint);
                case "ulong":
                case "uint64":
                    return typeof(ulong);
                case "datetime":
                    return typeof(DateTime);
                case "guid":
                    return typeof(Guid);
                case "timespan":
                    return typeof(TimeSpan);
                case "string":
                case "json":
                case "html":
                case "stringset":
                    return typeof(string);
                default:
                    return null;
            }
        }

        public static object GetDefaultValueFromTypeString(string columnDetailsType)
        {
            if (String.IsNullOrEmpty(columnDetailsType)) return null;

            switch (columnDetailsType.ToLowerInvariant())
            {
                case "bool":
                case "boolean":
                    return default(bool);
                case "byte":
                    return default(byte);
                case "short":
                case "int16":
                    return default(short);
                case "int":
                case "int32":
                    return default(int);
                case "long":
                case "int64":
                    return default(long);
                case "float":
                case "single":
                    return default(float);
                case "double":
                    return default(double);
                case "ushort":
                case "uint16":
                    return default(ushort);
                case "uint":
                case "uint32":
                    return default(uint);
                case "ulong":
                case "uint64":
                    return default(ulong);
                case "datetime":
                    return default(DateTime);
                case "guid":
                    return default(Guid);
                case "timespan":
                    return default(TimeSpan);
                case "string":
                case "json":
                case "html":
                case "stringset":
                    return default(string);
                default:
                    return null;
            }
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
        internal static IUntypedColumn Build(ColumnDetails details, ushort initialCapacity)
        {
            string[] columnComponents = details.Type.ToLowerInvariant().Split(':');
            string coreType = columnComponents[columnComponents.Length - 1];

            COLUMN_CREATOR creatorFunc = null;

            if (s_columnCreators.TryGetValue(coreType, out creatorFunc) == false)
            {
                throw new ArribaException(StringExtensions.Format("Column Type '{0}' is not currently supported.", coreType));
            }

            return creatorFunc(details, columnComponents, initialCapacity);
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

        private static string[] AdjustColumnComponents(ref string[] columnComponents)
        {
            string coreType = columnComponents[columnComponents.Length - 1];

            // Default: All columns are sorted, strings are indexed
            if (columnComponents.Length == 1)
            {
                if (coreType.Equals("html"))
                {
                    columnComponents = new string[] { "indexed[html]", "sorted", "string" };
                    coreType = "string";
                }
                else if (coreType.Equals("stringset"))
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

            return columnComponents;
        }
    }
}

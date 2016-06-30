// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Elfie.Model.Structures
{
    /// <summary>
    ///  SymbolNodeKind identifies the type of node (Namespace, Class, etc)
    ///  in SymbolTreeNode trees. They map to characters for readable serialization.
    ///  
    ///  Types and Above are uppercase. 
    ///  Members are lowercase.
    ///  
    ///  NOTE: Make sure to update extension methods when adding new types. The methods
    ///  are used to figure out how much of the tree to include in given names (namespace
    ///  is everything in the tree until IsAboveNamespace).
    /// </summary>
    public enum SymbolType : byte
    {
        Any = 0,

        Package = (byte)'P',
        Metadata = (byte)'M',
        Version = (byte)'V',
        PopularityRank = (byte)'R',
        Assembly = (byte)'A',
        FrameworkTarget = (byte)'F',
        Namespace = (byte)'N',
        Class = (byte)'C',
        Struct = (byte)'S',
        Enum = (byte)'E',
        Interface = (byte)'I',

        Constructor = (byte)'c',
        StaticConstructor = (byte)'s',
        Destructor = (byte)'d',
        Field = (byte)'f',
        Property = (byte)'p',
        Indexer = (byte)'i',
        Method = (byte)'m',
        ExtensionMethod = (byte)'x',
        ExtendedType = (byte)'t',
        Event = (byte)'e',

        Excluded = (byte)'X'
    }

    public static class SymbolTypeExtensions
    {
        /// <summary>
        ///  Returns whether the SymbolType is one used above namespaces. Used to identify the namespace part
        ///  of a Symbol's path to write the namespace properly.
        /// </summary>
        /// <param name="type">SymbolType to check</param>
        /// <returns>True if the SymbolType is used above the root namespace in the hierarchy, False otherwise</returns>
        public static bool IsAboveNamespace(this SymbolType type)
        {
            return
                type == SymbolType.Package ||
                type == SymbolType.Version ||
                type == SymbolType.PopularityRank ||
                type == SymbolType.FrameworkTarget ||
                type == SymbolType.Assembly;
        }

        /// <summary>
        ///  Returns whether the SymbolType is one used above types in the hierarchy (namespace, assembly, package, ...).
        ///  Used to tell distinguish non-type SymbolTypes above types (namespace) from members (method, property).
        /// </summary>
        /// <param name="type">SymbolType to check</param>
        /// <returns>True if a SymbolType above types, False if type or member SymbolType</returns>
        public static bool IsAboveType(this SymbolType type)
        {
            return type.IsAboveNamespace() || type == SymbolType.Namespace;
        }

        /// <summary>
        ///  Returns whether the SymbolType is a "type" level entity (class, struct, enum, interface).
        ///  Used to control traversal to nested types and distinguish ancestor types from namespace and higher.
        /// </summary>
        /// <param name="type">SymbolType to check</param>
        /// <returns>True if SymbolType is a 'type', False for members and SymbolTypes above types in the hierarchy</returns>
        public static bool IsType(this SymbolType type)
        {
            return type == SymbolType.Class || type == SymbolType.Struct || type == SymbolType.Enum || type == SymbolType.Interface;
        }

        /// <summary>
        ///  Returns whether the SymbolType is an extension method type or related details.
        /// </summary>
        /// <param name="type">SymbolType to check</param>
        /// <returns>True if extension method or related, False otherwise</returns>
        public static bool IsExtensionMethod(this SymbolType type)
        {
            return type == SymbolType.ExtensionMethod || type == SymbolType.ExtendedType;
        }

        /// <summary>
        ///  Returns whether the SymbolType should be written with braces around any parameters [C# style syntax]
        /// </summary>
        /// <param name="type">SymbolType to check</param>
        /// <returns>True to write parameters in braces, False to use parens</returns>
        public static bool IsBracedType(this SymbolType type)
        {
            return type == SymbolType.Property || type == SymbolType.Indexer;
        }
    }
}

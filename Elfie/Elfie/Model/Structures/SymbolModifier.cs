// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.CodeAnalysis.Elfie.Model.Structures
{
    /// <summary>
    ///  SymbolNodeKind indicates modifiers (public override) on nodes in SymbolTreeNode trees.
    /// </summary>
    [Flags]
    public enum SymbolModifier : byte
    {
        None = 0,
        Public = 1,
        Protected = 2,
        Internal = 4,
        Private = 8,

        Static = 16,

        // Not Needed for scenarios.
        //Extern = 32,
        //Override = 64,
        //Sealed = 128,        
        //Virtual = 256,
        //Abstract = 512,
    }

    public static class SymbolModifierExtensions
    {
        public static bool Matches(this SymbolModifier query, SymbolModifier candidate)
        {
            // Modifiers match if every flag on query is set on candidate
            return ((int)candidate & (int)query) == (int)query;
        }
    }
}

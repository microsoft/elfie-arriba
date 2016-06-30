// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model.Structures
{
    /// <summary>
    ///  SymbolTreeNodes contain the details needed to describe code symbols fully
    ///  and are used in the DefinedSymbolTree and ExternalSymbolTree.
    /// </summary>
    public struct SymbolDetails : IBinarySerializable
    {
        // Additional Information for Symbols
        public int ParametersIdentifier;
        public SymbolType Type;
        public SymbolModifier Modifiers;

        public void UpdateIdentifiers(StringStore store)
        {
            this.ParametersIdentifier = store.GetSerializationIdentifier(this.ParametersIdentifier);
        }

        public override bool Equals(object o)
        {
            if (!(o is SymbolDetails)) return false;

            SymbolDetails other = (SymbolDetails)o;
            return this.ParametersIdentifier == other.ParametersIdentifier && this.Type == other.Type && this.Modifiers == other.Modifiers;
        }

        public override int GetHashCode()
        {
            return this.ParametersIdentifier.GetHashCode() ^ this.Type.GetHashCode() ^ this.Modifiers.GetHashCode();
        }

        public void WriteBinary(BinaryWriter w)
        {
            w.Write(ParametersIdentifier);
            w.Write((byte)Type);
            w.Write((byte)Modifiers);
        }

        public void ReadBinary(BinaryReader r)
        {
            ParametersIdentifier = r.ReadInt32();
            Type = (SymbolType)r.ReadByte();
            Modifiers = (SymbolModifier)r.ReadByte();
        }
    }
}

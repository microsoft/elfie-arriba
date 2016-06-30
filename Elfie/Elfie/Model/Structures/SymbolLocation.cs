// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model.Structures
{
    /// <summary>
    ///  SymbolLocations are kept in parallel with a corresponding symbol tree
    ///  and add file and location information.
    /// </summary>
    public struct SymbolLocation : IBinarySerializable
    {
        // Additional Location information
        public int FileIndex;
        public ushort Line;
        public ushort CharInLine;

        public SymbolLocation(int fileIndex, ushort line, ushort charInLine)
        {
            FileIndex = fileIndex;
            Line = line;
            CharInLine = charInLine;
        }

        public void WriteBinary(BinaryWriter w)
        {
            w.Write(FileIndex);
            w.Write(Line);
            w.Write(CharInLine);
        }

        public void ReadBinary(BinaryReader r)
        {
            FileIndex = r.ReadInt32();
            Line = r.ReadUInt16();
            CharInLine = r.ReadUInt16();
        }
    }
}

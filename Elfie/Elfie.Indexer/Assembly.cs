// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection.PortableExecutable;

using Microsoft.CodeAnalysis.Elfie.PDB;

namespace Microsoft.CodeAnalysis.Elfie.Indexer
{
    public class RsDsSignature
    {
        public Guid Guid { get; private set; }
        public uint Age { get; private set; }

        public RsDsSignature(Guid guid, uint age)
        {
            this.Guid = guid;
            this.Age = age;
        }

        public override string ToString()
        {
            return String.Format("{0:n}{1}", this.Guid, this.Age);
        }
    }

    public static class Assembly
    {
        private const uint RsDsMarker = 0x53445352;

        public static string GetSymbolCachePdbPath(string binaryFilePath)
        {
            RsDsSignature signature = ReadRsDsSignature(binaryFilePath);
            if (signature == null) return null;

            string pdbFileName = Path.GetFileNameWithoutExtension(binaryFilePath) + ".pdb";
            return Path.Combine(SymbolCache.Path, pdbFileName, signature.ToString(), pdbFileName);
        }

        public static RsDsSignature ReadRsDsSignature(string binaryFilePath)
        {
            using (FileStream stream = new FileStream(binaryFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ReadRsDsSignature(stream);
            }
        }

        public static RsDsSignature ReadRsDsSignature(Stream stream)
        {
            try
            {
                // Verify the binary starts with "MZ"
                stream.Seek(0, SeekOrigin.Begin);
                if (stream.ReadByte() != 'M') return null;
                if (stream.ReadByte() != 'Z') return null;

                // Create a PEReader
                stream.Seek(0, SeekOrigin.Begin);
                PEReader peReader = new PEReader(stream);
                BinaryReader binReader = new BinaryReader(stream);

                // Look for a DebugDirectory entry for RSDS
                foreach (var entry in peReader.ReadDebugDirectory())
                {
                    int debugDirectoryOffset = entry.DataPointer;

                    // Go to the offset it points to
                    long currentPosition = stream.Position;
                    stream.Seek(debugDirectoryOffset, SeekOrigin.Begin);

                    // If we see "RSDS", read the Guid and Age
                    uint dataDirectorySignature = binReader.ReadUInt32();
                    if (dataDirectorySignature == RsDsMarker)
                    {
                        Guid binaryGuid = new Guid(binReader.ReadBytes(16));
                        uint age = binReader.ReadUInt32();

                        return new RsDsSignature(binaryGuid, age);
                    }

                    // If not, put the stream back to read the next DebugDirectory
                    stream.Seek(currentPosition, SeekOrigin.Begin);
                }
            }
            catch (BadImageFormatException) { }
            catch (EndOfStreamException) { }

            // If we couldn't find a DebugDirectory of the right type, give up
            return null;
        }
    }
}

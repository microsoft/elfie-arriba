// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
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
        private const uint RsDsMarker = 0x53445352;  // "RSDS" in ASCII
        private const uint PdbVersion = 20000404;    // 2000-04-04; 0x01312E94
        private const long PdbBlockSize = 0x200;

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

        public static void WriteRsDsSignature(string pdbFilePath, RsDsSignature signature)
        {
            using (FileStream stream = new FileStream(pdbFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                WriteRsDsSignature(stream, signature);
            }
        }

        public static void WriteRsDsSignature(Stream pdbStream, RsDsSignature signature)
        {
            BinaryReader br = new BinaryReader(pdbStream);
            BinaryWriter bw = new BinaryWriter(pdbStream);

            // Look for all blocks starting with the PDB Version (20000404) and replace the Age and GUID four bytes later
            int replacementCount = 0;

            for(long position = PdbBlockSize; position + PdbBlockSize < pdbStream.Length; position += PdbBlockSize)
            {
                pdbStream.Seek(position, SeekOrigin.Begin);
                uint startingValue = br.ReadUInt32();
                if(startingValue == PdbVersion)
                {
                    br.ReadUInt32();

                    // Read existing valuesand seek back  [debuggability] 
                    //uint age = br.ReadUInt32();
                    //Guid binaryGuid = new Guid(br.ReadBytes(16));
                    //pdbStream.Seek(-20, SeekOrigin.Current);

                    bw.Write(signature.Age);
                    bw.Write(signature.Guid.ToByteArray());
                    replacementCount++;
                }
            }

            if(replacementCount == 0) throw new BadImageFormatException($"PDB Stream didn't contain expected marker {PdbVersion} on any blocks.");
        }
    }
}

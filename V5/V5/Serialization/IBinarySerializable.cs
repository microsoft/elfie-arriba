using System.IO;

namespace V5.Serialization
{
    public interface IBinarySerializable
    {
        /// <summary>
        ///  Read this object from the given reader from the current position for the given length.
        /// </summary>
        /// <param name="reader">BinaryReader to read from</param>
        /// <param name="length">Length to read</param>
        void ReadBinary(BinaryReader reader, long length);

        /// <summary>
        ///  Prepare this instance to be written (sorting, remapping, etc), and return whether
        ///  it needs to be written (whether it's changed since it was loaded/created).
        /// </summary>
        bool PrepareToWrite();

        /// <summary>
        ///  Write this object to the given writer at the current position.
        /// </summary>
        /// <param name="writer">BinaryWriter to write to</param>
        void WriteBinary(BinaryWriter writer);
    }
}

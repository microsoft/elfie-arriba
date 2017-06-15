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
        ///  Get the length needed to write this object.
        /// </summary>
        long LengthBytes { get; }

        /// <summary>
        ///  Write this object to the given writer at the current position.
        /// </summary>
        /// <param name="writer">BinaryWriter to write to</param>
        void WriteBinary(BinaryWriter writer);
    }
}

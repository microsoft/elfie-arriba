using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Elfie.Model.Strings
{
    /// <summary>
    ///  String8Block enables copying String8s to a stable location with
    ///  constrained object overhead. Use it when a String8 needs to be kept
    ///  longer than the array it's currently referencing.
    /// </summary>
    /// <remarks>
    ///     Each Block is 32 bytes (BlockPart) + 24 bytes (byte[]) = 56 bytes + length.
    ///     Individual Strings [1,024 bytes] are 5% overhead [56 bytes].
    ///     Packed Strings are &lt;1.5% overhead [&gt;= 63k filled, 1,024 bytes empty + 56 bytes]
    /// </remarks>
    public class String8Block
    {
        public const int DefaultBlockLengthBytes = 64 * 1024;
        public const int StoreIndividuallyLengthBytes = 1024;

        private class BlockPart
        {
            public byte[] Block;
            public int LengthUsed;

            public BlockPart() : this(DefaultBlockLengthBytes)
            { }

            public BlockPart(int length)
            {
                this.Block = new byte[length];
                this.LengthUsed = 0;
            }
        }

        private List<BlockPart> _blocks;
        private BlockPart _current;

        /// <summary>
        ///  Construct a new String8Block to hold String8 copies.
        /// </summary>
        public String8Block()
        {
            _blocks = new List<BlockPart>();

            _current = new BlockPart();
            _blocks.Add(_current);
        }

        /// <summary>
        ///  Create a copy of a String8. Use when the source of the String8s
        ///  will change (like a reader reusing the same byte[] buffer) and
        ///  you need to keep a copy of specific values with minimal object overhead.
        /// </summary>
        /// <param name="source">String8 to copy</param>
        /// <returns>String8 copy which will persist</returns>
        public String8 GetCopy(String8 source)
        {
            if (source.IsEmpty()) return String8.Empty;

            BlockPart targetBlock = _current;

            // If the String8 is too long, store by itself
            if(source.Length >= StoreIndividuallyLengthBytes)
            {
                targetBlock = new BlockPart(source.Length);
                _blocks.Add(targetBlock);
            }

            // If the current block is too full, start a new one
            if (_current.Block.Length < _current.LengthUsed + source.Length)
            {
                targetBlock = new BlockPart();
                _blocks.Add(targetBlock);
                _current = targetBlock;
            }

            // Write the String8 to the chosen block and return a reference to the new copy
            int writePosition = targetBlock.LengthUsed;
            source.WriteTo(targetBlock.Block, writePosition);
            targetBlock.LengthUsed += source.Length;
            return new String8(targetBlock.Block, writePosition, source.Length);
        }
    }
}

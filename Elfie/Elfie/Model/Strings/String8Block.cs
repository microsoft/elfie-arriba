// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis.Elfie.Serialization;

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

        private BlockPart GetBlockForLength(int length)
        {
            BlockPart targetBlock = _current;

            if (length >= StoreIndividuallyLengthBytes)
            {
                // If the String8 is too long, store by itself
                targetBlock = new BlockPart(length);
                _blocks.Add(targetBlock);
            }
            else if (_current.Block.Length < _current.LengthUsed + length)
            {
                // If the current block is too full, start a new one
                targetBlock = new BlockPart();
                _blocks.Add(targetBlock);
                _current = targetBlock;
            }

            return targetBlock;
        }

        /// <summary>
        ///  Create a copy of a String8. Use when the source of the String8s
        ///  will change (like a reader reusing the same byte[] buffer) and
        ///  you need to keep a copy of specific values with minimal object overhead.
        /// </summary>
        /// <param name="source">String8 to copy</param>
        /// <returns>String8 copy which will persist</returns>
        public String8 GetCopy(ITabularValue source)
        {
            if (source is String8TabularValue) return GetCopy(source.ToString8());
            return GetCopy(source.ToString());
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

            BlockPart targetBlock = GetBlockForLength(source.Length);

            // Write the String8 to the chosen block and return a reference to the new copy
            int writePosition = targetBlock.LengthUsed;
            targetBlock.LengthUsed += source.WriteTo(targetBlock.Block, writePosition);
            return new String8(targetBlock.Block, writePosition, source.Length);
        }

        /// <summary>
        ///  Create a concatenation of three String8s. Used to join values
        ///  with a delimiter in a memory efficient way.
        /// </summary>
        /// <param name="first">First Value</param>
        /// <returns>String8 copy which will persist</returns>
        public String8 Concatenate(String8 first, String8 delimiter, String8 second)
        {
            // If either string is empty, use only the other [if both empty, String8.Empty returned]
            if (first.IsEmpty()) return GetCopy(second);
            if (second.IsEmpty()) return GetCopy(first);

            BlockPart targetBlock = null;

            // If "first" is the last thing on the last block...
            if (_blocks.Count > 0)
            {
                targetBlock = _blocks[_blocks.Count - 1];
                if (targetBlock.Block == first.Array && targetBlock.LengthUsed == first.Index + first.Length)
                {
                    // If there's room to concatenate in place, do that
                    if (targetBlock.Block.Length >= targetBlock.LengthUsed + delimiter.Length + second.Length)
                    {
                        targetBlock.LengthUsed += delimiter.WriteTo(targetBlock.Block, targetBlock.LengthUsed);
                        targetBlock.LengthUsed += second.WriteTo(targetBlock.Block, targetBlock.LengthUsed);
                        return new String8(first.Array, first.Index, targetBlock.LengthUsed - first.Index);
                    }

                    // If not, "remove" first from the block to recycle the space
                    if (first.Index == 0)
                    {
                        // If first was alone, remove the whole block
                        _blocks.RemoveAt(_blocks.Count - 1);
                    }
                    else
                    {
                        // Deduct the used space for "first"
                        _blocks[_blocks.Count - 1].LengthUsed -= first.Length;
                    }
                }
            }

            // Find new room for the concatenated value
            int requiredLength = first.Length + delimiter.Length + second.Length;
            targetBlock = GetBlockForLength((int)(1.5 * requiredLength));

            // Write the parts to the chosen block and return a reference to the new copy
            int startPosition = targetBlock.LengthUsed;
            targetBlock.LengthUsed += first.WriteTo(targetBlock.Block, targetBlock.LengthUsed);
            targetBlock.LengthUsed += delimiter.WriteTo(targetBlock.Block, targetBlock.LengthUsed);
            targetBlock.LengthUsed += second.WriteTo(targetBlock.Block, targetBlock.LengthUsed);
            return new String8(targetBlock.Block, startPosition, targetBlock.LengthUsed - startPosition);
        }

        /// <summary>
        ///  Create a copy of a String8. Use when the source of the String8s
        ///  will change (like a reader reusing the same byte[] buffer) and
        ///  you need to keep a copy of specific values with minimal object overhead.
        /// </summary>
        /// <param name="source">String8 to copy</param>
        /// <returns>String8 copy which will persist</returns>
        public String8 GetCopy(string source)
        {
            if (string.IsNullOrEmpty(source)) return String8.Empty;

            int length = String8.GetLength(source);
            BlockPart targetBlock = GetBlockForLength(length);

            // Write the String to the chosen block and return a reference to the new copy
            int writePosition = targetBlock.LengthUsed;
            targetBlock.LengthUsed += source.Length;
            return String8.Convert(source, targetBlock.Block, writePosition);
        }

        /// <summary>
        ///  Clear the String8Block. Clear reuses some memory, avoiding allocations
        ///  if use between clear calls is relatively small.
        /// </summary>
        public void Clear()
        {
            // Clear length used on the first block
            BlockPart first = _blocks[0];
            first.LengthUsed = 0;

            // Remove other blocks and restore only the first
            _blocks.Clear();
            _blocks.Add(first);
        }
    }
}

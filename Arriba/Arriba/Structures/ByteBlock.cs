// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using Arriba.Serialization;

namespace Arriba.Structures
{
    /// <summary>
    ///  ByteBlock represents any value of dynamic length. They are packed together in
    ///  ByteBlockSets, avoiding per-instance overhead (24+ bytes per object). Strings
    ///  and byte arrays are represented within Arriba columns as ByteBlocks.
    /// </summary>
    public struct ByteBlock : IComparable<ByteBlock>, IComparable, IEquatable<ByteBlock>
    {
        public byte[] Array;
        public int Index;
        public int Length;

        public static ByteBlock Zero = new ByteBlock() { Array = null, Index = 0, Length = 0 };

        public static ByteBlock TestBlock(string value)
        {
            // Convert string to bytes normally
            byte[] unwrappedBytes = Encoding.UTF8.GetBytes(value);

            // Make a padded array for contents
            byte[] wrappedBytes = new byte[unwrappedBytes.Length + 6];

            // Make all 'A' to see splitting errors if bounds disrespected
            for (int i = 0; i < wrappedBytes.Length; ++i)
            {
                wrappedBytes[i] = 41;
            }

            // Copy the value into the middle of the padded array
            unwrappedBytes.CopyTo(wrappedBytes, 3);

            return new ByteBlock(wrappedBytes, 3, unwrappedBytes.Length);
        }

        public ByteBlock(byte[] array)
            : this(array, 0, array.Length)
        { }

        public ByteBlock(byte[] array, int index, int length)
        {
            this.Array = array;
            this.Index = index;
            this.Length = length;
        }

        public ByteBlock Copy()
        {
            byte[] copyArray = new byte[this.Length];
            if (this.Length > 0) this.CopyTo(copyArray);
            return new ByteBlock(copyArray);
        }

        #region Implicit Conversions (from string, byte[])
        [DebuggerStepThroughAttribute]
        public static implicit operator ByteBlock(byte[] value)
        {
            if (value == null) return ByteBlock.Zero;
            return new ByteBlock(value);
        }

        [DebuggerStepThroughAttribute]
        public static implicit operator ByteBlock(string value)
        {
            if (value == null) return ByteBlock.Zero;

            // TEST ONLY: Use padded ByteBlocks to look for code ignoring index
            //return ByteBlock.TestBlock(value);

            return new ByteBlock(Encoding.UTF8.GetBytes(value));
        }
        #endregion

        #region IComparable
        /// <summary>
        ///  Compare this ByteBlock to another object. Will return not equal unless
        ///  the type of the other object is ByteBlock.
        /// </summary>
        /// <param name="o">Object to compare this one with</param>
        /// <returns>Positive if this is greater, negative if other greater, 0 if equal</returns>
        public int CompareTo(object o)
        {
            if (o is ByteBlock)
            {
                return this.CompareTo((ByteBlock)o);
            }
            else if (o is string)
            {
                return this.CompareTo((ByteBlock)(string)o);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        ///  Compare this ByteBlock to another one and returns an indication
        ///  of their relative values. Comparison is a byte by byte compare,
        ///  and the first Block with a larger value is considered greater.
        /// </summary>
        /// <param name="other">ByteBlock to compare with</param>
        /// <returns>Positive if this is greater, negative if other greater, 0 if equal</returns>
        public int CompareTo(ByteBlock other)
        {
            // Compare bytes and return the first difference, if any
            int cmp = IsPrefixOf(other);
            if (cmp != 0) return cmp;

            // If identical, return the length comparison (smaller first)
            return this.Length.CompareTo(other.Length);
        }

        /// <summary>
        ///  Compare this ByteBlock to another one and return whether other
        ///  starts with this value.
        /// </summary>
        /// <param name="other">ByteBlock to compare with</param>
        /// <returns>Positive if this is greater, negative if other greater, 0 if other starts with this</returns>
        public int IsPrefixOf(ByteBlock other)
        {
            int commonLength = Math.Min(this.Length, other.Length);

            // Compare bytes and return the first difference, if any
            for (int i = 0; i < commonLength; ++i)
            {
                int cmp = this.Array[this.Index + i].CompareTo(other.Array[other.Index + i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            // If identical, this is a prefix if other is at least as long
            if (other.Length >= this.Length) return 0;

            // Otherwise, this is a later value
            return 1;
        }

        /// <summary>
        ///  Compare this ByteBlock to another one and returns an indication
        ///  of their relative values. Comparison is a byte by byte compare,
        ///  and the first Block with a larger value is considered greater.
        /// </summary>
        /// <param name="other">ByteBlock to compare with</param>
        /// <returns>Positive if this is greater, negative if other greater, 0 if equal</returns>
        public int CaseInsensitiveCompareTo(ByteBlock other)
        {
            // Compare bytes and return the first difference, if any
            int cmp = CaseInsensitiveIsPrefixOf(other);
            if (cmp != 0) return cmp;

            // If identical, return the length comparison (smaller first)
            return this.Length.CompareTo(other.Length);
        }

        /// <summary>
        ///  Compare this ByteBlock to another one and return whether other
        ///  starts with this value.
        /// </summary>
        /// <param name="other">ByteBlock to compare with</param>
        /// <returns>Positive if this is greater, negative if other greater, 0 if other starts with this</returns>
        public int CaseInsensitiveIsPrefixOf(ByteBlock other)
        {
            int commonLength = Math.Min(this.Length, other.Length);

            // Compare bytes and return the first difference, if any
            for (int i = 0; i < commonLength; ++i)
            {
                byte tC = this.Array[this.Index + i];
                tC = UTF8.ToLowerInvariant(tC);

                byte oC = other.Array[other.Index + i];
                oC = UTF8.ToLowerInvariant(oC);

                int cmp = tC.CompareTo(oC);
                if (cmp != 0) return cmp;
            }

            // If identical, this is a prefix if other is at least as long
            if (other.Length >= this.Length) return 0;

            // Otherwise, this is a later value
            return 1;
        }

        /// <summary>
        /// Gets a wrapper that implements the desired comparison logic
        /// </summary>
        /// <param name="comparisonType">type of comparison</param>
        /// <returns>a wrapper which implements IComparable with the requested comparison logic</returns>
        public IComparable<ByteBlock> GetExtendedIComparable(Comparison comparisonType)
        {
            return new ByteBlockComparison(this, comparisonType);
        }

        #endregion

        #region Array Operations
        public void CopyTo(ByteBlock other)
        {
            System.Array.Copy(this.Array, this.Index, other.Array, other.Index, this.Length);
        }

        public void CopyTo(byte[] other)
        {
            CopyTo(other, 0);
        }

        public void CopyTo(byte[] other, int position)
        {
            System.Array.Copy(this.Array, this.Index, other, position, this.Length);
        }

        public void WriteTo(Stream stream)
        {
            if (this.Length > 0)
            {
                stream.Write(this.Array, this.Index, this.Length);
            }
        }

        public bool IsZero()
        {
            return this.Array == null || this.Length == 0;
        }
        #endregion

        #region String Operations
        public void ToLowerInvariant()
        {
            if (this.Array != null) UTF8.ToLowerInvariant(this.Array, this.Index, this.Length);
        }
        #endregion

        #region Object Overrides
        public override bool Equals(object o)
        {
            if (!(o is ByteBlock))
            {
                if (o is string)
                {
                    return this.CompareTo((ByteBlock)(string)o) == 0;
                }
                else
                {
                    return false;
                }
            }

            ByteBlock oB = (ByteBlock)o;
            if (this.Length != oB.Length) return false;
            return this.CompareTo(oB) == 0;
        }

        public static bool operator ==(ByteBlock left, ByteBlock right)
        {
            return left.CompareTo(right) == 0;
        }

        public static bool operator !=(ByteBlock left, ByteBlock right)
        {
            return left.CompareTo(right) != 0;
        }

        bool IEquatable<ByteBlock>.Equals(ByteBlock other)
        {
            return this.CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            return unchecked((int)GetHashULong());
        }

        public Guid GetHashAsGuid()
        {
            ulong hash = GetHashULong();

            // Return a GUID with the hash ulong used twice
            return new Guid(
                (uint)((hash >> 32) & 0xFFFFFFFF),
                (ushort)((hash >> 16) & 0xFFFF),
                (ushort)(hash & 0xFFFF),
                (byte)(hash & 0xFF),
                (byte)((hash >> 8) & 0xFF),
                (byte)((hash >> 16) & 0xFF),
                (byte)((hash >> 24) & 0xFF),
                (byte)((hash >> 32) & 0xFF),
                (byte)((hash >> 40) & 0xFF),
                (byte)((hash >> 48) & 0xFF),
                (byte)((hash >> 56) & 0xFF)
            );
        }

        private unsafe ulong GetHashULong()
        {
            fixed (byte* a = this.Array)
            {
                return Hashing.MurmurHash3(a + this.Index, this.Length, 0);
            }
        }

        public override string ToString()
        {
            if (this.Length == 0) return String.Empty;
            return Encoding.UTF8.GetString(this.Array, this.Index, this.Length);
        }

        #endregion

        public enum Comparison
        {
            CaseInsensitiveCompareTo,
            IsPrefixOf,
            CaseInsensitiveIsPrefixOf
        }

        private class ByteBlockComparison : IComparable<ByteBlock>
        {
            private ByteBlock _byteBlock;
            private Func<ByteBlock, int> _comparisonFunction;

            public ByteBlockComparison(ByteBlock parentBlock, Comparison comparison)
            {
                _byteBlock = parentBlock;

                switch (comparison)
                {
                    case Comparison.CaseInsensitiveCompareTo:
                        _comparisonFunction = parentBlock.CaseInsensitiveCompareTo;
                        break;
                    case Comparison.IsPrefixOf:
                        _comparisonFunction = parentBlock.IsPrefixOf;
                        break;
                    case Comparison.CaseInsensitiveIsPrefixOf:
                        _comparisonFunction = parentBlock.CaseInsensitiveIsPrefixOf;
                        break;
                    default:
                        throw new ArgumentException("unexpected comparison type: " + comparison.ToString());
                }
            }

            public int CompareTo(ByteBlock other)
            {
                return _comparisonFunction(other);
            }
        }
    }
}

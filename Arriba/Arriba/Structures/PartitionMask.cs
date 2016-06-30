// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

using Arriba.Serialization;

namespace Arriba.Structures
{
    /// <summary>
    ///  PartitionMask determines the set of hash values a given Table Partition
    ///  contains. Partitions handle items with a specific bit prefix of the hash
    ///  value.
    /// </summary>
    public class PartitionMask : IBinarySerializable
    {
        private byte _bitCount;
        private int _mask;

        public int Value;

        public byte BitCount
        {
            get { return _bitCount; }
            set
            {
                _bitCount = value;
                _mask = (~0 << (32 - this.BitCount));
            }
        }

        public PartitionMask(int value, byte bitCount)
        {
            this.BitCount = bitCount;
            this.Value = value;
        }

        public static PartitionMask All
        {
            get { return new PartitionMask(0, 0); }
        }

        public static PartitionMask[] BuildSet(byte bitCount)
        {
            int count = 1 << bitCount;
            PartitionMask[] set = new PartitionMask[count];

            for (int i = 0; i < count; ++i)
            {
                set[i] = new PartitionMask((i << (32 - bitCount)), bitCount);
            }

            return set;
        }

        public bool Matches(int hash)
        {
            if (this.BitCount == 0) return true;

            int maskedHash = hash & _mask;
            return (maskedHash == this.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfHash(int hash, byte bitCount)
        {
            // compute the index for a hash in a set created by BuildSet with the same bitCount
            // NOTE: must cast to uint to prevent sign extension on shift
            return (int)((uint)hash >> (32 - bitCount));
        }

        public override bool Equals(object other)
        {
            if (other == null || !(other is PartitionMask)) return false;

            PartitionMask o = (PartitionMask)other;
            return (o.BitCount.Equals(this.BitCount) && o.Value.Equals(this.Value));
        }

        public override int GetHashCode()
        {
            return this.BitCount.GetHashCode() ^ this.Value.GetHashCode();
        }

        public override string ToString()
        {
            if (this.BitCount == 0) return string.Empty;

            string valueString = Convert.ToString(this.Value, 2);
            string fullValueString = valueString.PadLeft(32, '0');

            return fullValueString.Substring(0, this.BitCount);
        }

        public void ReadBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.BitCount = context.Reader.ReadByte();
            this.Value = context.Reader.ReadInt32();
        }

        public void WriteBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            context.Writer.Write(this.BitCount);
            context.Writer.Write(this.Value);
        }
    }
}

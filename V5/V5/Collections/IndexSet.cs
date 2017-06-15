using System;
using System.IO;
using V5.Query;
using V5.Serialization;

namespace V5.Collections
{
    /// <summary>
    ///  IndexSet is a bit vector which contains a set of indexes in the range [offset, offset + length).
    ///  It stores them as single bits in an array to keep them extremely compact.
    /// </summary>
    public class IndexSet : IBinarySerializable, IEquatable<IndexSet>
    {
        private uint offset;
        private uint length;

        private ulong[] bitVector;

        public IndexSet(uint offset, uint length)
        {
            this.offset = offset;
            this.length = length;
            this.bitVector = new ulong[(length + 63) >> 6];
        }

        public IndexSet(int offset, int length) : this((uint)offset, (uint)length)
        { }

        public bool this[int index]
        {
            get => (this.bitVector[index >> 6] & (0x1UL << (index & 63))) != 0;
            set
            {
                if (value)
                {
                    this.bitVector[index >> 6] |= (0x1UL << (index & 63));
                }
                else
                {
                    this.bitVector[index >> 6] &= ~(0x1UL << (index & 63));
                }
            }
        }

        public int Count
        {
            get => IndexSetN.Count(this.bitVector);
        }

        public bool Equals(IndexSet other)
        {
            if (this.offset != other.offset) return false;
            if (this.length != other.length) return false;

            for (int i = 0; i < this.bitVector.Length; ++i)
            {
                if (this.bitVector[i] != other.bitVector[i])
                {
                    return false;
                }
            }

            return true;
        }

        public IndexSet None()
        {
            Array.Clear(this.bitVector, 0, this.bitVector.Length);
            return this;
        }

        public IndexSet All()
        {
            // Turn on all bits
            for (int i = 0; i < this.bitVector.Length; ++i)
            {
                this.bitVector[i] = ulong.MaxValue;
            }

            // Turn off bits over 'length'
            if ((length & 63) > 0)
            {
                this.bitVector[this.bitVector.Length - 1] &= (ulong.MaxValue >> (64 - (int)(length & 63)));
            }

            return this;
        }

        //public IndexSet And<T>(T[] values, Operator op, T value)
        //{
        //    IndexSetN.AndWhereGreaterThan<T>(values, value, this.bitVector);
        //    return this;
        //}

        public IndexSet And(Array values, Operator op, object value)
        {
            IndexSetN.AndWhereGreaterThan((byte[])values, (byte)value, this.bitVector);
            return this;
        }

        public IndexSet And(IndexSet other)
        {
            if (this.offset != other.offset) throw new InvalidOperationException();
            if (this.length != other.length) throw new InvalidOperationException();

            for (int i = 0; i < this.bitVector.Length; ++i)
            {
                this.bitVector[i] &= other.bitVector[i];
            }

            return this;
        }

        public IndexSet Or(IndexSet other)
        {
            if (this.offset != other.offset) throw new InvalidOperationException();
            if (this.length != other.length) throw new InvalidOperationException();

            for (int i = 0; i < this.bitVector.Length; ++i)
            {
                this.bitVector[i] |= other.bitVector[i];
            }

            return this;
        }

        public IndexSet AndNot(IndexSet other)
        {
            if (this.offset != other.offset) throw new InvalidOperationException();
            if (this.length != other.length) throw new InvalidOperationException();

            for (int i = 0; i < this.bitVector.Length; ++i)
            {
                this.bitVector[i] = this.bitVector[i] & ~other.bitVector[i];
            }

            return this;
        }

        public void ReadBinary(BinaryReader reader, long length)
        {
            if (length == 0)
            {
                this.offset = 0;
                this.length = 0;
                this.bitVector = Array.Empty<ulong>();
            }
            else
            {
                ulong offsetAndLength = reader.ReadUInt64();
                this.offset = (uint)((offsetAndLength >> 32) & uint.MaxValue);
                this.length = (uint)(offsetAndLength & uint.MaxValue);

                this.bitVector = reader.ReadArray<ulong>(length - 8);
            }
        }

        public void WriteBinary(BinaryWriter writer)
        {
            ulong offsetAndLength = (this.offset << 32) + this.length;
            writer.Write(offsetAndLength);
            writer.Write(this.bitVector);
        }
    }
}

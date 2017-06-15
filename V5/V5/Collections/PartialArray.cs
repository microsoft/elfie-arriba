using System;
using System.IO;
using V5.Serialization;

namespace V5.Collections
{
    public class PartialArray<T> : IBinarySerializable
    {
        private T[] _items;
        private int _count;
        private bool _isDirty;

        public PartialArray() : this(8)
        { }

        public PartialArray(int initialCapacity = 8)
        {
            this._items = new T[initialCapacity];
            this._count = 0;
            this._isDirty = false;
        }

        public PartialArray(T[] array, int count)
        {
            this._items = array;
            this._count = count;
            this._isDirty = false;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= this._count) throw new ArgumentOutOfRangeException("index");
                return this._items[index];
            }
            set
            {
                if (index < 0 || index >= this._count) throw new ArgumentOutOfRangeException("index");
                this._items[index] = value;
                this._isDirty = true;
            }
        }

        #region IColumn [partial]
        public int Count
        {
            get { return this.Count; }
            set
            {
                if (this._items.Length < value) SetCapacity(value);
                this._count = value;
                this._isDirty = true;
            }
        }

        public void SetCapacity(int capacity)
        {
            if (this._items.Length >= capacity) return;

            T[] newArray = new T[capacity];
            this._items.CopyTo(newArray, 0);
            this._items = newArray;
        }

        public void AppendFrom(T[] other, int index, int length)
        {
            this.SetCapacity(this.Count + length);
            Array.Copy(other, index, this._items, this.Count, length);
            this.Count = this.Count + length;
        }

        public void SetValues(T[] values, uint[] indices)
        {
            for(int i = 0; i < indices.Length; ++i)
            {
                this._items[indices[i]] = values[i];
            }

            this._isDirty = true;
        }

        public void GetValues(T[] values, uint[] indices)
        {
            for (int i = 0; i < indices.Length; ++i)
            {
                values[i] = this._items[indices[i]];
            }
        }

        public Array TryGetArray()
        {
            return this._items;
        }
        #endregion

        #region IBinarySerializable
        public bool PrepareToWrite()
        {
            return this._isDirty;
        }

        public void ReadBinary(BinaryReader reader, long length)
        {
            this._items = reader.ReadArray<T>(length);
            this._count = this._items.Length;
            this._isDirty = false;
        }

        public long LengthBytes => this._items.LengthBytes();

        public void WriteBinary(BinaryWriter writer)
        {
            writer.Write(this._items, 0, this._count);
            this._isDirty = false;
        }
        #endregion
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model.Structures
{
    public struct PartialArray<T> : IBinarySerializable, IEnumerable<T>
    {
        private T[] _array;
        public bool IsStaticSize { get; private set; }
        public int Count { get; private set; }

        public PartialArray(int capacity, bool isStaticSize = true)
        {
            _array = new T[capacity];
            this.Count = 0;
            this.IsStaticSize = isStaticSize;
        }

        public PartialArray(T[] array, int currentCount = 0, bool isStaticSize = true)
        {
            _array = array;
            this.Count = currentCount;
            this.IsStaticSize = isStaticSize;
        }

        public T this[int index]
        {
            get
            {
                if (_array == null || index < 0 || index >= this.Count) throw new ArgumentOutOfRangeException("index");
                return _array[index];
            }
            set
            {
                if (_array == null || index < 0 || index >= this.Count) throw new ArgumentOutOfRangeException("index");
                _array[index] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (!this.IsStaticSize)
            {
                if (_array == null)
                {
                    _array = new T[10];
                }
                else if (_array.Length == this.Count)
                {
                    T[] newArray = new T[(int)(_array.Length * 1.50 + 8)];
                    _array.CopyTo(newArray, 0);
                    _array = newArray;
                }
            }

            _array[this.Count] = item;
            this.Count++;
        }

        public void Clear()
        {
            this.Count = 0;
        }

        public void Sort(IComparer<T> comparer)
        {
            System.Array.Sort<T>(_array, 0, this.Count, comparer);
        }

        public int Capacity
        {
            get
            {
                if (_array == null) return 0;
                return _array.Length;
            }
        }

        public bool IsFull
        {
            get { return this.IsStaticSize && (_array == null || _array.Length == this.Count); }
        }

        public void CopyTo(ref PartialArray<T> other)
        {
            if (other.Capacity < this.Count)
            {
                other._array = new T[this.Count];
            }

            System.Array.Copy(_array, other._array, this.Count);
            other.Count = this.Count;
            other.IsStaticSize = this.IsStaticSize;
        }

        public T[] ToArray()
        {
            if (this.Count == 0)
            {
                return EmptyArray<T>.Instance;
            }

            T[] array = new T[this.Count];
            System.Array.Copy(_array, array, this.Count);
            return array;
        }

        public void WriteBinary(BinaryWriter w)
        {
            w.WritePrimitiveArray(_array, 0, this.Count);
        }

        public void ReadBinary(BinaryReader r)
        {
            _array = r.ReadPrimitiveArray<T>();
            this.Count = _array.Length;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(_array, Count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(_array, Count);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _array;
            private readonly int _count;
            private int _current;

            public Enumerator(T[] array, int count)
            {
                _array = array;
                _count = count;
                _current = -1;
            }

            public T Current => _array[_current];

            object IEnumerator.Current => _array[_current];

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                _current++;
                return _current < _count;
            }

            public void Reset()
            {
                _current = -1;
            }
        }
    }
}

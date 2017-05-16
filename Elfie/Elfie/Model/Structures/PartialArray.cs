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
    /// <summary>
    ///  PartialArray is a replacement to List&lt;T&gt; which uses less memory.
    ///  Use the default constructor to build a reusable array which is extremely
    ///  cheap to clear and re-use between loop iterations.
    /// </summary>
    /// <typeparam name="T">Type of items</typeparam>
    public class PartialArray<T> : IColumn, IBinarySerializable, IEnumerable<T>
    {
        private T[] _array;
        public bool IsStaticSize { get; private set; }
        public int Count { get; private set; }

        /// <summary>
        ///  Create an empty, resizable PartialArray.
        /// </summary>
        public PartialArray() : this(0, false)
        { }

        /// <summary>
        ///  Construct a partial array with a specific initial size.
        /// </summary>
        /// <param name="capacity">Initital Capacity</param>
        /// <param name="isStaticSize">True to prevent resizing, False to grow when needed</param>
        public PartialArray(int capacity, bool isStaticSize = true)
        {
            _array = new T[capacity];
            this.Count = 0;
            this.IsStaticSize = isStaticSize;
        }

        /// <summary>
        ///  Construct a PartialArray wrapper around an existing array.
        /// </summary>
        /// <param name="array">T[] to wrap</param>
        /// <param name="currentCount">Number of valid elements already in array</param>
        /// <param name="isStaticSize">True to prevent resizing, False to grow when needed</param>
        public PartialArray(T[] array, int currentCount = 0, bool isStaticSize = true)
        {
            _array = array;
            this.Count = currentCount;
            this.IsStaticSize = isStaticSize;
        }

        /// <summary>
        ///  Get/Set the item at the given array index.
        /// </summary>
        /// <param name="index">Index for which to get/set item.</param>
        /// <returns>Value at Index</returns>
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

        /// <summary>
        ///  Add the default value to the array.
        /// </summary>
        public void Add()
        {
            this.Add(default(T));
        }

        /// <summary>
        ///  Set the count to a specific value, copying existing items if any
        ///  and defaulting new values.
        /// </summary>
        /// <param name="count">Count to set</param>
        public void SetCount(int count)
        {
            T[] newArray = new T[(int)(count)];

            if (this.Count > 0)
            {
                Array.Copy(_array, newArray, Math.Min(count, this.Count));
            }

            _array = newArray;
            this.Count = count;
        }

        /// <summary>
        ///  Add an item to the array. Resize the array if full.
        /// </summary>
        /// <param name="item">Value to add to array</param>
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

        /// <summary>
        ///  Clear the PartialArray. Extremely inexpensive.
        /// </summary>
        public void Clear()
        {
            this.Count = 0;
        }

        /// <summary>
        ///  Sort values in the array with the given IComparer.
        /// </summary>
        /// <param name="comparer">Comparer to sort with</param>
        public void Sort(IComparer<T> comparer)
        {
            System.Array.Sort<T>(_array, 0, this.Count, comparer);
        }

        /// <summary>
        ///  Sort a pair of Arrays (keys and values). The keys are sorted and
        ///  the values are reordered so that each key still has the same value.
        /// </summary>
        /// <typeparam name="U">Type of Key items</typeparam>
        /// <typeparam name="V">Type of Value items</typeparam>
        /// <param name="keys">Key Array (will be sorted in order)</param>
        /// <param name="items">Value array (will be reordered to match keys)</param>
        public static void SortKeysAndItems<U, V>(PartialArray<U> keys, PartialArray<V> items)
        {
            if (keys.Count != items.Count) throw new InvalidOperationException("Can't sort key and item arrays of different lengths.");
            Array.Sort(keys._array, items._array, 0, keys.Count);
        }

        /// <summary>
        ///  Current PartialArray capacity.
        /// </summary>
        public int Capacity
        {
            get
            {
                if (_array == null) return 0;
                return _array.Length;
            }
        }

        /// <summary>
        ///  Returns true if the array is full (and not resizable).
        /// </summary>
        public bool IsFull
        {
            get { return this.IsStaticSize && (_array == null || _array.Length == this.Count); }
        }

        /// <summary>
        ///  Copy a PartialArray into another one.
        /// </summary>
        /// <param name="other">PartialArray to copy values to</param>
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

        /// <summary>
        ///  Copy PartialArray values to a new array. Use to get a persistent
        ///  copy if the PartialArray will be cleared and reused.
        /// </summary>
        /// <returns></returns>
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

        public void ConvertToImmutable()
        {
            // By default, don't do anything
        }

        /// <summary>
        ///  Write the PartialArray elements (only the filled portion) to a binary file.
        ///  Works for primitive types (float, int, byte, ...) only.
        ///  
        ///  Extremely fast - only one Write call is needed for the whole array.
        /// </summary>
        /// <param name="w">BinaryWriter to write to</param>
        public void WriteBinary(BinaryWriter w)
        {
            if (_array == null) _array = EmptyArray<T>.Instance;
            w.WritePrimitiveArray(_array, 0, this.Count);
        }

        /// <summary>
        ///  Read a PartialArray from a binary file.
        ///  Works for primitive types only.
        ///  
        ///  Extremely fast - only one Read call is needed for the whole array.
        /// </summary>
        /// <param name="r">BinaryReader to read from</param>
        public void ReadBinary(BinaryReader r)
        {
            _array = r.ReadPrimitiveArray<T>();
            this.Count = _array.Length;
        }

        /// <summary>
        ///  Get a typed enumerator for this PartialArray. Does not allocate (enumerator is struct).
        /// </summary>
        /// <returns>Enumerator for this array</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(_array, Count);
        }

        /// <summary>
        ///  Get an untyped enumerator for this PartialArray. Does not allocate (enumerator is struct).
        ///  PERFORMANCE: Enumerating array untyped will box every element.
        /// </summary>
        /// <returns>Enumerator for this array</returns>
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
            { }

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

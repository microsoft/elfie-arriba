// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace XForm
{
    /// <summary>
    ///  Dictionary5 is a Dictionary using the Robin Hood hashing strategy.
    ///  It provides with fast insert and lookup performance with much lower memory use than .NET Dictionary.
    ///  
    ///  It adds one byte of overhead per item over the key and value sizes, compared to 8 bytes.
    ///  It resizes less each time when large to reduce overhead.
    /// </summary>
    /// <typeparam name="T">Type of Dictionary keys</typeparam>
    /// <typeparam name="U">Type of Dictionary values</typeparam>
    public class Dictionary5<T, U> : HashCore
    {
        private IEqualityComparer<T> _comparer;
        private T[] _keys;
        private U[] _values;

        private T _currentKey;
        private U _currentValue;

        public Dictionary5(IEqualityComparer<T> comparer, int initialCapacity = -1)
        {
            _comparer = comparer;
            Reset(HashCore.SizeForCapacity(initialCapacity));
        }

        private uint HashCurrent()
        {
            return unchecked((uint)_comparer.GetHashCode(_currentKey));
        }

        #region Base Overrides
        protected override void Reset(int size)
        {
            base.Reset(size);
            _keys = new T[size];
            _values = new U[size];
        }

        protected override bool EqualsCurrent(uint index)
        {
            return _comparer.Equals(_keys[index], _currentKey);
        }

        protected override void SwapWithCurrent(uint index, SwapType swapType)
        {
            T swapKey = _keys[index];
            U swapValue = _values[index];

            _keys[index] = _currentKey;
            _values[index] = _currentValue;

            _currentKey = swapKey;
            _currentValue = swapValue;
        }

        protected override void Expand()
        {
            // Save the current Keys/Values/Metadata
            T[] oldKeys = _keys;
            U[] oldValues = _values;
            byte[] oldMetaData = this.Metadata;

            // Expand the table
            Reset(HashCore.ResizeToSize(_keys.Length));

            // Add items to the enlarged table
            for (int i = 0; i < oldMetaData.Length; ++i)
            {
                if (oldMetaData[i] > 0) Add(oldKeys[i], oldValues[i]);
            }
        }

        /// <summary>
        ///  Remove all items from the Dictionary, retaining the allocated size.
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            Array.Clear(_keys, 0, _keys.Length);
            Array.Clear(_values, 0, _values.Length);
        }
        #endregion

        #region Public Members
        public U this[T key]
        {
            get
            {
                _currentKey = key;
                int bucket = this.IndexOf(HashCurrent());
                if (bucket == -1) throw new KeyNotFoundException();
                return _values[bucket];
            }

            set
            {
                Add(key, value);
            }
        }

        public bool TryGetValue(T key, out U value)
        {
            _currentKey = key;
            int bucket = this.IndexOf(HashCurrent());

            if (bucket == -1)
            {
                value = default(U);
                return false;
            }
            else
            {
                value = _values[bucket];
                return true;
            }
        }

        /// <summary>
        ///  Return whether this Dictionary contains the given key.
        /// </summary>
        /// <param name="key">Value to find</param>
        /// <returns>True if in set, False otherwise</returns>
        public bool ContainsKey(T key)
        {
            _currentKey = key;
            return this.IndexOf(HashCurrent()) != -1;
        }

        /// <summary>
        ///  Add the given value to the set.
        /// </summary>
        /// <param name="key">Key to add</param>
        /// <param name="key">Value to add</param>
        public void Add(T key, U value)
        {
            _currentKey = key;
            _currentValue = value;

            uint hash = HashCurrent();
            if (!this.Add(hash))
            {
                Expand();

                _currentKey = key;
                _currentValue = value;
                this.Add(hash);
            }
        }

        /// <summary>
        ///  Remove the given key from the Dictionary.
        /// </summary>
        /// <param name="key">Value to remove</param>
        /// <returns>True if removed, False if not found</returns>
        public bool Remove(T key)
        {
            _currentKey = key;
            int index = this.IndexOf(HashCurrent());
            if (index == -1) return false;

            base.Remove(index);
            _keys[index] = default(T);
            _values[index] = default(U);
            return true;
        }

        public IEnumerable<T> AllKeys
        {
            get
            {
                for (int index = 0; index < _keys.Length; ++index)
                {
                    if (this.Metadata[index] > 0)
                    {
                        yield return _keys[index];
                    }
                }
            }
        }
        #endregion
    }
}

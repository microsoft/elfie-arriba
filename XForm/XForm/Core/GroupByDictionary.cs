// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm
{
    /// <summary>
    ///  GroupByDictionary handles one or more key columns. It assigns a new ascending index to each new
    ///  combination of keys seen.
    /// </summary>
    public class GroupByDictionary : HashCore
    {
        private ColumnDetails[] _keyColumns;
        private IDictionaryColumn[] _keys;

        private int _uniqueValueCount;

        private int[] _assignedIndices;
        private int _currentAssignedIndex;

        private int _currentRowAdding;
        private int[] _currentAddedArrayIndices;

        public GroupByDictionary(ColumnDetails[] keyColumns, int initialCapacity = -1)
        {
            _keyColumns = keyColumns;

            // Create a strongly typed column for each key
            _keys = new IDictionaryColumn[keyColumns.Length];
            for (int i = 0; i < keyColumns.Length; ++i)
            {
                _keys[i] = (IDictionaryColumn)Allocator.ConstructGenericOf(typeof(DictionaryColumn<>), keyColumns[i].Type);
            }

            // Allocate the arrays for the keys and values themselves
            Reset(HashCore.SizeForCapacity(initialCapacity));
        }

        public XArray FindOrAdd(XArray[] keys)
        {
            if (keys.Length != _keys.Length) throw new ArgumentOutOfRangeException("keys.Length");
            int rowCount = keys[0].Count;

            // Give the arrays to the keys columns
            SetCurrentArrays(keys);

            // Build an array to contain the found (or added) index for each key
            Allocator.AllocateToSize(ref _currentAddedArrayIndices, rowCount);

            for (uint rowIndex = 0; rowIndex < rowCount; ++rowIndex)
            {
                // Set values to insert as current
                SetCurrent(rowIndex);

                // Get the hash of the row
                uint hash = HashCurrent();

                // Add the new item
                if (!this.Add(hash))
                {
                    Expand();

                    // Reset current to the xarray we're trying to add
                    SetCurrentArrays(keys);
                    SetCurrent(rowIndex);
                    Add(hash);
                }
            }

            // Return the found or assigned indices per row
            return XArray.All(_currentAddedArrayIndices, rowCount);
        }

        private void FindOrAdd(XArray[] keys, XArray indices)
        {
            if (keys.Length != _keys.Length) throw new ArgumentOutOfRangeException("keys.Length");
            int rowCount = keys[0].Count;

            // Give the arrays to the keys columns
            SetCurrentArrays(keys);

            int[] indicesArray = (int[])indices.Array;

            for (uint rowIndex = 0; rowIndex < rowCount; ++rowIndex)
            {
                // Set values to insert as current
                SetCurrent(rowIndex, indicesArray[indices.Index((int)rowIndex)]);

                // Get the hash of the row
                uint hash = HashCurrent();

                // Add the new item
                if (!this.Add(hash))
                {
                    Expand();

                    // Reset current to the xarray we're trying to add
                    SetCurrentArrays(keys);
                    SetCurrent(rowIndex, indicesArray[indices.Index((int)rowIndex)]);
                    Add(hash);
                }
            }
        }

        protected override void Reset(int size)
        {
            base.Reset(size);

            for (int i = 0; i < _keys.Length; ++i)
            {
                _keys[i].Reset(size);
            }

            Allocator.AllocateToSize(ref _assignedIndices, size);
        }

        private void SetCurrentArrays(XArray[] keys)
        {
            for (int keyIndex = 0; keyIndex < keys.Length; ++keyIndex)
            {
                _keys[keyIndex].SetArray(keys[keyIndex]);
            }
        }

        private void SetCurrent(uint index)
        {
            // Set the row in each XArray as current
            for (int keyIndex = 0; keyIndex < _keys.Length; ++keyIndex)
            {
                _keys[keyIndex].SetCurrent(index);
            }

            // Set the value to write (if these keys aren't already found)
            _currentAssignedIndex = _uniqueValueCount;

            // Set the row in the input we're finding the index for
            _currentRowAdding = (int)index;
        }

        private void SetCurrent(uint index, int assignedIndex)
        {
            // Set the row in each XArray as current
            for (int keyIndex = 0; keyIndex < _keys.Length; ++keyIndex)
            {
                _keys[keyIndex].SetCurrent(index);
            }

            // Set the value to write (resize)
            _currentAssignedIndex = assignedIndex;

            // This is a resize, so we're not tracking indices of inserted values
            _currentRowAdding = -1;
        }

        private uint HashCurrent()
        {
            int hash = 0;

            for (int keyIndex = 0; keyIndex < _keys.Length; ++keyIndex)
            {
                hash = _keys[keyIndex].HashCurrent(hash);
            }

            return unchecked((uint)hash);
        }

        protected override bool EqualsCurrent(uint index)
        {
            bool equals = true;
            for (int i = 0; equals == true && i < _keys.Length; ++i)
            {
                equals &= _keys[i].EqualsCurrent(index);
            }
            return equals;
        }

        protected override void SwapWithCurrent(uint index, SwapType swapType)
        {
            // If the value being swapped in is one we're inserting, record the index found or assigned
            if (_currentRowAdding >= 0)
            {
                if (swapType == SwapType.Match)
                {
                    _currentAddedArrayIndices[_currentRowAdding] = _assignedIndices[index];
                }
                else
                {
                    _currentAddedArrayIndices[_currentRowAdding] = _uniqueValueCount++;
                }

                _currentRowAdding = -1;
            }

            // If this wasn't a match (key already found), swap in the keys and (new) index
            if (swapType != SwapType.Match)
            {
                // If this is a swap, write all values
                for (int i = 0; i < _keys.Length; ++i)
                {
                    _keys[i].SwapCurrent(index);
                }

                // Swap the assigned index
                int assignedIndexSwap = _assignedIndices[index];
                _assignedIndices[index] = _currentAssignedIndex;
                _currentAssignedIndex = assignedIndexSwap;
            }
        }

        protected override void Expand()
        {
            // Build a selector of table values which were non-empty
            int[] indices = new int[_assignedIndices.Length];

            byte[] metadata = this.Metadata;
            int count = 0;
            for (int i = 0; i < indices.Length; ++i)
            {
                if (metadata[i] != 0) indices[count++] = i;
            }

            // Save the old keys, ranks, and row indices in arrays
            XArray[] keyArrays = new XArray[_keys.Length];
            for (int i = 0; i < _keys.Length; ++i)
            {
                keyArrays[i] = XArray.All(_keys[i].Values).Reselect(ArraySelector.Map(indices, count));
            }

            XArray indicesArray = XArray.All(_assignedIndices).Reselect(ArraySelector.Map(indices, count));

            // Expand the table
            Reset(HashCore.ResizeToSize(_assignedIndices.Length));

            // Add items to the enlarged table
            FindOrAdd(keyArrays, indicesArray);
        }

        public XArray[] DistinctKeys()
        {
            // Build a map from each assigned index to the hash bucket containing it
            int[] indicesInOrder = new int[Count];

            byte[] metadata = this.Metadata;
            for (int i = 0; i < metadata.Length; ++i)
            {
                if (metadata[i] != 0) indicesInOrder[_assignedIndices[i]] = i;
            }

            // Get the array for each key and reselect into assigned order
            XArray[] keyArrays = new XArray[_keys.Length];
            for (int i = 0; i < _keys.Length; ++i)
            {
                keyArrays[i] = XArray.All(_keys[i].Values).Reselect(ArraySelector.Map(indicesInOrder, Count));
            }

            return keyArrays;
        }
    }
}

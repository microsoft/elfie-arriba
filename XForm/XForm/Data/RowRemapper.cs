// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using XForm.Data;

namespace XForm.Transforms
{
    /// <summary>
    ///  RowRemapper is used by row filtering operations.
    ///  The filter computes the rows to include as indices from 0 to Count - 1.
    ///  This class then converts those matching row indices to real indices on the array for each column.
    ///  This is needed because each column from a source can be mapped differently.
    /// </summary>
    public class RowRemapper
    {
        private BitVector _matchVector;
        private int[] _matchIndices;
        private Dictionary<ArraySelector, ArraySelector> _cachedRemappings;

        public RowRemapper()
        {
            _cachedRemappings = new Dictionary<ArraySelector, ArraySelector>();
        }

        public BitVector Vector => _matchVector;

        public void ClearAndSize(int length)
        {
            // Ensure the row index array is large enough
            if (_matchVector == null || _matchVector.Capacity < length) _matchVector = new BitVector(length);

            // Clear previous matches
            _matchVector.None();

            // Clear cached remappings (they will need to be recomputed)
            _cachedRemappings.Clear();
        }

        public int Count => _matchVector.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int index)
        {
            _matchVector.Set(index);
        }

        public void All(int count)
        {
            _matchVector.All(count);
        }

        public DataBatch Remap(DataBatch source, ref int[] remapArray)
        {
            // See if we have the remapping cached already
            ArraySelector cachedMapping;
            if (_cachedRemappings.TryGetValue(source.Selector, out cachedMapping)) return source.Reselect(cachedMapping);

            // Convert the BitVector to indices
            int count = _matchVector.Count;
            Allocator.AllocateToSize(ref _matchIndices, count);
            int start = 0;
            _matchVector.Page(_matchIndices, ref start);

            // Remap the outer selector
            DataBatch remapped = source.Select(ArraySelector.Map(_matchIndices, count), ref remapArray);

            // Cache the remapping
            _cachedRemappings[source.Selector] = remapped.Selector;

            return remapped;
        }
    }
}

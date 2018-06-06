// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

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
        private BitVector _vector;
        private int[] _indices;
        private int _count;

        private bool _indicesFound;
        private int _nextVectorIndex;

        private Dictionary<ArraySelector, ArraySelector> _cachedRemappings;

        public RowRemapper()
        {
            _cachedRemappings = new Dictionary<ArraySelector, ArraySelector>();
        }

        public void SetMatches(BitVector vector, int count = -1)
        {
            _vector = vector;
            _count = (count == -1 ? vector.Count : count);

            // Set that we need indices again starting from the vector start
            _indicesFound = false;
            _nextVectorIndex = 0;

            // Clear cached remappings (they will need to be recomputed)
            _cachedRemappings.Clear();
        }

        public bool NextMatchPage(int countLimit)
        {
            // If we didn't page the previous set, skip past them
            if (!_indicesFound)
            {
                Allocator.AllocateToSize(ref _indices, _count);
                int countFound = _vector.Page(_indices, ref _nextVectorIndex, _count);
                if (countFound != _count) throw new InvalidOperationException($"RowRemapper found {countFound:n0} rows when {_count:n0} expected paging in Vector with {_vector.Count:n0} total matches up to index {_nextVectorIndex:n0}.");
            }

            // Set that we need indices again and the next expected count
            _indicesFound = false;
            _count = countLimit;

            // Clear cached remappings (they will need to be recomputed)
            _cachedRemappings.Clear();

            return _nextVectorIndex != -1;
        }

        public void SetMatches(int[] indices, int count)
        {
            _vector = null;
            _indices = indices;
            _count = count;
            _indicesFound = true;

            // Clear cached remappings (they will need to be recomputed)
            _cachedRemappings.Clear();
        }

        public int Count => _count;

        public XArray Remap(XArray source, ref int[] remapArray)
        {
            // See if we have the remapping cached already
            ArraySelector cachedMapping;
            if (_cachedRemappings.TryGetValue(source.Selector, out cachedMapping))
            {
                return source.Reselect(cachedMapping);
            }

            // Convert the BitVector to indices if we haven't yet (deferred to first column wanting values)
            if (!_indicesFound)
            {
                _indicesFound = true;
                Allocator.AllocateToSize(ref _indices, _count);
                int countFound = _vector.Page(_indices, ref _nextVectorIndex, _count);
                if (countFound != _count) throw new InvalidOperationException($"RowRemapper found {countFound:n0} rows when {_count:n0} expected paging in Vector with {_vector.Count:n0} total matches up to index {_nextVectorIndex:n0}.");
            }

            // Remap the outer selector
            XArray remapped = source.Select(ArraySelector.Map(_indices, _count), ref remapArray);

            // Cache the remapping
            _cachedRemappings[source.Selector] = remapped.Selector;

            return remapped;
        }
    }
}

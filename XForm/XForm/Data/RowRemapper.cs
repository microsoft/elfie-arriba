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
        private BitVector _vector;
        private int[] _indices;
        private int _count;

        private Dictionary<ArraySelector, ArraySelector> _cachedRemappings;

        public RowRemapper()
        {
            _cachedRemappings = new Dictionary<ArraySelector, ArraySelector>();
        }

        public void SetMatches(BitVector vector)
        {
            _vector = vector;
            _count = vector.Count;

            // Clear cached remappings (they will need to be recomputed)
            _cachedRemappings.Clear();
        }

        public void SetMatches(int[] indices, int count)
        {
            _vector = null;
            _indices = indices;
            _count = count;

            // Clear cached remappings (they will need to be recomputed)
            _cachedRemappings.Clear();
        }

        public int Count => _count;

        public DataBatch Remap(DataBatch source, ref int[] remapArray)
        {
            // See if we have the remapping cached already
            ArraySelector cachedMapping;
            if (_cachedRemappings.TryGetValue(source.Selector, out cachedMapping)) return source.Reselect(cachedMapping);

            // Convert the BitVector to indices
            if (_vector != null)
            {
                Allocator.AllocateToSize(ref _indices, _count);
                int start = 0;
                _vector.Page(_indices, ref start);
            }

            // Remap the outer selector
            DataBatch remapped = source.Select(ArraySelector.Map(_indices, _count), ref remapArray);

            // Cache the remapping
            _cachedRemappings[source.Selector] = remapped.Selector;

            return remapped;
        }
    }
}

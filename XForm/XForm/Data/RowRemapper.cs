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
        public int[] MatchingRowIndices;
        public int Count;
        private Dictionary<ArraySelector, int[]> _cachedRemappings;

        public RowRemapper()
        {
            _cachedRemappings = new Dictionary<ArraySelector, int[]>();
        }

        public void ClearAndSize(int length)
        {
            // Ensure the row index array is large enough
            Allocator.AllocateToSize(ref MatchingRowIndices, length);

            // Mark the array empty
            Count = 0;

            // Clear cached remappings (they will need to be recomputed)
            _cachedRemappings.Clear();
        }

        public void Add(int index)
        {
            MatchingRowIndices[Count++] = index;
        }

        public DataBatch Remap(DataBatch source, ref int[] remapArray)
        {
            // The source could be a full Array, Array slice, or Array with indirection indices.
            // The rows matching the filter have been computed as indices from 0 to Count - 1.
            // For each column, we need to translate those indices to real indices on the source array.

            // If the source returned a full array, the row indices of the matches point to the right array elements.
            if (source.Selector.Indices == null && source.Selector.StartIndexInclusive == 0) return DataBatch.Map(source.Array, MatchingRowIndices, Count);

            ArraySelector sourceIndicesIdentity = source.Selector;

            // If we've already remapped for this indices and start index, we can re-use it.
            // Many columns are likely to be mapped in the same one or two ways; this avoids redoing the remap work for every column
            int[] cachedMapping;
            if (_cachedRemappings.TryGetValue(sourceIndicesIdentity, out cachedMapping)) return DataBatch.Map(source.Array, cachedMapping, Count);

            // If we need to remap, translate the 0 to Count - 1 index to a real index in the source array.
            // [Add the StartIndex and look up in the Indices array].
            Allocator.AllocateToSize(ref remapArray, Count);
            for (int i = 0; i < Count; ++i)
            {
                remapArray[i] = source.Index(MatchingRowIndices[i]);
            }

            // Cache this mapping so if other columns use the same indices and offset, we don't redo this work
            _cachedRemappings[sourceIndicesIdentity] = remapArray;

            // Return a DataBatch with the remapped indices using the new array.
            // Note: Even if the source had an offset, the remap array was built starting at index zero, so it doesn't.
            return DataBatch.Map(source.Array, remapArray, Count);
        }
    }
}

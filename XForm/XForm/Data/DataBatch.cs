// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace XForm.Data
{
    /// <summary>
    ///  DataBatch represents the fundamental unit XForm operations apply to -
    ///  a set of rows for a single column in a typed array.
    /// </summary>
    public struct DataBatch
    {
        /// <summary>
        ///  Strongly typed array of raw values for one column for a set of rows.
        ///  Processors may not change the contents of this array.
        /// </summary>
        public Array Array { get; private set; }

        /// <summary>
        ///  Selector represents the specific items in the array to include.
        ///  It may be just a start and end index, or may contain an array of
        ///  indices to represent an out-of-order view on the array.
        /// </summary>
        public ArraySelector Selector { get; private set; }

        /// <summary>
        ///  Return the row count in this batch.
        ///  This may not be the full size of the array, but only this count should be accessed
        /// </summary>
        public int Count => Selector.Count;

        /// <summary>
        ///  Return the real Array index for a "logical row index" from zero to Count - 1.
        /// </summary>
        /// <remarks>
        ///  For optimal performance, checking whether the selector has out-of-order Indices
        ///  should be outside loops. However, this code is inlined and the CPU figures out the
        ///  pattern quickly, so there isn't a branch misprediction penalty.
        /// </remarks>
        /// <param name="logicalRowIndex">Index from Zero to Count-1 of the desired row</param>
        /// <returns>Real Array index of the value for this column for the desired row</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index(int logicalRowIndex)
        {
            return Selector.Index(logicalRowIndex);
        }

        public static DataBatch All(Array array)
        {
            return All(array, array.Length);
        }

        public static DataBatch All(Array array, int length)
        {
            if (length > array.Length) throw new ArgumentOutOfRangeException("length");
            return new DataBatch() { Array = array, Selector = ArraySelector.All(length) };
        }

        public static DataBatch Map(Array array, int[] indices, int length)
        {
            if (length > indices.Length) throw new ArgumentOutOfRangeException("length");
            return new DataBatch() { Array = array, Selector = ArraySelector.Map(indices, length) };
        }

        public static DataBatch Select(Array array, int length, ArraySelector selector)
        {
            if (selector.EndIndexExclusive > array.Length || selector.EndIndexExclusive > length) throw new ArgumentOutOfRangeException("selector");
            return new DataBatch() { Array = array, Selector = selector };
        }

        public DataBatch Slice(int startIndexInclusive, int endIndexExclusive)
        {
            return new DataBatch() { Array = this.Array, Selector = this.Selector.Slice(startIndexInclusive, endIndexExclusive) };
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace XForm.Data
{
    /// <summary>
    ///  DataBatch is the fundamental unit XForm is built on.
    ///  It represents a set of rows for a single column in a strongly typed array,
    ///    which avoids casting, boxing, or copying individual values.    
    ///  It provides indirection on the Array through ArraySelector, allowing filtering, 
    ///    re-ordering, and lookups without copying the raw array.
    ///  It provides null support around non-nullable types (which are faster to read and write)
    ///    via an optional IsNull array.
    ///    
    ///  Usage:
    ///     T[] realArray = (T[])batch.Array;                                                   // Array is of ColumnDetails.Type. Only one cast for the whole array.
    ///     for(int i = 0; i &lt; batch.Count; ++i)                                             // Always loop from zero to batch.Count - 1.
    ///     {
    ///         int realIndex = batch.Index(i);                                                 // Index() is an inlined method which returns the real index of a row
    ///         bool valueIsNull = (batch.IsNull != null &amp;&amp; batch.IsNull[realIndex]);   // IsNull, if provided, indicates whether the row is null
    ///         T rowValue = realArray[realIndex];
    ///     }
    /// </summary>
    public struct DataBatch
    {
        private static bool[] s_NullSingle;

        /// <summary>
        ///  Optional array when DataBatch may contain null values indicating which
        ///  are null. If the array itself is null, none of the values are null.
        ///  Avoids using Nullable which keeps the values back-to-back for bulk serialization.
        /// </summary>
        public bool[] IsNull { get; private set; }

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

        static DataBatch()
        {
            s_NullSingle = new bool[1];
            s_NullSingle[0] = true;
        }

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

        private DataBatch(DataBatch copyFrom)
        {
            this.Array = copyFrom.Array;
            this.IsNull = copyFrom.IsNull;
            this.Selector = copyFrom.Selector;
        }

        /// <summary>
        ///  Build a DataBatch referring to [0, Count) in the array with the optional given null array.
        /// </summary>
        /// <param name="array">Array containing values</param>
        /// <param name="count">Count of valid Array values</param>
        /// <param name="isNullArray">bool[] true for rows which have a null value</param>
        /// <returns>DataBatch wrapping the array from [0, Count)</returns>
        public static DataBatch All(Array array, int count = -1, bool[] isNullArray = null)
        {
            if (count == -1) count = array.Length;
            if (count > array.Length) throw new ArgumentOutOfRangeException("length");
            if (isNullArray != null && count > isNullArray.Length) throw new ArgumentOutOfRangeException("length");

            return new DataBatch() { Array = array, IsNull = isNullArray, Selector = ArraySelector.All(count) };
        }

        /// <summary>
        ///  Build a DataBatch with only the value 'null' for the logical row count.
        /// </summary>
        /// <param name="count">Row Count to return 'null' for</param>
        /// <returns>DataBatch with a null Array and a single value indicating null for everything</returns>
        public static DataBatch Null(Array singleNullValueArray, int count)
        {
            return new DataBatch() { Array = singleNullValueArray, IsNull = s_NullSingle, Selector = ArraySelector.Single(count) };
        }

        /// <summary>
        ///  Build a DataBatch referring to the first element only in an array.
        /// </summary>
        /// <param name="array">Array to use first element from</param>
        /// <returns>DataBatch wrapping first array value as a Single</returns>
        public static DataBatch Single(Array array, int count)
        {
            return new DataBatch() { Array = array, Selector = ArraySelector.Single(count) };
        }

        /// <summary>
        ///  Return a DataBatch matching the rows from this batch identified in an inner selector.
        ///  If this DataBatch already uses shifting or indirection, this will look up the real index
        ///  of each row in the innerSelector.
        /// </summary>
        /// <param name="innerSelector">ArraySelector referring to the logical [0, Count) rows in this DataBatch to return</param>
        /// <param name="remapArray">A buffer to hold remapped indices if they're required</param>
        /// <returns>DataBatch containing the remapped rows identified by the innerSelector on the real array</returns>
        public DataBatch Select(ArraySelector innerSelector, ref int[] remapArray)
        {
            return new DataBatch(this) { Selector = this.Selector.Select(innerSelector, ref remapArray) };
        }

        /// <summary>
        ///  Replace the Selector on this DataBatch.
        ///  This does not merge with an existing selector.
        ///  This is only have to use when the outer DataBatch is definitely not mapped (just created with DataBatch.All)
        ///  or when the selector passed has already been remapped relative to any indirection on this DataBatch.
        /// </summary>
        /// <param name="selector">ArraySelector to *replace* this one with, referring to actual array indices to return.</param>
        /// <returns>DataBatch containing the exact rows the selector specified</returns>
        public DataBatch Reselect(ArraySelector selector)
        {
            return new DataBatch(this) { Selector = selector };
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace XForm.Data
{
    /// <summary>
    ///  XArray is the fundamental unit XForm is built on.
    ///  It represents a set of rows for a single column in a strongly typed array,
    ///    which avoids casting, boxing, or copying individual values.    
    ///  It provides indirection on the Array through ArraySelector, allowing filtering, 
    ///    re-ordering, and lookups without copying the raw array.
    ///  It provides null support around non-nullable types (which are faster to read and write)
    ///    via an optional Nulls array.
    ///    
    ///  Usage:
    ///     T[] realArray = (T[])xarray.Array;                                                   // Array is of ColumnDetails.Type. Only one cast for the whole array.
    ///     for(int i = 0; i &lt; xarray.Count; ++i)                                             // Always loop from zero to xarray.Count - 1.
    ///     {
    ///         int realIndex = xarray.Index(i);                                                 // Index() is an inlined method which returns the real index of a row
    ///         bool valueIsNull = (xarray.HasNulls &amp;&amp; xarray.Nulls[realIndex]);   // IsNull, if provided, indicates whether the row is null
    ///         T rowValue = realArray[realIndex];
    ///     }
    /// </summary>
    public struct XArray
    {
        private static bool[] s_SingleTrue = new bool[1] { true };
        private static bool[] s_SingleFalse = new bool[1] { false };

        public static XArray Empty = new XArray() { Array = null, Selector = ArraySelector.All(0) };

        /// <summary>
        ///  Optional array when XArray may contain null values indicating which
        ///  are null. If the array itself is null, none of the values are null.
        ///  Avoids using Nullable which keeps the values back-to-back for bulk serialization.
        /// </summary>
        public bool[] NullRows { get; private set; }

        /// <summary>
        ///  HasNulls is a more intuitive way to check for whether the XArray can have any null
        ///  values in it. (If the Nulls array itself is null, there definitely aren't any null rows).
        /// </summary>
        public bool HasNulls => (NullRows != null);

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
        ///  Return the row count in this xarray.
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

        private XArray(XArray copyFrom)
        {
            this.Array = copyFrom.Array;
            this.NullRows = copyFrom.NullRows;
            this.Selector = copyFrom.Selector;
        }

        /// <summary>
        ///  Build an XArray referring to [0, Count) in the array with the optional given null array.
        /// </summary>
        /// <param name="array">Array containing values</param>
        /// <param name="count">Count of valid Array values</param>
        /// <param name="nulls">bool[] true for rows which have a null value</param>
        /// <param name="isSingle">True for IsSingleValue arrays, False otherwise</param>
        /// <returns>XArray wrapping the array from [0, Count)</returns>
        public static XArray All(Array array, int count = -1, bool[] nulls = null, bool isSingle = false)
        {
            if(array == null)
            {
                if (count == 0 || count == -1) return XArray.Empty;
                throw new ArgumentNullException("array");
            }

            if (count == -1) count = array.Length;
            if (isSingle == false && count > array.Length) throw new ArgumentOutOfRangeException("length");
            if (isSingle == false && nulls != null && count > nulls.Length) throw new ArgumentOutOfRangeException("length");

            return new XArray() { Array = array, NullRows = nulls, Selector = (isSingle ? ArraySelector.Single(count) : ArraySelector.All(count)) };
        }

        /// <summary>
        ///  Build an XArray with only the value 'null' for the logical row count.
        /// </summary>
        /// <param name="count">Row Count to return 'null' for</param>
        /// <returns>XArray with a null Array and a single value indicating null for everything</returns>
        public static XArray Null(Array singleNullValueArray, int count)
        {
            return new XArray() { Array = singleNullValueArray, NullRows = s_SingleTrue, Selector = ArraySelector.Single(count) };
        }

        /// <summary>
        ///  Build an XArray with only the value 'true' for the logical row count.
        /// </summary>
        /// <param name="count">Number of rows in result XArray</param>
        /// <returns>XArray with a single value true for everything</returns>
        public static XArray AllTrue(int count)
        {
            return new XArray() { Array = s_SingleTrue, NullRows = null, Selector = ArraySelector.Single(count) };
        }

        /// <summary>
        ///  Build an XArray with only the value 'false' for the logical row count.
        /// </summary>
        /// <param name="count">Number of rows in result XArray</param>
        /// <returns>XArray with a single value false for everything</returns>
        public static XArray AllFalse(int count)
        {
            return new XArray() { Array = s_SingleFalse, NullRows = null, Selector = ArraySelector.Single(count) };
        }

        /// <summary>
        ///  Build an XArray referring to the first element only in an array.
        /// </summary>
        /// <param name="array">Array to use first element from</param>
        /// <returns>XArray wrapping first array value as a Single</returns>
        public static XArray Single(Array array, int count)
        {
            return new XArray() { Array = array, Selector = ArraySelector.Single(count) };
        }

        /// <summary>
        ///  Return an XArray matching the rows from this xarray identified in an inner selector.
        ///  If this XArray already uses shifting or indirection, this will look up the real index
        ///  of each row in the innerSelector.
        /// </summary>
        /// <param name="innerSelector">ArraySelector referring to the logical [0, Count) rows in this XArray to return</param>
        /// <param name="remapArray">A buffer to hold remapped indices if they're required</param>
        /// <returns>XArray containing the remapped rows identified by the innerSelector on the real array</returns>
        public XArray Select(ArraySelector innerSelector, ref int[] remapArray)
        {
            return new XArray(this) { Selector = this.Selector.Select(innerSelector, ref remapArray) };
        }

        /// <summary>
        ///  Return an XArray matching the slice of rows from the current one specified.
        /// </summary>
        /// <param name="startIndexInclusive">Index of first row to include [ex: 10 to skip first 10 rows]</param>
        /// <param name="endIndexExclusive">Index of first row to exclude [ex: 20 to get rows before index 20]</param>
        /// <returns>XArray containing the slice of rows specified</returns>
        public XArray Slice(int startIndexInclusive, int endIndexExclusive)
        {
            return new XArray(this) { Selector = this.Selector.Slice(startIndexInclusive, endIndexExclusive) };
        }

        /// <summary>
        ///  Replace the Selector on this XArray.
        ///  This does not merge with an existing selector.
        ///  This is only safe to use when the selector indices are relative to the real array positions and not any
        ///  indexed positions from this ArraySelector.
        /// </summary>
        /// <param name="selector">ArraySelector to *replace* this one with, referring to actual array indices to return.</param>
        /// <returns>XArray containing the exact rows the selector specified</returns>
        public XArray Reselect(ArraySelector selector)
        {
            return new XArray(this) { Selector = selector };
        }

        /// <summary>
        ///  Replace the values in an XArray and return it with the same selector
        /// </summary>
        /// <param name="other">Replacement Array to use</param>
        /// <returns>XArray with values replaced and Selector the same</returns>
        public XArray ReplaceValues(XArray other)
        {
            return ReplaceValues(other, other.NullRows);
        }

        /// <summary>
        ///  Replace the values in an XArray and return it with the same selector
        /// </summary>
        /// <param name="other">Replacement Array to use</param>
        /// <returns>XArray with values replaced and Selector the same</returns>
        public XArray ReplaceValues(XArray other, bool[] nulls)
        {
            if (other.Selector.IsSingleValue)
            {
                return new XArray(this) { Array = other.Array, NullRows = nulls, Selector = ArraySelector.Single(this.Count) };
            }
            else
            {
                return new XArray(this) { Array = other.Array, NullRows = nulls };
            }
        }

        /// <summary>
        ///  Replace the values in an XArray and return it with the same selector
        /// </summary>
        /// <param name="other">Replacement Array to use</param>
        /// <param name="nulls">Replacement nulls array to use</param>
        /// <returns>XArray with values replaced and Selector the same</returns>
        public XArray ReplaceValues(Array other, bool[] nulls = null)
        {
            return new XArray(this) { Array = other, NullRows = nulls };
        }

        /// <summary>
        ///  Return a copy of this XArray with no logical nulls (just the underlying values).
        /// </summary>
        /// <returns>XArray with no null array</returns>
        internal XArray WithoutNulls()
        {
            if (!this.HasNulls) return this;
            return new XArray(this) { NullRows = null };
        }

        /// <summary>
        ///  Remap the IsNull array from the source XArray, if any, to a non-indexed array.
        ///  Used when the values in the XAray were converted into an in-order array but IsNull
        ///  from the source needs to be preserved.
        /// </summary>
        /// <param name="array">XArray to remap nulls from</param>
        /// <param name="remapArray">bool[] to use to remap Nulls values, if needed</param>
        /// <returns>Nulls array to use in returned XArray</returns>
        public static bool[] RemapNulls(XArray array, ref bool[] remapArray)
        {
            // If there were no source nulls, there are none for the output
            if (!array.HasNulls) return null;

            // If the source isn't indexed or shifted, the Nulls array may be reused
            if (array.Selector.Indices == null && array.Selector.StartIndexInclusive == 0) return array.NullRows;

            // Otherwise, we must remap nulls
            Allocator.AllocateToSize(ref remapArray, array.Count);

            bool areAnyNulls = false;
            for (int i = 0; i < array.Count; ++i)
            {
                areAnyNulls |= (remapArray[i] = array.NullRows[array.Index(i)]);
            }

            return (areAnyNulls ? remapArray : null);
        }

        /// <summary>
        ///  Convert an XArray which uses indices into a contiguous, zero-based array.
        ///  This allows using (fast) Reselect once converted.
        /// </summary>
        /// <param name="array">Buffer to use to write contiguous values</param>
        /// <param name="nulls">Buffer to use for new null array</param>
        /// <returns>XArray of values written contiguously</returns>
        public XArray ToContiguous<T>(ref T[] array, ref bool[] nulls)
        {
            // If this XArray isn't shifted or indirected, we can use it as-is
            if (this.Selector.Indices == null && this.Selector.StartIndexInclusive == 0) return this;

            T[] thisArray = (T[])this.Array;
            Allocator.AllocateToSize(ref array, this.Count);

            if (!this.HasNulls)
            {
                for (int i = 0; i < this.Count; ++i)
                {
                    int index = this.Index(i);
                    array[i] = thisArray[index];
                }

                return XArray.All(array, this.Count);
            }
            else
            {
                Allocator.AllocateToSize(ref nulls, this.Count);

                for (int i = 0; i < this.Count; ++i)
                {
                    int index = this.Index(i);
                    array[i] = thisArray[index];
                    nulls[i] = this.NullRows[index];
                }

                return XArray.All(array, this.Count, nulls);
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is XArray)) return false;

            XArray other = (XArray)obj;
            return other.Array == this.Array && other.NullRows == this.NullRows && other.Selector == this.Selector;
        }

        public override int GetHashCode()
        {
            return (this.Array?.GetHashCode() ?? 0) ^ (this.NullRows?.GetHashCode() ?? 0) ^ (this.Selector?.GetHashCode() ?? 0);
        }
    }
}

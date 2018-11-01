// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace XForm.Data
{
    /// <summary>
    ///  ArraySelectors describe the items in an associated XArray which each logical row
    ///  corresponds to.
    ///  
    ///  Selectors can specify:
    ///    - The whole array (StartIndexInclusive = 0, EndIndexExclusive = Length - 1)
    ///    - An array slice (StartIndexInclusive = index, EndIndexExclusive = length)
    ///    - A single constant value for all rows (IsSingleValue = true)
    ///    - Indirect indices into an array (Indices != null)
    ///  
    ///  The ArraySelector design allows the pipeline to use the raw array of values read in
    ///  without copying even as the rows are filtered, joined, sorted, and so on.
    /// </summary>
    public class ArraySelector
    {
        /// <summary>
        ///  The index in the array of the value for each row in the set.
        ///  Null Indices means each index in the array should be processed in turn.
        ///  Processors may not change the contents of this array.
        /// </summary>
        public int[] Indices { get; private set; }

        /// <summary>
        ///  The index in the indices array (if non-null) or the raw array to start with.
        /// </summary>
        public int StartIndexInclusive { get; private set; }

        /// <summary>
        ///  The index in the indices array (if non-null) or the raw array to stop before.
        /// </summary>
        public int EndIndexExclusive { get; private set; }

        /// <summary>
        ///  IsSingleValue denotes whether this xarray contains a single value for each logical row.
        ///  SingleValue arrays are used for constants or column rows which all happen to share one value.
        /// </summary>
        public bool IsSingleValue { get; private set; }

        public int Count => EndIndexExclusive - StartIndexInclusive;

        public ArraySelector()
        { }

        private ArraySelector(ArraySelector copyFrom)
        {
            this.Indices = copyFrom.Indices;
            this.StartIndexInclusive = copyFrom.StartIndexInclusive;
            this.EndIndexExclusive = copyFrom.EndIndexExclusive;
            this.IsSingleValue = copyFrom.IsSingleValue;
        }

        /// <summary>
        ///  Single is a static selector pointing to the first value of a one element array only
        /// </summary>
        public static ArraySelector Single(int count)
        {
            return new ArraySelector() { IsSingleValue = true, StartIndexInclusive = 0, EndIndexExclusive = count };
        }

        /// <summary>
        ///  Build a selector for [0, count) in an array.
        /// </summary>
        /// <param name="count">Count of rows in array to include</param>
        /// <returns>ArraySelector for [0, count)</returns>
        public static ArraySelector All(int count)
        {
            return new ArraySelector() { Indices = null, StartIndexInclusive = 0, EndIndexExclusive = count };
        }

        public static ArraySelector Map(int[] indices, int length)
        {
            return new ArraySelector() { Indices = indices, StartIndexInclusive = 0, EndIndexExclusive = length };
        }

        public ArraySelector Slice(int startIndexInclusive, int endIndexExclusive)
        {
            // Get the slice relative to the current offsets
            int shiftedStart = this.StartIndexInclusive + startIndexInclusive;
            int shiftedEnd = shiftedStart + (endIndexExclusive - startIndexInclusive);

            // Validate the Slice is within bounds of the outer ArraySelector
            if (shiftedStart < this.StartIndexInclusive || shiftedStart > this.EndIndexExclusive)
            {
                throw new ArgumentOutOfRangeException("startIndexInclusive");
            }

            if (shiftedEnd < this.StartIndexInclusive || shiftedEnd > this.EndIndexExclusive)
            {
                throw new ArgumentOutOfRangeException("endIndexExclusive");
            }

            return new ArraySelector(this) { StartIndexInclusive = shiftedStart, EndIndexExclusive = shiftedEnd };
        }

        public ArraySelector Select(ArraySelector inner, ref int[] remapArray)
        {
            // This selector could refer to a full array [0, Count), a slice [Start, End), or indirection indices.

            // If this is a single value, return the requested count of copies of it
            if (this.IsSingleValue) return ArraySelector.Single(inner.Count);

            // If this selector is not shifted or indirected, the inner selector can be used as-is
            if (this.Indices == null && this.StartIndexInclusive == 0) return inner;

            // If the inner selector specifies just an index and length, we can shift our index and length
            if (inner.Indices == null)
            {
                ArraySelector newSelector = new ArraySelector(this);
                newSelector.StartIndexInclusive += inner.StartIndexInclusive;
                newSelector.EndIndexExclusive = newSelector.StartIndexInclusive + inner.Count;
                return newSelector;
            }

            // Otherwise, we need to remap the indices in the inner array to ones in the real array
            Allocator.AllocateToSize(ref remapArray, inner.Count);
            for (int i = 0; i < inner.Count; ++i)
            {
                remapArray[i] = Index(inner.Index(i));
            }

            return new ArraySelector() { StartIndexInclusive = 0, EndIndexExclusive = inner.Count, Indices = remapArray };
        }

        public ArraySelector NextPage(int totalCount, int desiredCount)
        {
            int startIndex = this.EndIndexExclusive;
            int endIndex = Math.Min(startIndex + desiredCount, totalCount);
            return new ArraySelector(this) { StartIndexInclusive = startIndex, EndIndexExclusive = endIndex };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index(int zeroSpaceIndex)
        {
            if (this.IsSingleValue) return 0;
            int realIndex = zeroSpaceIndex + this.StartIndexInclusive;
            if (this.Indices != null) realIndex = this.Indices[realIndex];
            return realIndex;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ArraySelector)) return false;
            return Equals((ArraySelector)obj);
        }

        public bool Equals(ArraySelector other)
        {
            if (other == null) return false;
            return this.Indices == other.Indices && this.StartIndexInclusive == other.StartIndexInclusive && this.EndIndexExclusive == other.EndIndexExclusive;
        }

        public override int GetHashCode()
        {
            int code = this.StartIndexInclusive.GetHashCode() ^ this.EndIndexExclusive.GetHashCode();
            if (this.Indices != null) code ^= this.Indices.GetHashCode();
            return code;
        }
    }
}

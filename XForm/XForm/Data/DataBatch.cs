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

        public int Count => EndIndexExclusive - StartIndexInclusive;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index(int zeroSpaceIndex)
        {
            int realIndex = zeroSpaceIndex + this.StartIndexInclusive;
            if (this.Indices != null) realIndex = this.Indices[realIndex];
            return realIndex;
        }

        public static DataBatch All(Array array)
        {
            return All(array, array.Length);
        }

        public static DataBatch All(Array array, int length)
        {
            if (length > array.Length) throw new ArgumentOutOfRangeException("length");
            return new DataBatch() { Array = array, Indices = null, StartIndexInclusive = 0, EndIndexExclusive = length };
        }

        public static DataBatch Map(Array array, int[] indices, int length)
        {
            if (length > indices.Length) throw new ArgumentOutOfRangeException("length");
            return new DataBatch() { Array = array, Indices = indices, StartIndexInclusive = 0, EndIndexExclusive = length };
        }

        public DataBatch Slice(int startIndexInclusive, int endIndexExclusive)
        {
            if (startIndexInclusive < this.StartIndexInclusive) throw new ArgumentOutOfRangeException("startIndexInclusive");
            if (endIndexExclusive < startIndexInclusive || endIndexExclusive > this.EndIndexExclusive) throw new ArgumentOutOfRangeException("endIndexExclusive");
            return new DataBatch() { Array = this.Array, Indices = this.Indices, StartIndexInclusive = startIndexInclusive, EndIndexExclusive = endIndexExclusive };
        }
    }
}

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

        public ArraySelector Selector { get; private set; }

        public int Count => Selector.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index(int zeroSpaceIndex)
        {
            return Selector.Index(zeroSpaceIndex);
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

        public DataBatch Slice(int startIndexInclusive, int endIndexExclusive)
        {
            return new DataBatch() { Array = this.Array, Selector = this.Selector.Slice(startIndexInclusive, endIndexExclusive) };
        }
    }
}

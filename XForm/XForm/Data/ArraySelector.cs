// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace XForm.Data
{
    public struct ArraySelector
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

        public int Count => EndIndexExclusive - StartIndexInclusive;

        public static ArraySelector All(int length)
        {
            return new ArraySelector() { Indices = null, StartIndexInclusive = 0, EndIndexExclusive = length };
        }

        public static ArraySelector Map(int[] indices, int length)
        {
            return new ArraySelector() { Indices = indices, StartIndexInclusive = 0, EndIndexExclusive = length };
        }

        public ArraySelector Slice(int startIndexInclusive, int endIndexExclusive)
        {
            if (startIndexInclusive < this.StartIndexInclusive) throw new ArgumentOutOfRangeException("startIndexInclusive");
            if (endIndexExclusive < startIndexInclusive || endIndexExclusive > this.EndIndexExclusive) throw new ArgumentOutOfRangeException("endIndexExclusive");
            return new ArraySelector() { Indices = this.Indices, StartIndexInclusive = startIndexInclusive, EndIndexExclusive = endIndexExclusive };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index(int zeroSpaceIndex)
        {
            int realIndex = zeroSpaceIndex + this.StartIndexInclusive;
            if (this.Indices != null) realIndex = this.Indices[realIndex];
            return realIndex;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ArraySelector)) return false;
            ArraySelector other = (ArraySelector)obj;
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

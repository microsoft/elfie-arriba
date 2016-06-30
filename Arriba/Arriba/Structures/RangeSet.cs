// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;

using Arriba.Extensions;
using Arriba.Structures;

namespace Arriba.Structures
{
    /// <summary>
    ///  Contains a set of Ranges and the Count of them to allow Splitter to
    ///  easily share a set of results.
    /// </summary>
    public class RangeSet
    {
        private const int MaxRangeSets = 128 * 1024 * 1024;

        public Range[] Ranges;
        public int Count;

        public RangeSet()
        {
            this.Ranges = new Range[ArrayExtensions.MinimumSize];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Range range)
        {
            if (range.Length > 0)
            {
                ArrayExtensions.Resize(ref Ranges, this.Count + 1, MaxRangeSets);
                this.Ranges[this.Count] = range;
                ++this.Count;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int index, int length)
        {
            Add(new Range(index, length));
        }

        public string ToString(byte[] text)
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < this.Count; ++i)
            {
                Range word = this.Ranges[i];

                if (result.Length > 0) result.Append("|");
                result.Append(Encoding.UTF8.GetChars(text, word.Index, word.Length));
            }

            return result.ToString();
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Extensions;

namespace Arriba.Structures
{
    /// <summary>
    ///  Range represents a subsection of an array. It is used by the string
    ///  splitters to return the set of words found as Ranges.
    /// </summary>
    public struct Range
    {
        public int Index;
        public int Length;

        public Range(int index, int length)
        {
            this.Index = index;
            this.Length = length;
        }

        public override string ToString()
        {
            return StringExtensions.Format("({0}, {1})", this.Index, this.Length);
        }
    }
}

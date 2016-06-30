// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.CodeAnalysis.Elfie.Model.Structures
{
    public struct Range
    {
        public int Start { get; private set; }
        public int End { get; private set; }

        public static Range Empty = new Range(0, -1);
        public static Range Max = new Range(int.MinValue, int.MaxValue);

        public Range(int singleValue)
        {
            this.Start = singleValue;
            this.End = singleValue;
        }

        public Range(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }

        public bool IsEmpty()
        {
            return this.Start > this.End;
        }

        public bool Contains(int value)
        {
            return value >= this.Start && value <= this.End;
        }

        public int Length
        {
            get { return this.End - this.Start + 1; }
        }

        public override string ToString()
        {
            if (this.IsEmpty())
            {
                return "<EMPTY>";
            }
            else if (this.Start == this.End)
            {
                return this.Start.ToString("n0");
            }
            else
            {
                return String.Format("{0:n0}-{1:n0}", this.Start, this.End);
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Extensions;
using Arriba.Structures;

namespace Arriba.Model.Expressions
{
    /// <summary>
    ///  RangeToScan determines the best range of items to scan to find matches for
    ///  a given operator and computes the resulting set.
    /// </summary>
    internal struct RangeToScan
    {
        public int Start;
        public int End;
        public int Count;
        public bool ScanWithinRange;
        public bool NegateResult;

        private void Clear()
        {
            Start = 0;
            End = 0;
            Count = 0;
            ScanWithinRange = false;
            NegateResult = false;
        }

        public static bool TryBuild(Operator op, int firstSortedIndexWithValue, int lastSortedIndexWithValue, int count, ref RangeToScan r)
        {
            r.Clear();
            r.Count = count;
            r.ScanWithinRange = true;

            // If source is empty, range is always empty
            if (count == 0)
            {
                r.Start = -1;
                r.End = -1;
                r.ScanWithinRange = true;
                r.NegateResult = false;
                return true;
            }

            // Set range and negate based on the operator
            if (op == Operator.Equals || op == Operator.NotEquals || op == Operator.Matches || op == Operator.MatchesExact)
            {
                // NOTE: Treat "Matches", "MatchesExact" like "Equals" if an IndexedColumn doesn't pick them up.
                // NOTE: "StartsWith" must be mapped to "Equals" by a caller which also knows to use StartsWith to find the first and last indicies.

                // Scan the item subset containing the value
                r.Start = firstSortedIndexWithValue;
                r.End = lastSortedIndexWithValue;

                // Include the opposite for not equals
                r.NegateResult = (op == Operator.NotEquals);
            }
            else if (op == Operator.LessThan || op == Operator.GreaterThanOrEqual)
            {
                r.Start = 0;

                if (firstSortedIndexWithValue < 0)
                {
                    // If not in array, scan up to (including) where it would've been inserted
                    r.End = ~firstSortedIndexWithValue - 1;
                }
                else
                {
                    // Scan below the first item with the value
                    r.End = firstSortedIndexWithValue - 1;
                }

                // Include the opposite for >=
                r.NegateResult = (op == Operator.GreaterThanOrEqual);
            }
            else if (op == Operator.GreaterThan || op == Operator.LessThanOrEqual)
            {
                r.End = count - 1;

                if (lastSortedIndexWithValue < 0)
                {
                    // If not in array, scan from where it would've been inserted
                    r.Start = ~lastSortedIndexWithValue;
                }
                else
                {
                    // Scan above the last item with the value
                    r.Start = lastSortedIndexWithValue + 1;
                }

                // Include the opposite for <=
                r.NegateResult = (op == Operator.LessThanOrEqual);
            }
            else
            {
                return false;
            }

            // Canonicalize
            if (r.End < 0 || r.Start > r.End)
            {
                // Canonicalize: If the range is empty (Start > End), always report as -1, -1 (leave negate according to operator)
                r.End = -1;
                r.Start = -1;
            }
            else if (r.Start == 0 && r.End == count - 1)
            {
                // Canonicalize: If the range is all items, convert to an empty range and negate it
                r.End = -1;
                r.Start = -1;
                r.ScanWithinRange = false;
                r.NegateResult = !r.NegateResult;
            }

            // Optimize
            if (r.Start >= 0)
            {
                // Optimize: If the range to scan is non-empty and more than half the set, scan the inverse and negate
                int rangeSize = r.End - r.Start + 1;
                if (rangeSize > count / 2)
                {
                    r.ScanWithinRange = false;
                    r.NegateResult = !r.NegateResult;
                }

                if (!r.ScanWithinRange && r.Start < r.End)
                {
                    // Optimize: If we want outside the range and one end is the end, convert to a single range
                    if (r.Start == 0)
                    {
                        r.Start = r.End + 1;
                        r.End = count - 1;
                        r.ScanWithinRange = true;
                    }
                    else if (r.End == count - 1)
                    {
                        r.End = r.Start - 1;
                        r.Start = 0;
                        r.ScanWithinRange = true;
                    }
                }
            }

            return true;
        }

        public void AddMatches(ushort[] sortedIDs, ShortSet matches)
        {
            ShortSet m = matches;

            // If negating, we have to negate in a separate set and then OR to preserve previously set values
            if (this.NegateResult)
            {
                m = new ShortSet(matches.Capacity);
            }

            // Add all items within or around the range as requested
            if (this.End != -1)
            {
                if (this.ScanWithinRange)
                {
                    for (int i = this.Start; i <= this.End; ++i)
                    {
                        m.Add(sortedIDs[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < this.Start; ++i)
                    {
                        m.Add(sortedIDs[i]);
                    }

                    for (int i = this.End + 1; i < this.Count; ++i)
                    {
                        m.Add(sortedIDs[i]);
                    }
                }
            }

            // Negate if requested
            if (this.NegateResult)
            {
                m.Not();
                matches.Or(m);
            }
        }

        public override string ToString()
        {
            if (this.End == -1)
            {
                return (this.NegateResult ? "{ALL}" : "{EMPTY}");
            }
            else
            {
                string format = (this.ScanWithinRange ? "{0}{1}=> <={2}" : "{0}<={1} {2}=>");
                return StringExtensions.Format(format, (this.NegateResult ? "! " : ""), this.Start, this.End);
            }
        }
    }
}

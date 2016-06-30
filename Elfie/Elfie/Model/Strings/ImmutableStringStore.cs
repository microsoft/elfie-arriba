// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Model.Strings
{
    /// <summary>
    ///  ImmutableStringStore contains StringStore values which were previously
    ///  written out and then reloaded. They're kept sorted, four byte aligned,
    ///  tab padded, Cr Lf delimited, in a byte[]. This form is extremely compact
    ///  and allows fast search for values and output to other streams without
    ///  allocation string instances.
    /// </summary>
    internal class ImmutableStringStore : IStringStore
    {
        private const bool IgnoreCase = true;
        private String8Set _sortedExistingValues;

        /// <summary>
        ///  Serialization-only constructor
        /// </summary>
        internal ImmutableStringStore()
        { }

        internal ImmutableStringStore(String8Set sortedExistingValues)
        {
            _sortedExistingValues = sortedExistingValues;
        }

        #region Get Values Out
        public String8 this[int identifier]
        {
            get { return _sortedExistingValues[identifier]; }
        }

        public Range RangeForString(int identifier)
        {
            String8 value = this[identifier];
            String8 valueHere;

            // Find the first matching value
            int start = identifier;
            while (start > 0)
            {
                valueHere = this[start - 1];
                if (valueHere.CompareTo(value, IgnoreCase) != 0) break;
                start--;
            }

            // Find the last matching value
            int end = identifier;
            while (end < _sortedExistingValues.Count - 1)
            {
                valueHere = this[end + 1];
                if (valueHere.CompareTo(value, IgnoreCase) != 0) break;
                end++;
            }

            return new Range(start, end);
        }
        #endregion

        #region Find Values
        public bool TryFindString(string value, out Range matches)
        {
            return TryFindString(String8.Convert(value, new byte[String8.GetLength(value)]), out matches);
        }

        public bool TryFindString(String8 value, out Range matches)
        {
            if (value.IsEmpty())
            {
                matches = Range.Empty;
                return true;
            }

            // Binary search sorted strings for the value
            int min = 0;
            int max = _sortedExistingValues.Count - 1;
            int mid = 0;
            int cmp = 0;
            String8 valueHere = String8.Empty;

            while (min <= max)
            {
                mid = (min + max) / 2;
                valueHere = this[mid];

                cmp = value.CompareTo(valueHere, IgnoreCase);
                if (cmp == 0)
                {
                    // 'value' Found - look for bounds
                    break;
                }
                else if (cmp > 0)
                {
                    // 'value' is later - look after valueHere
                    min = mid + 1;
                }
                else
                {
                    // 'value' is earlier - look before valueHere
                    max = mid - 1;
                }
            }

            // If no match, set both bounds to insertion position
            if (cmp > 0)
            {
                // If 'value' was after last comparison, we'd insert after it
                matches = new Range(mid + 1);
                return false;
            }
            else if (cmp < 0)
            {
                // If 'value' was before last comparison, we'd insert before it
                matches = new Range(mid);
                return false;
            }

            matches = RangeForString(mid);
            return true;
        }

        public bool TryGetRangeStartingWith(String8 prefix, out Range matches)
        {
            if (prefix.IsEmpty())
            {
                matches = Range.Empty;
                return false;
            }

            // Find the index of the prefix (or the first value after where it would be inserted)
            TryFindString(prefix, out matches);

            // Binary search sorted strings for the last value containing the prefix
            int min = matches.End;
            int max = _sortedExistingValues.Count - 1;
            int cmp = 0;
            int lastMatch = 0;
            String8 valueHere = String8.Empty;

            while (min <= max)
            {
                int mid = (min + max) / 2;
                valueHere = this[mid];

                cmp = prefix.CompareAsPrefixTo(valueHere, IgnoreCase);
                if (cmp == 0)
                {
                    // 'prefix' is a prefix - keep looking for last instance
                    lastMatch = mid;
                    min = mid + 1;
                }
                else if (cmp > 0)
                {
                    // 'prefix' is later - look after valueHere
                    min = mid + 1;
                }
                else
                {
                    // 'prefix' is earlier - look before valueHere
                    max = mid - 1;
                }
            }

            // If we found at least one value with the prefix, we were successful
            matches = new Range(matches.Start, lastMatch);
            return lastMatch != 0;
        }
        #endregion

        #region Comparison
        public int CompareValues(int leftIdentifier, int rightIdentifier)
        {
            return leftIdentifier.CompareTo(rightIdentifier);
        }
        #endregion

        #region Serialization
        public int GetSerializationIdentifier(int identifier)
        {
            // No remapping once immutable
            return identifier;
        }

        public void WriteBinary(BinaryWriter w)
        {
            _sortedExistingValues.WriteBinary(w);
        }

        public void ReadBinary(BinaryReader r)
        {
            _sortedExistingValues.ReadBinary(r);
        }
        #endregion

        #region Statistics
        public int Count
        {
            get { return _sortedExistingValues.Count; }
        }

        public long Bytes
        {
            get { return _sortedExistingValues.Bytes; }
        }
        #endregion

        /// <summary>
        ///  Debugging: Used to get length distribution to understand string sizes.
        /// </summary>
        /// <returns></returns>
        internal int[] CountByPaddedLength()
        {
            int maxLength = 0;
            Dictionary<int, int> result = new Dictionary<int, int>();

            for (int i = 0; i < _sortedExistingValues.Count; ++i)
            {
                String8 current = _sortedExistingValues[i];

                int length = current.Length;
                length = length & ~3;
                if (length > maxLength) maxLength = length;

                int count;
                if (!result.TryGetValue(length, out count)) count = 0;

                result[length] = count + 1;
            }

            int[] resultByFourByteLength = new int[maxLength / 4];
            for (int i = 0; i < resultByFourByteLength.Length; ++i)
            {
                int count;
                if (!result.TryGetValue(i * 4, out count)) count = 0;
                resultByFourByteLength[i] = count;
            }

            return resultByFourByteLength;
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Model.Strings
{
    /// <summary>
    ///  MutableStringStore contains new StringStore values which haven't been
    ///  serialized. They're kept in .NET string form and in a Dictionary for
    ///  fast de-duplication.
    ///  
    ///  NOTE: StringStores use complex sorting
    ///   - Inserted case sensitive (to keep each different casing),
    ///   - Sorted case insensitive stable (to keep diffs small),
    ///   - Searched case insensitive (to allow case insensitive search).
    /// </summary>
    internal class MutableStringStore
    {
        private static IComparer<string> s_comparer = new CaseInsensitiveStableComparer();
        private static StringComparison s_comparison = StringComparison.OrdinalIgnoreCase;

        private List<string> _values;
        private Dictionary<string, int> _valueToIdentifier;

        public MutableStringStore()
        {
            _values = new List<string>();

            // NOTE: Insert is case sensitive (so values with different case are preserved).
            _valueToIdentifier = new Dictionary<string, int>();

            FindOrAddString(String.Empty);
        }

        #region Get Values Out
        public String8 this[int identifier]
        {
            get { return String8.Convert(_values[identifier], new byte[String8.GetLength(_values[identifier])]); }
        }
        #endregion

        #region Put Values In
        public int FindOrAddString(String8 value)
        {
            return FindOrAddString(value.ToString());
        }

        public int FindOrAddString(string value)
        {
            if (value == null) value = String.Empty;

            int identifier;

            // If not already added, add to List and Map; identifier is index in array
            if (!_valueToIdentifier.TryGetValue(value, out identifier))
            {
                identifier = _values.Count;
                _values.Add(value);
                _valueToIdentifier[value] = identifier;
            }

            return identifier;
        }
        #endregion

        #region Find Values
        public bool TryFindString(string value, out int identifier)
        {
            return _valueToIdentifier.TryGetValue(value, out identifier);
        }

        public bool TryFindString(String8 value, out int identifier)
        {
            return _valueToIdentifier.TryGetValue(value.ToString(), out identifier);
        }

        public bool TryGetRangeStartingWith(String8 prefix, out int firstIdentifier, out int lastIdentifier)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Comparison
        public int CompareValues(int leftIdentifier, int rightIdentifier)
        {
            return String.Compare(_values[leftIdentifier], _values[rightIdentifier], s_comparison);
        }
        #endregion

        #region Serialization
        public ImmutableStringStore ConvertToImmutable(out int[] identifierToSerializedIdentifier)
        {
            int valueCount = _values.Count;

            // Build an array of string indexes sorted by the strings
            int[] valueIndexesSorted = new int[valueCount];
            for (int i = 0; i < valueCount; ++i)
            {
                valueIndexesSorted[i] = i;
            }

            // Sort index array to point to values in sorted order
            // NOTE: Sort is case insensitive stable (case insensitive search and minimal diff size)
            Array.Sort(_values.ToArray(), valueIndexesSorted, s_comparer);

            // Build a map from the original identifiers (ascending order by when inserted) to the new ones (sorted order) to fix references
            int[] map = new int[valueCount];
            for (int i = 0; i < valueCount; ++i)
            {
                int oldIdentifier = valueIndexesSorted[i];
                int newIdentifier = i;
                map[oldIdentifier] = newIdentifier;
            }

            // Walk in sorted order and determine the byte position where each value will be written
            int totalLength = 0;

            PartialArray<int> positions = new PartialArray<int>(new int[valueIndexesSorted.Length + 1]);
            for (int i = 0; i < valueIndexesSorted.Length; ++i)
            {
                positions.Add(totalLength);

                // Compute space needed for this value and delimiter (\r\n delimited)
                totalLength += String8.GetLength(_values[valueIndexesSorted[i]]) + 2;
            }

            positions.Add(totalLength);

            // Build byte[] with the concatenated values
            byte[] sortedValueBytes = new byte[totalLength];

            int nextWritePosition = 0;
            for (int i = 0; i < valueIndexesSorted.Length; ++i)
            {
                // Copy value to output array
                String8 newValue = String8.Convert(_values[valueIndexesSorted[i]], sortedValueBytes, nextWritePosition);
                nextWritePosition += newValue.Length;

                sortedValueBytes[nextWritePosition] = UTF8.CR;
                sortedValueBytes[nextWritePosition + 1] = UTF8.LF;
                nextWritePosition += 2;
            }

            // Build the ImmutableStore equivalent of this and an array to translate identifiers
            identifierToSerializedIdentifier = map;
            return new ImmutableStringStore(new String8Set(new String8(sortedValueBytes, 0, sortedValueBytes.Length), 2, positions));
        }

        /// <summary>
        ///  CaseInsensitiveStableComparer sorts strings in case insensitive order,
        ///  but keeps different casings of the same word in case sensitive order.
        ///  
        ///  This combination allows case-insensitive binary search of the result
        ///  but is stable so that diffs of similar lists remain small.
        /// </summary>
        private class CaseInsensitiveStableComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                // Compare case insensitive and sort according to those rules [to support case insensitive binary search]
                int cmp = String.Compare(x, y, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;

                // Sort casing differences of the same word in case sensitive order [to minimize diff size]
                return String.Compare(x, y, StringComparison.Ordinal);
            }
        }

        #endregion

        #region IStatistics
        public int Count
        {
            get
            {
                return _values.Count;
            }
        }

        public long Bytes
        {
            get
            {
                long totalSerializedLength = 0;

                for (int i = 0; i < _values.Count; ++i)
                {
                    // Sum the UTF8 length each string will be, plus delimiters, plus the index of each.
                    totalSerializedLength += String8.GetLength(_values[i]) + 2 + 4;
                }

                return totalSerializedLength;
            }
        }
        #endregion
    }
}

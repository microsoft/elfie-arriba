// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model.Strings
{
    /// <summary>
    ///  StringStore contains a set of unique strings and provides callers with
    ///  integer identifiers for them. StringStore serializes to a very compact
    ///  form to drive fast search.
    ///   - Add a string to get an int identifier for it.
    ///   - Use the int to get back to string or write as UTF8 directly.
    ///   - Users must ask for updated identifiers when serializing out.
    ///   - StringStore serializes as readable UTF8 to disk.
    ///   - StringStore deserializes instantly with no memory overhead.
    /// </summary>
    /// <remarks>
    ///  StringStore wraps an ImmutableStringStore for previously loaded values
    ///  and a MutableStringStore for new values. Added values have negative
    ///  identifiers. Existing values have positive ones. Zero is String.Empty.
    /// </remarks>
    public class StringStore : IStringStore, IBinarySerializable
    {
        private ImmutableStringStore _existingValues;
        private MutableStringStore _addedValues;
        private int[] _addedIdentifierToExistingIdentifier;

        public StringStore()
        {
            // Nothing is initialized to start. They're created when needed.
        }

        public void Clear()
        {
            _existingValues = null;
            _addedValues = null;
            _addedIdentifierToExistingIdentifier = null;
        }

        #region Get Strings Out
        public String8 this[int identifier]
        {
            get
            {
                if (identifier == 0) return String8.Empty;

                if (identifier < 0)
                {
                    if (_addedValues != null)
                    {
                        // New identifier, not yet converted
                        return _addedValues[-identifier];
                    }
                    else
                    {
                        // New identifier, conversion happened
                        return _existingValues[_addedIdentifierToExistingIdentifier[-identifier]];
                    }
                }
                else
                {
                    // Existing or remapped identifier
                    return _existingValues[identifier];
                }
            }
        }

        public Range RangeForString(int identifier)
        {
            if (_addedValues != null) throw new NotSupportedException(Resources.ConvertToImmutableRequired);
            return _existingValues.RangeForString(identifier);
        }
        #endregion

        #region Add Values
        public int FindOrAddString(string value)
        {
            // If this value was previously written, return the existing identifier
            Range matches;
            if (_existingValues != null && _existingValues.TryFindString(value, out matches))
            {
                return matches.Start;
            }

            // Otherwise, add to the AddedStrings
            if (_addedValues == null) _addedValues = new MutableStringStore();
            return -_addedValues.FindOrAddString(value);
        }

        public int FindOrAddString(String8 value)
        {
            // If this value was previously written, return the existing identifier
            Range matches;
            if (_existingValues != null && _existingValues.TryFindString(value, out matches))
            {
                return matches.Start;
            }

            // Otherwise, add to the AddedStrings
            if (_addedValues == null) _addedValues = new MutableStringStore();
            return -_addedValues.FindOrAddString(value);
        }
        #endregion

        #region Find Values
        public bool TryFindString(string value, out Range matches)
        {
            // If this value was previously written, return the existing identifier
            if (_existingValues != null && _existingValues.TryFindString(value, out matches))
            {
                return true;
            }

            // If this value was just added, return the new identifier
            int foundIdentifier;
            if (_addedValues != null && _addedValues.TryFindString(value, out foundIdentifier))
            {
                matches = new Range(-foundIdentifier);
                return true;
            }

            matches = Range.Empty;
            return false;
        }

        public bool TryFindString(String8 value, out Range matches)
        {
            // If this value was previously written, return the existing identifier
            if (_existingValues != null && _existingValues.TryFindString(value, out matches))
            {
                return true;
            }

            // If this value was just added, return the new identifier
            int foundIdentifier;
            if (_addedValues != null && _addedValues.TryFindString(value, out foundIdentifier))
            {
                matches = new Range(-foundIdentifier);
                return true;
            }

            matches = Range.Empty;
            return false;
        }

        public bool TryFindString(String8 value, bool ignoreCase, out Range matches)
        {
            // Find value (case insensitive)
            if (!TryFindString(value, out matches)) return false;

            // Empty matches by default [despite an empty Range]
            if (value.IsEmpty()) return true;

            // If case sensitive, restrict to case sensitive subset
            if (ignoreCase == false)
            {
                matches = MakeCaseSensitive(matches, value);

                // If no case sensitive matches, return false
                return !matches.IsEmpty();
            }

            return true;
        }


        /// <summary>
        ///  Take a case insensitive range and restrict it to the case sensitive subset.
        ///  This can only be done for Ranges containing different casings of one value.
        ///  [Ranges from TryFindString, but not TryGetRangeStartingWith]
        /// </summary>
        /// <param name="r">Range to restrict</param>
        /// <param name="value">String8 casing of value to restrict to</param>
        /// <returns>Range constrained to subset matching value casing</returns>
        private Range MakeCaseSensitive(Range r, String8 value)
        {
            if (r.IsEmpty()) return r;

            // Verify this was called only for casing variations [there isn't a single Range for prefixes]
            if (this[r.End].Length != value.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            // Exclude values from start which don't match value case-sensitive
            int start;
            for (start = r.Start; start <= r.End; ++start)
            {
                if (value.CompareTo(this[start], false) == 0) break;
            }

            // Exclude values from end which don't match value case-sensitive
            int end;
            for (end = r.End; end > start; --end)
            {
                if (value.CompareTo(this[end], false) == 0) break;
            }

            return new Range(start, end);
        }

        public bool TryGetRangeStartingWith(String8 prefix, out Range matches)
        {
            if (_addedValues != null) throw new NotSupportedException(Resources.ConvertToImmutableRequired);
            return _existingValues.TryGetRangeStartingWith(prefix, out matches);
        }

        public IEnumerator<String8> GetSetStartingWith(String8 prefix)
        {
            Range matches;
            if (!TryGetRangeStartingWith(prefix, out matches)) matches = Range.Empty;
            return new StringStoreEnumerator(this, matches);
        }

        private class StringStoreEnumerator : IEnumerator<String8>
        {
            private StringStore Store { get; set; }
            private Range Range { get; set; }
            private int Next { get; set; }

            public String8 Current => this.Store[this.Next];
            object IEnumerator.Current => this.Store[this.Next];

            public StringStoreEnumerator(StringStore store, Range range)
            {
                this.Store = store;
                this.Range = range;
                Reset();
            }

            public bool MoveNext()
            {
                this.Next++;
                return this.Next <= this.Range.End;
            }

            public void Reset()
            {
                this.Next = this.Range.Start - 1;
            }

            public void Dispose()
            { }
        }
        #endregion

        #region Comparison
        public int CompareValues(int leftIdentifier, int rightIdentifier)
        {
            if (leftIdentifier == 0)
            {
                if (rightIdentifier == 0)
                {
                    // Both empty - equal
                    return 0;
                }
                else
                {
                    // Left is before right
                    return -1;
                }
            }
            else if (rightIdentifier == 0)
            {
                // Right is before left
                return 1;
            }
            else if (leftIdentifier < 0 && rightIdentifier < 0)
            {
                // If both new, compare in AddedValues
                return _addedValues.CompareValues(-leftIdentifier, -rightIdentifier);
            }
            else if (leftIdentifier >= 0 && rightIdentifier >= 0)
            {
                // If both existing, compare in ExistingValues
                return _existingValues.CompareValues(leftIdentifier, rightIdentifier);
            }
            else
            {
                // Otherwise, get and compare string values for each
                String8 left = this[leftIdentifier];
                String8 right = this[rightIdentifier];
                return left.CompareTo(right);
            }
        }
        #endregion

        #region Serialization
        public bool ConvertToImmutable()
        {
            // If there are added values, we need to convert for serialization
            if (_addedValues != null)
            {
                if (_existingValues != null)
                {
                    // Need to implement index merging for this
                    throw new NotImplementedException();
                }
                else
                {
                    // Convert AddedValues to immutable form
                    _existingValues = _addedValues.ConvertToImmutable(out _addedIdentifierToExistingIdentifier);
                    _addedValues = null;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///  Convert a whole PartialArray of identifiers from the ones assigned by FindOrAdd
        ///  to the values being serialized out.
        /// </summary>
        /// <param name="stringIdentifiers">PartialArray containing string identifiers for this StringStore</param>
        public void ConvertToImmutable(PartialArray<int> stringIdentifiers)
        {
            for (int i = 0; i < stringIdentifiers.Count; ++i)
            {
                stringIdentifiers[i] = this.GetSerializationIdentifier(stringIdentifiers[i]);
            }
        }

        /// <summary>
        ///  Get the serialized identifier for a given new string identifier.
        ///  StringStore assigns incrementing integers to each string as they are FindOrAdded.
        ///  When written, the strings are written in sorted order, and the new identifiers are
        ///  in that order. 
        /// </summary>
        /// <param name="identifier">Identifier from FindOrAdd to remap</param>
        /// <returns>Identifier to serialize out for correct behavior on reload</returns>
        public int GetSerializationIdentifier(int identifier)
        {
            if (identifier == 0) return 0;

            // Verify identifier is positive (negative identifiers are returned during indexing and should be remapped before being requested here)
            if (identifier > 0) throw new ArgumentOutOfRangeException("identifier");

            ConvertToImmutable();
            return _addedIdentifierToExistingIdentifier[-identifier];
        }

        public void ReadBinary(BinaryReader r)
        {
            // All values are serialized in immutable form, so only an ImmutableStringStore is created on load
            ImmutableStringStore store = new ImmutableStringStore();
            store.ReadBinary(r);

            _addedValues = null;
            _existingValues = store;
        }

        public void WriteBinary(BinaryWriter w)
        {
            ConvertToImmutable();
            if (_existingValues == null) _existingValues = new ImmutableStringStore(new String8Set(String8.Empty, 1, new PartialArray<int>()));
            _existingValues.WriteBinary(w);
        }
        #endregion

        #region Statistics
        public int Count
        {
            get
            {
                int total = 0;
                if (_addedValues != null) total += _addedValues.Count;
                if (_existingValues != null) total += _existingValues.Count;
                return total;
            }
        }

        public long Bytes
        {
            get
            {
                long total = 0;
                if (_addedValues != null) total += _addedValues.Bytes;
                if (_existingValues != null) total += _existingValues.Bytes;
                return total;
            }
        }
        #endregion
    }
}

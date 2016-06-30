// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Model.Index
{
    public class MemberIndex : IStatistics
    {
        private ImmutableMemberIndex _existingIndex;
        private MutableMemberIndex _addedIndex;

        public MemberIndex()
        {
            // Neither Index is initialized to start.
        }

        public void AddItem(int wordIdentifier, int itemIndex)
        {
            if (_addedIndex == null) _addedIndex = new MutableMemberIndex();
            _addedIndex.AddItem(wordIdentifier, itemIndex);
        }

        public bool TryGetMatchesInRange(Range identifiers, out int[] buffer, out int index, out int count)
        {
            if (_existingIndex == null) throw new InvalidOperationException(Resources.ConvertToImmutableRequired);
            return _existingIndex.TryGetMatchesInRange(identifiers, out buffer, out index, out count);
        }

        #region IStatistics
        public int Count
        {
            get
            {
                int total = 0;
                if (_addedIndex != null) total += _addedIndex.Count;
                if (_existingIndex != null) total += _existingIndex.Count;
                return total;
            }
        }

        public long Bytes
        {
            get
            {
                long total = 0;
                if (_addedIndex != null) total += _addedIndex.Bytes;
                if (_existingIndex != null) total += _existingIndex.Bytes;
                return total;
            }
        }
        #endregion

        #region IBinarySerializable
        public void ConvertToImmutable(StringStore strings)
        {
            if (_addedIndex != null)
            {
                if (_existingIndex != null)
                {
                    // Need to implement index merging for this
                    throw new NotImplementedException();
                }
                else
                {
                    // Convert AddedIndex to Immutable form
                    _addedIndex.UpdateIdentifiers(strings);
                    _existingIndex = _addedIndex.ConvertToImmutable(strings);
                    _addedIndex = null;
                }
            }

            // If nothing was indexed, write an empty index for serialization to load
            if (_existingIndex == null) _existingIndex = ImmutableMemberIndex.Empty;
        }

        public void WriteBinary(BinaryWriter w)
        {
            _existingIndex.WriteBinary(w);
        }

        public void ReadBinary(BinaryReader r)
        {
            // Read as ImmutableMemberIndex only
            _addedIndex = null;
            _existingIndex = new ImmutableMemberIndex();
            _existingIndex.ReadBinary(r);
        }
        #endregion
    }
}

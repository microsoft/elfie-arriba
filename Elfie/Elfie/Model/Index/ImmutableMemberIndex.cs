// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model.Index
{
    internal class ImmutableMemberIndex : IBinarySerializable, IStatistics
    {
        private int[] _sortedWordIdentifiers;
        private int[] _indexOfFirstMatch;
        private int[] _matchesBlock;

        /// <summary>
        ///  Serialization Constructor
        /// </summary>
        public ImmutableMemberIndex()
        { }

        internal ImmutableMemberIndex(int[] sortedWordIdentifiers, int[] indexOfFirstMatch, int[] matchesBlock)
        {
            _sortedWordIdentifiers = sortedWordIdentifiers;
            _indexOfFirstMatch = indexOfFirstMatch;
            _matchesBlock = matchesBlock;
        }

        public static ImmutableMemberIndex Empty = new ImmutableMemberIndex(EmptyArray<int>.Instance, EmptyArray<int>.Instance, EmptyArray<int>.Instance);

        public bool TryGetMatchesInRange(Range range, out int[] buffer, out int index, out int count)
        {
            int firstWordIndex, lastWordIndex;
            if (_sortedWordIdentifiers == null)
            {
                firstWordIndex = range.Start;
                lastWordIndex = range.End;
            }
            else
            {
                firstWordIndex = Array.BinarySearch(_sortedWordIdentifiers, range.Start);
                if (firstWordIndex < 0) firstWordIndex = ~firstWordIndex;

                lastWordIndex = Array.BinarySearch(_sortedWordIdentifiers, range.End);
                if (lastWordIndex < 0) lastWordIndex = ~lastWordIndex - 1;
            }

            if (firstWordIndex < _indexOfFirstMatch.Length)
            {
                buffer = _matchesBlock;
                index = _indexOfFirstMatch[firstWordIndex];
                count = GetIndexAfterLastMatch(lastWordIndex) - index;
                return count > 0;
            }
            else
            {
                buffer = null;
                index = 0;
                count = 0;
                return false;
            }
        }

        private int GetIndexAfterLastMatch(int wordIndex)
        {
            if (wordIndex >= _indexOfFirstMatch.Length - 1)
            {
                return _matchesBlock.Length;
            }
            else
            {
                return _indexOfFirstMatch[wordIndex + 1];
            }
        }

        #region IStatistics
        public int Count
        {
            get
            {
                return _indexOfFirstMatch.Length;
            }
        }

        public long Bytes
        {
            get
            {
                long length = 4 * (_indexOfFirstMatch.Length + _matchesBlock.Length);
                if (_sortedWordIdentifiers != null) length += 4 * _sortedWordIdentifiers.Length;
                return length;
            }
        }
        #endregion

        #region IBinarySerializable
        public void WriteBinary(BinaryWriter w)
        {
            w.Write(_sortedWordIdentifiers != null);

            if (_sortedWordIdentifiers != null)
            {
                w.WritePrimitiveArray(_sortedWordIdentifiers);
            }

            w.WritePrimitiveArray(_indexOfFirstMatch);
            w.WritePrimitiveArray(_matchesBlock);
        }

        public void ReadBinary(BinaryReader r)
        {
            bool hasSortedWordIdentifiers = r.ReadBoolean();

            if (hasSortedWordIdentifiers)
            {
                _sortedWordIdentifiers = r.ReadPrimitiveArray<int>();
            }
            else
            {
                _sortedWordIdentifiers = null;
            }

            _indexOfFirstMatch = r.ReadPrimitiveArray<int>();
            _matchesBlock = r.ReadPrimitiveArray<int>();
        }
        #endregion
    }
}

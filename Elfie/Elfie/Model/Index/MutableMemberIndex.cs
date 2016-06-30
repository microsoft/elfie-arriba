// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Model.Index
{
    internal class MutableMemberIndex : IStatistics
    {
        private Dictionary<int, List<int>> _wordToItemsIndex;

        public MutableMemberIndex()
        {
            _wordToItemsIndex = new Dictionary<int, List<int>>();
        }

        public void AddItem(int wordIdentifier, int itemIndex)
        {
            List<int> itemsForWord;

            if (!_wordToItemsIndex.TryGetValue(wordIdentifier, out itemsForWord))
            {
                itemsForWord = new List<int>(1);
                _wordToItemsIndex[wordIdentifier] = itemsForWord;
            }

            itemsForWord.Add(itemIndex);
        }

        public void UpdateIdentifiers(StringStore strings)
        {
            // Remap all words *and merge pages with only casing differences*
            Dictionary<int, List<int>> remappedIndex = new Dictionary<int, List<int>>(_wordToItemsIndex.Count);

            foreach (int wordIdentifier in _wordToItemsIndex.Keys)
            {
                int updatedIdentifier = strings.GetSerializationIdentifier(wordIdentifier);
                Range wordMatches = strings.RangeForString(updatedIdentifier);
                int firstCaseInsensitiveWordIdentifier = wordMatches.Start;

                List<int> existingMatchesForWord;
                if (!remappedIndex.TryGetValue(firstCaseInsensitiveWordIdentifier, out existingMatchesForWord))
                {
                    // This is the first (or only) case-insensitive copy of the word - use the list we had
                    remappedIndex[firstCaseInsensitiveWordIdentifier] = _wordToItemsIndex[wordIdentifier];
                }
                else
                {
                    // There are already values for another casing of this word. 
                    // Merge them to the current list in sorted order (this preserves ranking, if indexing was done in ranked order)
                    existingMatchesForWord.AddRange(_wordToItemsIndex[wordIdentifier]);
                    existingMatchesForWord.Sort();
                }
            }

            _wordToItemsIndex = remappedIndex;
        }

        public ImmutableMemberIndex ConvertToImmutable(StringStore strings)
        {
            // We can store "dense" mode (just IndexOfFirstMatch for every string) or
            // "sparse" mode (WordIdentifier AND IndexOfFirstMatch only for indexed strings
            // Choose the smaller option
            int bytesForDenseMode = 4 * strings.Count;
            int bytesForSparseMode = 4 * 2 * _wordToItemsIndex.Keys.Count;

            bool useDenseMode = (bytesForDenseMode <= bytesForSparseMode);
            int[] sortedWordIdentifiers;

            // If most or all strings are included, build an array for everything
            if (useDenseMode)
            {
                // One for every identifier
                sortedWordIdentifiers = new int[strings.Count];
                for (int i = 0; i < sortedWordIdentifiers.Length; ++i)
                {
                    sortedWordIdentifiers[i] = i;
                }
            }
            else
            {
                // One for each indexed word in order
                List<int> sortedWords = new List<int>(_wordToItemsIndex.Keys);
                sortedWords.Sort();
                sortedWordIdentifiers = sortedWords.ToArray();
            }

            // Build array of where matches for each word will begin in a shared matches block
            int matchCountSoFar = 0;
            int[] indexOfFirstMatch = new int[sortedWordIdentifiers.Length];
            for (int i = 0; i < sortedWordIdentifiers.Length; ++i)
            {
                indexOfFirstMatch[i] = matchCountSoFar;

                List<int> matchesForCurrentWord;
                if (_wordToItemsIndex.TryGetValue(sortedWordIdentifiers[i], out matchesForCurrentWord))
                {
                    matchCountSoFar += matchesForCurrentWord.Count;
                }
            }

            // Write all match lists into a new block
            int[] matchesBlock = new int[matchCountSoFar];
            for (int i = 0; i < sortedWordIdentifiers.Length; ++i)
            {
                List<int> matchesForCurrentWord;
                if (_wordToItemsIndex.TryGetValue(sortedWordIdentifiers[i], out matchesForCurrentWord))
                {
                    matchesForCurrentWord.CopyTo(matchesBlock, indexOfFirstMatch[i]);
                }
            }

            // Build an ImmutableMemberIndex on the converted structures
            if (useDenseMode)
            {
                return new ImmutableMemberIndex(null, indexOfFirstMatch, matchesBlock);
            }
            else
            {
                return new ImmutableMemberIndex(sortedWordIdentifiers, indexOfFirstMatch, matchesBlock);
            }
        }

        #region IStatistics
        public int Count
        {
            get
            {
                return _wordToItemsIndex.Count;
            }
        }

        public long Bytes
        {
            get
            {
                long total = 0;

                foreach (int stringIdentifier in _wordToItemsIndex.Keys)
                {
                    // Add size for word (int for word index, int for first match position, int for each match)
                    // This is computing the size the immutable version will be, not the size of this version.
                    total += 4 * (1 + 1 + _wordToItemsIndex[stringIdentifier].Count);
                }

                return total;
            }
        }
        #endregion
    }
}

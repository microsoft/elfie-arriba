// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Extensions;
using Arriba.Indexing;
using Arriba.Model;
using Arriba.Model.Expressions;
using Arriba.Serialization;

namespace Arriba.Structures
{
    /// <summary>
    ///  WordIndex provides inverted index functionality for a given column.
    /// </summary>
    public class WordIndex : IBinarySerializable
    {
        public const int MinimumPrefixExpandLength = 3;

        private IWordSplitter _splitter;
        private List<WordIndexBlock> _blocks;

        public WordIndex(IWordSplitter splitter)
        {
            _splitter = splitter;
            _blocks = new List<WordIndexBlock>();
        }

        #region Search
        /// <summary>
        ///  Find the items containing the exact word passed and add them to
        ///  the result set passed.
        /// </summary>
        /// <param name="word">Word to find</param>
        /// <param name="result">Result to which to add matches</param>
        public void WhereMatchExact(ByteBlock word, ShortSet result)
        {
            // Find the block with the exact word and find matches
            foreach (WordIndexBlock block in _blocks)
            {
                ushort wordIndex = block.IndexOf(word);

                if (wordIndex != ushort.MaxValue)
                {
                    block.GetInSet(wordIndex, result);
                    return;
                }
            }
        }

        /// <summary>
        ///  Find the items containing *any word* starting with the word passed
        ///  and add the union of all of them to the result set passed.
        /// </summary>
        /// <param name="prefix">Prefix to find</param>
        /// <param name="result">Result to which to add matches</param>
        public void WhereMatches(ByteBlock prefix, ShortSet result)
        {
            if (result == null) throw new ArgumentNullException("result");

            // Split each word in the input value
            RangeSet prefixWords = _splitter.Split(prefix);

            // Shortcut: If only one word, add directly to result set
            if (prefixWords.Count == 1 && prefixWords.Ranges[0].Length == prefix.Length)
            {
                // Add matches for words starting with this prefix in every block
                foreach (WordIndexBlock block in _blocks)
                {
                    block.WhereMatches(prefix, result);
                }
            }
            else
            {
                // We need to add (OR) the items which match all words (AND) in the split prefix
                ShortSet matchesForAllWords = null;
                ShortSet matchesForWord = new ShortSet(result.Capacity);

                // For each found word, add all matches
                for (int i = 0; i < prefixWords.Count; ++i)
                {
                    Range word = prefixWords.Ranges[i];
                    ByteBlock wordBlock = new ByteBlock(prefix.Array, word.Index, word.Length);

                    matchesForWord.Clear();

                    // Add matches for words starting with this prefix in every block
                    foreach (WordIndexBlock block in _blocks)
                    {
                        block.WhereMatches(wordBlock, matchesForWord);
                    }

                    // AND matches for this word with each other word in the prefix
                    if (matchesForAllWords == null)
                    {
                        matchesForAllWords = new ShortSet(result.Capacity);
                        matchesForAllWords.Or(matchesForWord);
                    }
                    else
                    {
                        matchesForAllWords.And(matchesForWord);
                    }
                }

                // OR matches for ALL words with the final result
                if (matchesForAllWords != null) result.Or(matchesForAllWords);
            }
        }
        #endregion

        #region Add/Remove
        public void Index(ushort id, ByteBlock oldValue, ByteBlock newValue)
        {
            RangeSet oldValueWords = _splitter.Split(oldValue);
            RangeSet newValueWords = _splitter.Split(newValue);

            for (int i = 0; i < oldValueWords.Count; ++i)
            {
                Range word = oldValueWords.Ranges[i];
                ByteBlock wordBlock = new ByteBlock(oldValue.Array, word.Index, word.Length);
                RemoveWord(id, wordBlock);
            }

            for (int i = 0; i < newValueWords.Count; ++i)
            {
                Range word = newValueWords.Ranges[i];
                ByteBlock wordBlock = new ByteBlock(newValue.Array, word.Index, word.Length);
                AddWord(id, wordBlock);
            }
        }

        /// <summary>
        ///  Add a given item to the index for a given word.
        /// </summary>
        /// <param name="id">Item to add</param>
        /// <param name="word">Word with which to associate item</param>
        public void AddWord(ushort id, ByteBlock word)
        {
            WordIndexBlock block = null;
            ushort wordIndex = ushort.MaxValue;

            // If the word is already in a block, get the index
            for (int i = 0; i < _blocks.Count; ++i)
            {
                block = _blocks[i];
                wordIndex = block.IndexOf(word);
                if (wordIndex != ushort.MaxValue) break;
            }

            // If not, try to add the word to the last block
            if (wordIndex == ushort.MaxValue && block != null)
            {
                wordIndex = block.Add(word);
            }

            // If the last block was full, add another block
            if (wordIndex == ushort.MaxValue)
            {
                block = new WordIndexBlock();
                wordIndex = block.Add(word);
                _blocks.Add(block);
            }

            block.AddToSet(wordIndex, id);
        }

        /// <summary>
        ///  Remove a given item from the index for a given word.
        /// </summary>
        /// <param name="id">Item to remove</param>
        /// <param name="word">Word from which to disassociate item</param>
        public void RemoveWord(ushort id, ByteBlock word)
        {
            foreach (WordIndexBlock block in _blocks)
            {
                ushort wordIndex = block.IndexOf(word);

                if (wordIndex != ushort.MaxValue)
                {
                    block.RemoveFromSet(wordIndex, id);
                    break;
                }
            }
        }
        #endregion

        #region Management
        public void VerifyConsistency(IColumn column, VerificationLevel level, ExecutionDetails details)
        {
            foreach (WordIndexBlock block in _blocks)
            {
                block.VerifyConsistency(column, level, details);
            }
        }
        #endregion

        #region Dictionary Conversion [Testability]
        public Dictionary<string, List<ushort>> ConvertToDictionary()
        {
            Dictionary<string, List<ushort>> result = new Dictionary<string, List<ushort>>();

            foreach (WordIndexBlock block in _blocks)
            {
                block.ExportToDictionary(result);
            }

            return result;
        }
        #endregion

        #region IBinarySerializable
        public void ReadBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            _blocks.Clear();

            int blockCount = context.Reader.ReadInt32();
            for (int i = 0; i < blockCount; ++i)
            {
                WordIndexBlock block = new WordIndexBlock();
                block.ReadBinary(context);
                _blocks.Add(block);
            }
        }

        public void WriteBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            context.Writer.Write(_blocks.Count);
            foreach (WordIndexBlock block in _blocks)
            {
                block.WriteBinary(context);
            }
        }
        #endregion

        #region WordIndexBlock Nested Class
        internal class WordIndexBlock : IBinarySerializable
        {
            private const ushort WordCountLimit = 65000;
            private const ushort DenseSetLengthCutoff = 1024;

            private SortedColumn<ByteBlock> _words;
            private ByteBlockColumn _sets;

            public WordIndexBlock()
            {
                _words = new SortedColumn<ByteBlock>(new ByteBlockColumn(ByteBlock.Zero), 0);
                _sets = new ByteBlockColumn(ByteBlock.Zero);
            }

            #region Word Operations
            /// <summary>
            ///  Return the index of a given word in this block.
            /// </summary>
            /// <param name="word">Word to find</param>
            /// <returns>Index of word (and corresponding set) or ushort.MaxValue if not present.</returns>
            public ushort IndexOf(ByteBlock word)
            {
                ushort index;
                _words.TryGetIndexOf(word, out index);
                return index;
            }

            /// <summary>
            ///  Add the given word and return the index.
            /// </summary>
            /// <param name="word">Word to add</param>
            /// <returns>Index of new word, or ushort.MaxValue if unable to add</returns>
            public ushort Add(ByteBlock word)
            {
                if (_words.Count < WordCountLimit)
                {
                    ushort wordIndex = _words.Count;
                    _words.SetSize((ushort)(wordIndex + 1));
                    _sets.SetSize((ushort)(wordIndex + 1));
                    _words[wordIndex] = word;
                    return wordIndex;
                }

                return ushort.MaxValue;
            }

            /// <summary>
            ///  Add matches to the given set for all words starting with the provided prefix.
            /// </summary>
            /// <param name="prefix">Prefix for which to add matches</param>
            /// <param name="result">Set to add all items containing words beginning with prefix</param>
            public void WhereMatches(ByteBlock prefix, ShortSet result)
            {
                // Look for prefixes if above the length minimum; equality otherwise
                if (prefix.Length < MinimumPrefixExpandLength)
                {
                    WhereMatchesExact(prefix, result);
                    return;
                }

                IComparable<ByteBlock> isPrefixOf = prefix.GetExtendedIComparable(ByteBlock.Comparison.IsPrefixOf);

                // Otherwise, find all words starting with this prefix
                int firstIndex = _words.FindFirstWhere(isPrefixOf);
                if (firstIndex < 0) return;

                int lastIndex = _words.FindLastWhere(isPrefixOf);

                IList<ushort> sortedIndexes;
                int sortedIndexescount;
                _words.TryGetSortedIndexes(out sortedIndexes, out sortedIndexescount);

                for (int i = firstIndex; i <= lastIndex; ++i)
                {
                    GetInSet(sortedIndexes[i], result);
                }
            }

            /// <summary>
            ///  Add matches to the given set for all items with the exact value passed.
            /// </summary>
            /// <param name="value">Word for which to add matches</param>
            /// <param name="result">Set to add all items containing the word to</param>
            public void WhereMatchesExact(ByteBlock value, ShortSet result)
            {
                ushort index;

                _words.TryGetIndexOf(value, out index);

                if (index != ushort.MaxValue)
                {
                    GetInSet(index, result);
                }
            }
            #endregion

            #region Set Operations
            /// <summary>
            ///  Add a given item to a given set.
            /// </summary>
            /// <param name="setId">SetID (WordID) of word to add item to.</param>
            /// <param name="itemId">ItemID to add for word</param>
            public unsafe void AddToSet(ushort setId, ushort itemId)
            {
                ByteBlock set = _sets[setId];

                fixed (byte* array = set.Array)
                {
                    if (set.Length < DenseSetLengthCutoff)
                    {
                        // Sparse Set: Add values as individual ushorts.
                        ushort* valuesForWord = (ushort*)(array + set.Index);
                        ushort availableLength = (ushort)(set.Length / 2);
                        ushort usedLength = FindUsedLength(valuesForWord, availableLength);

                        // If this value was already added, stop
                        if (usedLength > 0 && valuesForWord[usedLength - 1] == itemId) return;

                        if (usedLength < availableLength)
                        {
                            // Set not full - append the new value
                            valuesForWord[usedLength] = itemId;
                        }
                        else
                        {
                            // Set is full - create a new one
                            if (set.Length * 2 >= DenseSetLengthCutoff)
                            {
                                // At cutoff - convert to dense set
                                byte[] newDenseBlock = new byte[ushort.MaxValue / 8];
                                fixed (byte* newArray = newDenseBlock)
                                {
                                    ulong* newBits = (ulong*)(newArray);

                                    for (int i = 0; i < usedLength; ++i)
                                    {
                                        ushort id = valuesForWord[i];
                                        newBits[id / 64] |= (ShortSet.FirstBit >> id % 64);
                                    }

                                    newBits[itemId / 64] |= (ShortSet.FirstBit >> itemId % 64);
                                }

                                _sets[setId] = newDenseBlock;
                            }
                            else
                            {
                                // Below cutoff - keep sparse set
                                byte[] newBlock = new byte[Math.Max(2, set.Length * 2)];

                                // Copy current values
                                set.CopyTo(newBlock);

                                fixed (byte* newArray = newBlock)
                                {
                                    ushort* newValues = (ushort*)newArray;

                                    // Add new value
                                    newValues[usedLength] = itemId;

                                    // Pad remainder with sentinel maxvalue
                                    for (int i = usedLength + 1; i < newBlock.Length / 2; ++i)
                                    {
                                        newValues[i] = ushort.MaxValue;
                                    }
                                }

                                _sets[setId] = newBlock;
                            }
                        }
                    }
                    else
                    {
                        // Dense Set: Turn on the bit for the value
                        ulong* bitsForWord = (ulong*)(array + set.Index);
                        bitsForWord[itemId / 64] |= (ShortSet.FirstBit >> itemId % 64);
                    }
                }
            }

            /// <summary>
            ///  Remove a given item from a given set.
            /// </summary>
            /// <param name="setId">SetID (WordID) of word to remove</param>
            /// <param name="itemId">ItemID of item to remove from word</param>
            public unsafe void RemoveFromSet(ushort setId, ushort itemId)
            {
                ByteBlock set = _sets[setId];

                fixed (byte* array = set.Array)
                {
                    if (set.Length < DenseSetLengthCutoff)
                    {
                        // Sparse Set: Remove the value and swap the last value.
                        ushort* valuesForWord = (ushort*)(array + set.Index);
                        ushort availableLength = (ushort)(set.Length / 2);
                        ushort usedLength = FindUsedLength(valuesForWord, availableLength);

                        if (usedLength == 1)
                        {
                            // If word is unique, remove the word and swap the last value.
                            // NOTE: Need to confirm last ID is really this one, because multiple copies of the word may be in the value.
                            if (valuesForWord[0] == itemId)
                            {
                                ushort lastWordIndex = (ushort)(_words.Count - 1);

                                if (lastWordIndex > 0 && lastWordIndex != setId)
                                {
                                    _sets[setId] = _sets[lastWordIndex];
                                    _words[setId] = _words[lastWordIndex];
                                }

                                _words.SetSize(lastWordIndex);
                                _sets.SetSize(lastWordIndex);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < usedLength; ++i)
                            {
                                if (valuesForWord[i] == itemId)
                                {
                                    // Swap the last value here
                                    --usedLength;
                                    valuesForWord[i] = valuesForWord[usedLength];
                                    valuesForWord[usedLength] = ushort.MaxValue;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Dense Set: Turn off the bit for the value
                        ulong* bitsForWord = (ulong*)(array + set.Index);
                        bitsForWord[itemId / 64] &= ~(ShortSet.FirstBit >> itemId % 64);
                    }
                }
            }

            /// <summary>
            ///  Get the IDs listed in the set for a given word and add them
            ///  to a result ShortSet.
            /// </summary>
            /// <param name="setId">ID of set/word to add</param>
            /// <param name="result">ShortSet to which to add results</param>
            public unsafe void GetInSet(ushort setId, ShortSet result)
            {
                ByteBlock set = _sets[setId];

                fixed (byte* array = set.Array)
                {
                    if (set.Length < DenseSetLengthCutoff)
                    {
                        // Sparse Set: Add values as individual ushorts.
                        ushort* valuesForWord = (ushort*)(array + set.Index);
                        ushort usedLength = FindUsedLength(valuesForWord, (ushort)(set.Length / 2));
                        result.Or(valuesForWord, usedLength);
                    }
                    else
                    {
                        // Dense Set: Add values as ulong bits.
                        ulong* bitsForWord = (ulong*)(array + set.Index);
                        result.Or(bitsForWord, (ushort)(set.Length / 8));
                    }
                }
            }

            /// <summary>
            ///  Find the used portion of a given sparse set. Sets are MaxValue
            ///  padded, so the used portion is the number of non-MaxValue values.
            /// </summary>
            /// <param name="set">Set array to search</param>
            /// <param name="totalSpace">Length of full array</param>
            /// <returns>Number of items set, totalSpace if all of them</returns>
            private unsafe ushort FindUsedLength(ushort* set, ushort totalSpace)
            {
                // If the last value is set, there is no free space
                if (set == null || totalSpace == 0 || set[totalSpace - 1] != ushort.MaxValue) return totalSpace;

                int min = 0;
                int max = totalSpace - 1;

                // Find the first zero value in our array
                while (min < max)
                {
                    int mid = (min + max) / 2;

                    if (set[mid] != ushort.MaxValue)
                    {
                        min = mid + 1;
                    }
                    else
                    {
                        max = mid;
                    }
                }

                // Return the last index with a value (same as the length)
                if (set[min] != ushort.MaxValue)
                    return (ushort)(min + 1);
                else
                    return (ushort)min;
            }
            #endregion

            #region Management
            public void VerifyConsistency(IColumn column, VerificationLevel level, ExecutionDetails details)
            {
                if (_words.Count > WordCountLimit)
                {
                    details.AddError(ExecutionDetails.WordIndexBlockTooFull, column.Name, _words.Count);
                }

                if (_words.Count != _sets.Count)
                {
                    details.AddError(ExecutionDetails.WordIndexBlockSizesMismatch, column.Name, _words.Count, _sets.Count);
                }

                if (level == VerificationLevel.Full)
                {
                    // Validate that all IDs in all sets are valid
                    // NOTE: Replacing with a validating GetInSet would be more thorough; check for duplicate values, padding problems, etc.
                    ShortSet allValidItems = new ShortSet(column.Count);
                    allValidItems.Not();

                    ShortSet items = new ShortSet(ushort.MaxValue);
                    for (ushort i = 0; i < _words.Count; ++i)
                    {
                        items.Clear();
                        GetInSet(i, items);

                        items.AndNot(allValidItems);
                        if (items.Count() > 0)
                        {
                            details.AddError(ExecutionDetails.WordIndexInvalidItemID, column.Name, _words[i], String.Join(", ", items.Values));
                        }
                    }
                }

                // Ask the Sets and Words columns to self-verify
                _sets.VerifyConsistency(level, details);
                _words.VerifyConsistency(level, details);
            }
            #endregion

            #region Dictionary Conversion [Testability]
            public void ExportToDictionary(Dictionary<string, List<ushort>> result)
            {
                IList<ushort> sortedWords;
                int sortedWordsCount;
                _words.TryGetSortedIndexes(out sortedWords, out sortedWordsCount);

                for (int i = 0; i < _words.Count; ++i)
                {
                    ushort lid = sortedWords[i];
                    string word = _words[lid].ToString();

                    ShortSet set = new ShortSet(ushort.MaxValue);
                    GetInSet(lid, set);

                    List<ushort> page;
                    if (!result.TryGetValue(word, out page))
                    {
                        page = new List<ushort>();
                        result[word] = page;
                    }

                    page.AddRange(set.Values);
                }
            }
            #endregion

            #region IBinarySerializable
            public void ReadBinary(ISerializationContext context)
            {
                _words.ReadBinary(context);
                _sets.ReadBinary(context);
            }

            public void WriteBinary(ISerializationContext context)
            {
                _words.WriteBinary(context);
                _sets.WriteBinary(context);
            }
            #endregion
        }
        #endregion
    }
}

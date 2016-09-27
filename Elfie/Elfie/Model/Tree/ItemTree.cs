// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model.Tree
{
    /// <summary>
    ///  ItemTree is a data structure designed to efficiently encode a hierarchy
    ///  structure on a set of items. It's used when the items stored in a data
    ///  structure have a single logical hierarchy that needs to be traversed.
    ///  
    ///  Like other Elfie structures, ItemTree has a mutable form for indexing
    ///  time and an Immutable form for search time. After the structure is built,
    ///  ConvertToImmutable must be called and no further changes can be made.
    /// </summary>
    public class ItemTree : IStatistics, IBinarySerializable
    {
        private PartialArray<int> _parentIndex;
        private PartialArray<int> _firstChildIndex;
        private PartialArray<int> _nextSiblingIndex;
        private PartialArray<int> _nameIdentifier;

        public ItemTree() : this(0)
        { }

        public ItemTree(int rootIdentifier)
        {
            _parentIndex = new PartialArray<int>();
            _firstChildIndex = new PartialArray<int>();
            _nextSiblingIndex = new PartialArray<int>();
            _nameIdentifier = new PartialArray<int>();

            // Add sentinel root
            _parentIndex.Add(-1);
            _firstChildIndex.Add(-1);
            _nextSiblingIndex.Add(-1);
            _nameIdentifier.Add(rootIdentifier);
        }

        #region Add
        public int Add(int parentIndex, int nameIdentifier)
        {
            if (parentIndex < -1 || parentIndex >= Count) throw new ArgumentOutOfRangeException("parentIndex");

            // If parent was not found from a search, parent is the root
            if (parentIndex == -1) parentIndex = 0;

            // Make a new node, pointing to the parent, with the parent's first child as a sibling
            int newNodeIndex = Count;
            _parentIndex.Add(parentIndex);
            _firstChildIndex.Add(-1);
            _nextSiblingIndex.Add(_firstChildIndex[parentIndex]);
            _nameIdentifier.Add(nameIdentifier);

            // Make the parent's first child now the new node
            _firstChildIndex[parentIndex] = newNodeIndex;

            return newNodeIndex;
        }

        public int AddPath(int rootIndex, String8Set path, StringStore strings)
        {
            int currentIndex = rootIndex;

            for (int i = 0; i < path.Count; ++i)
            {
                String8 part = path[i];
                int partNameIdentifier = strings.FindOrAddString(part);

                int foundNode;
                if (!TryFindChildByName(currentIndex, partNameIdentifier, out foundNode))
                {
                    foundNode = Add(currentIndex, partNameIdentifier);
                }

                currentIndex = foundNode;
            }

            return currentIndex;
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetNameIdentifier(int index)
        {
            return _nameIdentifier[index];
        }

        public Path8 GetPath(int index, StringStore strings, char delimiter = '.')
        {
            return new Path8(strings, this, index, delimiter);
        }

        #region Tree Traversal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetParent(int index)
        {
            if (index == 0) return -1;
            return _parentIndex[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetFirstChild(int index)
        {
            return _firstChildIndex[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetNextSibling(int index)
        {
            return _nextSiblingIndex[index];
        }

        public int GetDepth(int index)
        {
            int depth = 0;

            int currentIndex = index;
            while (currentIndex > 0)
            {
                currentIndex = GetParent(currentIndex);
                depth++;
            }

            return depth;
        }

        public int GetAncestorAtDepth(int index, int desiredDepth)
        {
            int depth = GetDepth(index);
            int currentIndex = index;
            while (depth > desiredDepth)
            {
                currentIndex = GetParent(currentIndex);
                depth--;
            }

            return currentIndex;
        }
        #endregion  

        #region Search
        public bool TryFindChild(int parentIndex, Func<int, bool> doesNodeIndexMatch, out int matchingIndex)
        {
            int currentIndex = GetFirstChild(parentIndex);
            while (currentIndex > 0)
            {
                if (doesNodeIndexMatch(currentIndex))
                {
                    matchingIndex = currentIndex;
                    return true;
                }

                currentIndex = GetNextSibling(currentIndex);
            }

            matchingIndex = -1;
            return false;
        }

        public bool TryFindChildByName(int parentIndex, int nameIdentifier, out int matchingIndex)
        {
            int currentIndex = GetFirstChild(parentIndex);
            while (currentIndex > 0)
            {
                if (nameIdentifier == _nameIdentifier[currentIndex])
                {
                    matchingIndex = currentIndex;
                    return true;
                }

                currentIndex = _nextSiblingIndex[currentIndex];
            }

            matchingIndex = -1;
            return false;
        }

        public bool TryFindChildByName(int parentIndex, Range nameRange, out int matchingIndex)
        {
            int currentIndex = GetFirstChild(parentIndex);
            while (currentIndex > 0)
            {
                if (nameRange.Contains(_nameIdentifier[currentIndex]))
                {
                    matchingIndex = currentIndex;
                    return true;
                }

                currentIndex = _nextSiblingIndex[currentIndex];
            }

            matchingIndex = -1;
            return false;
        }

        public bool TryFindByPath(int startIndex, String8Set path, StringStore strings, out int matchingIndex)
        {
            // If we find nothing we should return 0 (the root was the last matching thing we found)
            matchingIndex = 0;

            return TryFindByPath(startIndex, 0, path, strings, ref matchingIndex);
        }

        private bool TryFindByPath(int parentNodeIndex, int currentPathPartIndex, String8Set path, StringStore strings, ref int matchingIndex)
        {
            // Try to find the string identifier for the next path part name
            Range currentPartIdentifier;
            if (!strings.TryFindString(path[currentPathPartIndex], out currentPartIdentifier)) return false;

            // Try to find nodes matching this name part and search within each of them
            int child = this.GetFirstChild(parentNodeIndex);
            while (child > 0)
            {
                if (currentPartIdentifier.Contains(this.GetNameIdentifier(child)))
                {
                    // If we've matched all parts, return success.
                    if (currentPathPartIndex == path.Count - 1)
                    {
                        matchingIndex = child;
                        return true;
                    }
                    else
                    {
                        // Record that we matched up to this point
                        matchingIndex = child;

                        // If not, search under here for the remaining parts (for each match at this level)
                        if (TryFindByPath(child, currentPathPartIndex + 1, path, strings, ref matchingIndex)) return true;
                    }
                }

                child = this.GetNextSibling(child);
            }

            return false;
        }

        public int FindByPath(int startIndex, String8Set path, StringStore strings)
        {
            int matchingIndex;
            if (TryFindByPath(startIndex, path, strings, out matchingIndex)) return matchingIndex;

            StringBuilder message = new StringBuilder();
            using (StringWriter writer = new StringWriter(message))
            {
                writer.Write("Could not find '");
                path.Value.WriteTo(writer);
                writer.WriteLine("' in tree.");

                int partsFound = this.GetDepth(matchingIndex) - this.GetDepth(startIndex);
                writer.Write("Could not find '");
                path[partsFound].WriteTo(writer);
                writer.Write("' under '");
                this.GetPath(matchingIndex, strings, '|').WriteTo(writer);
                writer.WriteLine("' with children:");
                WriteChildrenOf(writer, strings, matchingIndex);
                writer.WriteLine();
            }

            throw new TreeItemNotFoundException(message.ToString());
        }
        #endregion

        #region Tree Sorting
        public void Sort(Func<int, int, int> nodeIndexComparer)
        {
            PartialArray<int> buffer = new PartialArray<int>();
            Sort(0, new FuncToIComparerAdapter(nodeIndexComparer), ref buffer);
        }

        public void Sort(IComparer<int> nodeIndexComparer)
        {
            PartialArray<int> buffer = new PartialArray<int>();
            Sort(0, nodeIndexComparer, ref buffer);
        }

        public void SortByName(StringStore strings)
        {
            this.Sort((left, right) => strings[this.GetNameIdentifier(left)].CompareTo(strings[this.GetNameIdentifier(right)]));
        }

        private void Sort(int parentNodeIndex, IComparer<int> nodeIndexComparer, ref PartialArray<int> buffer)
        {
            int currentChild = _firstChildIndex[parentNodeIndex];

            // If no children, return
            if (currentChild <= 0) return;

            // Add all children of the current element to the buffer
            buffer.Clear();
            while (currentChild > 0)
            {
                buffer.Add(currentChild);
                currentChild = _nextSiblingIndex[currentChild];
            }

            // Sort the children by the compare function
            buffer.Sort(nodeIndexComparer);

            // Modify the FirstChild and NextSibling pointers to be in sorted order
            currentChild = buffer[0];
            _firstChildIndex[parentNodeIndex] = currentChild;

            for (int i = 1; i < buffer.Count; ++i)
            {
                int nextChild = buffer[i];
                _nextSiblingIndex[currentChild] = nextChild;
                currentChild = nextChild;
            }

            _nextSiblingIndex[currentChild] = -1;

            // Recurse on the children
            currentChild = _firstChildIndex[parentNodeIndex];
            while (currentChild > 0)
            {
                Sort(currentChild, nodeIndexComparer, ref buffer);
                currentChild = _nextSiblingIndex[currentChild];
            }
        }

        private class FuncToIComparerAdapter : IComparer<int>
        {
            private Func<int, int, int> _comparer;

            public FuncToIComparerAdapter(Func<int, int, int> comparer)
            {
                _comparer = comparer;
            }

            public int Compare(int leftIndex, int rightIndex)
            {
                return _comparer(leftIndex, rightIndex);
            }
        }
        #endregion

        #region IStatistics
        public int Count
        {
            get { return _parentIndex.Count; }
        }

        public virtual long Bytes
        {
            get
            {
                return Count * 16;
            }
        }
        #endregion

        #region IBinarySerializable
        /// <summary>
        ///  Return whether this ItemTree still requires a remap (and associated
        ///  ConvertToQueryable work). If string identifiers are still negative
        ///  a remap hasn't been done.
        /// </summary>
        /// <returns>True if a remap is required, false otherwise</returns>
        public bool RequiresRemap()
        {
            // Look for non-zero string identifiers. 
            // Negative means remap needed. Positive means remap done.
            for (int i = 0; i < this.Count; ++i)
            {
                int nameIdentifier = _nameIdentifier[i];
                if (nameIdentifier > 0) return false;
                if (nameIdentifier < 0) return true;
            }

            return false;
        }

        public void UpdateIdentifiers(StringStore strings)
        {
            for (int i = 0; i < this.Count; ++i)
            {
                _nameIdentifier[i] = strings.GetSerializationIdentifier(_nameIdentifier[i]);
            }
        }

        public void WriteBinary(BinaryWriter w)
        {
            _parentIndex.WriteBinary(w);
            _firstChildIndex.WriteBinary(w);
            _nextSiblingIndex.WriteBinary(w);
            _nameIdentifier.WriteBinary(w);
        }

        public void ReadBinary(BinaryReader r)
        {
            _parentIndex.ReadBinary(r);
            _firstChildIndex.ReadBinary(r);
            _nextSiblingIndex.ReadBinary(r);
            _nameIdentifier.ReadBinary(r);
        }
        #endregion

        #region Debuggability
        public void WriteChildrenOf(TextWriter writer, StringStore strings, int parentIndex)
        {
            // Consider -1 (not found) to be the same as the sentinel root
            if (parentIndex == -1) parentIndex = 0;

            int childIndex = this.GetFirstChild(parentIndex);
            while (childIndex > 0)
            {
                strings[this.GetNameIdentifier(childIndex)].WriteTo(writer);
                childIndex = this.GetNextSibling(childIndex);
                if (childIndex > 0) writer.Write(", ");
            }
        }

        public void WriteTree(TextWriter writer, StringStore strings, int index = 0, int indent = -1, Action<int, TextWriter> writeAdditionalDetail = null)
        {
            // Write element (but only if indent is positive)
            // Write everything under the sentinel root with WriteTree(w, s, 0, -1)
            if (indent >= 0)
            {
                for (int i = 0; i < indent; ++i)
                {
                    writer.Write('\t');
                }

                strings[this.GetNameIdentifier(index)].WriteTo(writer);
                if (writeAdditionalDetail != null) writeAdditionalDetail(index, writer);
                writer.WriteLine();
            }

            int childIndex = this.GetFirstChild(index);
            while (childIndex > 0)
            {
                WriteTree(writer, strings, childIndex, indent + 1, writeAdditionalDetail);
                childIndex = this.GetNextSibling(childIndex);
            }
        }
        #endregion
    }

    [Serializable]
    public class TreeItemNotFoundException : Exception
    {
        public TreeItemNotFoundException() { }
        public TreeItemNotFoundException(string message) : base(message) { }
        public TreeItemNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected TreeItemNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
    }
}

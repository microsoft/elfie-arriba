// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Model.Map
{
    internal class MutableItemMap<T>
    {
        private IReadOnlyList<T> _provider;
        private PartialArray<int> _groupIndices;
        private PartialArray<int> _memberIndices;

        public MutableItemMap(IReadOnlyList<T> provider)
        {
            _provider = provider;
            _groupIndices = new PartialArray<int>();
            _memberIndices = new PartialArray<int>();
        }

        /// <summary>
        ///  Add a link from one item to another. Links must be added to items in
        ///  order of insertion.
        /// </summary>
        /// <param name="groupIndex">Index of item from which to link</param>
        /// <param name="memberIndex">Index of item to which to link</param>
        public void AddLink(int groupIndex, int memberIndex)
        {
            _groupIndices.Add(groupIndex);
            _memberIndices.Add(memberIndex);
        }

        public ImmutableItemMap<T> ConvertToImmutable(ImmutableItemMap<T> previousLinks = null)
        {
            // If no links were added, return the previous map 
            if (_groupIndices.Count == 0) return previousLinks;

            // Sort the new links by the group index
            PartialArray<int>.SortKeysAndItems(_groupIndices, _memberIndices);

            // Build a new immutable map
            ImmutableItemMap<T> newMap = new ImmutableItemMap<T>(_provider);

            // Track how many of the sorted mutable links we've added so far
            int nextIndex = 0;

            // Track the group we're adding links for and what we've linked it to (to filter duplicates)
            int currentGroup = -1;
            HashSet<int> linksAlreadyAddedForGroup = null;

            // If there was an immutable set, build from it first
            if (previousLinks != null)
            {
                for (int groupIndex = 0; groupIndex < previousLinks._firstMemberIndexForGroup.Count; ++groupIndex)
                {
                    linksAlreadyAddedForGroup = new HashSet<int>();

                    // Add all links in the old set from this group (if any)
                    MapEnumerator<T> oldLinks = previousLinks.LinksFrom(groupIndex);
                    while (oldLinks.MoveNext())
                    {
                        if (linksAlreadyAddedForGroup.Add(oldLinks.CurrentIndex)) newMap.AddLink(groupIndex, oldLinks.CurrentIndex);
                    }

                    // Add all links in the new set from this group (if any)
                    while (nextIndex < _groupIndices.Count && _groupIndices[nextIndex] == groupIndex)
                    {
                        if (linksAlreadyAddedForGroup.Add(_memberIndices[nextIndex])) newMap.AddLink(_groupIndices[nextIndex], _memberIndices[nextIndex]);
                        nextIndex++;
                    }
                }
            }

            // Add remaining links in the new set, if any are left
            currentGroup = -1;
            while (nextIndex < _groupIndices.Count)
            {
                // If we're adding links for a different group, reset the 'already added' set
                if (_groupIndices[nextIndex] != currentGroup)
                {
                    currentGroup = _groupIndices[nextIndex];
                    linksAlreadyAddedForGroup = new HashSet<int>();
                }

                if (linksAlreadyAddedForGroup.Add(_memberIndices[nextIndex])) newMap.AddLink(_groupIndices[nextIndex], _memberIndices[nextIndex]);
                nextIndex++;
            }

            return newMap;
        }

        /// <summary>
        ///  Return the count of links in the Map (so far)
        /// </summary>
        public int Count
        {
            get { return _memberIndices.Count; }
        }
    }
}

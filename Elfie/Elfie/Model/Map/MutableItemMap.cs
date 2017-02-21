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

        public ImmutableItemMap<T> ConvertToImmutable()
        {
            // Build a new immutable map
            ImmutableItemMap<T> newMap = new ImmutableItemMap<T>(_provider);

            // If no links were generated, return an empty map
            if (_groupIndices.Count == 0) return newMap;

            // Sort the links by the group index
            PartialArray<int>.SortKeysAndItems(_groupIndices, _memberIndices);

            // Add links to immutable version in order by group, removing duplicates
            int currentgroup = _groupIndices[0];
            HashSet<int> linksForgroup = new HashSet<int>();

            for (int i = 0; i < _groupIndices.Count; ++i)
            {
                int group = _groupIndices[i];
                int member = _memberIndices[i];

                // If we're looking at a new group, reset the links already added
                // NOTE: new HashSet on each iteration much faster than HashSet.Clear().
                if (group != currentgroup)
                {
                    currentgroup = group;
                    linksForgroup = new HashSet<int>();
                }

                // Add a link for this member, if it wasn't already added
                if (linksForgroup.Add(member))
                {
                    newMap.AddLink(group, member);
                }
            }

            return newMap;
        }

        /// <summary>
        ///  Return the count of links in the Map (so far)
        /// </summary>
        public int Count
        {
            get { return this._memberIndices.Count; }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model.Map
{
    internal class ImmutableItemMap<T> : IBinarySerializable
    {
        internal IItemProvider<T> _provider;
        internal PartialArray<int> _firstMemberIndexForGroup;
        internal PartialArray<int> _memberIndices;

        public ImmutableItemMap(IItemProvider<T> provider)
        {
            this._provider = provider;
            this._firstMemberIndexForGroup = new PartialArray<int>();
            this._memberIndices = new PartialArray<int>();
        }

        ///  Add a link from one item to another. Links must be added to items in
        ///  order of insertion.
        /// </summary>
        /// <param name="groupIndex">Index of item from which to link</param>
        /// <param name="memberIndex">Index of item to which to link</param>
        public void AddLink(int groupIndex, int memberIndex)
        {
            // _firstMemberIndexForgroup[groupIndex] is the index in _memberIndices with the first member.

            // Fill _firstMemberIndexForgroup up to group index, if needed [add entries with no members]
            int currentSourceCount = this._firstMemberIndexForGroup.Count;
            int currentLinkTotal = this._memberIndices.Count;
            for (int i = currentSourceCount; i <= groupIndex; ++i)
            {
                this._firstMemberIndexForGroup.Add(currentLinkTotal);
            }

            // Add a member for this group.
            this._memberIndices.Add(memberIndex);
        }

        /// <summary>
        ///  Return the set of links from one item.
        /// </summary>
        /// <param name="sourceItemIndex">Index of item for which to get links.</param>
        /// <returns>PartialArrayRange of links for item.</returns>
        public MapEnumerator<T> LinksFrom(int sourceItemIndex)
        {
            int lastSourceIndex = this._firstMemberIndexForGroup.Count - 1;

            if (sourceItemIndex < lastSourceIndex)
            {
                return new MapEnumerator<T>(this, this._firstMemberIndexForGroup[sourceItemIndex], this._firstMemberIndexForGroup[sourceItemIndex + 1]);
            }
            else if (sourceItemIndex == lastSourceIndex)
            {
                return new MapEnumerator<T>(this, this._firstMemberIndexForGroup[sourceItemIndex], this._memberIndices.Count);
            }
            else
            {
                return new MapEnumerator<T>(this, 0, 0);
            }
        }

        #region IBinarySerializable
        public void ReadBinary(BinaryReader r)
        {
            this._firstMemberIndexForGroup.ReadBinary(r);
            this._memberIndices.ReadBinary(r);
        }

        public void WriteBinary(BinaryWriter w)
        {
            this._firstMemberIndexForGroup.WriteBinary(w);
            this._memberIndices.WriteBinary(w);
        }
        #endregion
    }
}

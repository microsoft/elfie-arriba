// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model.Map
{
    public class ItemMap<T> : IColumn, IBinarySerializable
    {
        internal IReadOnlyList<T> _provider;
        internal MutableItemMap<T> _mutableMap;
        internal ImmutableItemMap<T> _immutableMap;

        public ItemMap(IReadOnlyList<T> provider)
        {
            this._provider = provider;
            this.Clear();
        }

        public void Add()
        {
            // Nothing to add per item
        }

        public void SetCount(int count)
        {
            // Nothing to set automatically.
        }

        /// <summary>
        ///  Add a link from one item to another. Links must be added to items in
        ///  order of insertion.
        /// </summary>
        /// <param name="groupIndex">Index of item from which to link</param>
        /// <param name="memberIndex">Index of item to which to link</param>
        public void AddLink(int groupIndex, int memberIndex)
        {
            if (this._mutableMap == null) this._mutableMap = new MutableItemMap<T>(this._provider);
            this._mutableMap.AddLink(groupIndex, memberIndex);
        }

        /// <summary>
        ///  Return the set of links from one item.
        /// </summary>
        /// <param name="sourceItemIndex">Index of item for which to get links.</param>
        /// <returns>PartialArrayRange of links for item.</returns>
        public MapEnumerator<T> LinksFrom(int sourceItemIndex)
        {
            if (this._mutableMap != null) throw new InvalidOperationException();
            if (this._immutableMap == null) throw new InvalidOperationException();
            return this._immutableMap.LinksFrom(sourceItemIndex);
        }

        /// <summary>
        ///  Return the count of links from one item.
        /// </summary>
        /// <param name="sourceItemIndex">Index of item for which to get links.</param>
        /// <returns>Count of links from item.</returns>
        public int LinkCountFrom(int sourceItemIndex)
        {
            if (this._mutableMap != null) throw new InvalidOperationException();
            if (this._immutableMap == null) throw new InvalidOperationException();
            return this._immutableMap.LinkCountFrom(sourceItemIndex);
        }

        public int Count
        {
            get
            {
                int total = 0;
                if (this._immutableMap != null) total += this._immutableMap.Count;
                if (this._mutableMap != null) total += this._mutableMap.Count;
                return total;
            }
        }

        #region IColumn
        public void Clear()
        {
            this._immutableMap = null;
            this._mutableMap = null;
        }

        public void AddItem()
        {
            // Nothing is done on item add
        }

        public void ConvertToImmutable()
        {
            if (this._mutableMap != null)
            {
                // Convert or merge the maps
                this._immutableMap = this._mutableMap.ConvertToImmutable(this._immutableMap);

                // Clear the mutable map
                this._mutableMap = null;
            }
        }

        public void ReadBinary(BinaryReader r)
        {
            this._mutableMap = null;
            this._immutableMap = new ImmutableItemMap<T>(this._provider);
            this._immutableMap.ReadBinary(r);
        }

        public void WriteBinary(BinaryWriter w)
        {
            ConvertToImmutable();
            if (this._immutableMap == null) this._immutableMap = new ImmutableItemMap<T>(this._provider);
            this._immutableMap.WriteBinary(w);
        }
        #endregion
    }
}


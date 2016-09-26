// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model.Map
{
    public class ItemMap<T> : IBinarySerializable
    {
        internal IItemProvider<T> _provider;
        internal MutableItemMap<T> _mutableMap;
        internal ImmutableItemMap<T> _immutableMap;

        public ItemMap(IItemProvider<T> provider)
        {
            this._provider = provider;
            this._mutableMap = new MutableItemMap<T>(provider);
        }

        /// <summary>
        ///  Add a link from one item to another. Links must be added to items in
        ///  order of insertion.
        /// </summary>
        /// <param name="groupIndex">Index of item from which to link</param>
        /// <param name="memberIndex">Index of item to which to link</param>
        public void AddLink(int groupIndex, int memberIndex)
        {
            if (this._mutableMap == null) throw new InvalidOperationException();
            this._mutableMap.AddLink(groupIndex, memberIndex);
        }

        /// <summary>
        ///  Return the set of links from one item.
        /// </summary>
        /// <param name="sourceItemIndex">Index of item for which to get links.</param>
        /// <returns>PartialArrayRange of links for item.</returns>
        public MapEnumerator<T> LinksFrom(int sourceItemIndex)
        {
            if (this._immutableMap == null) throw new InvalidOperationException();
            return this._immutableMap.LinksFrom(sourceItemIndex);
        }

        public void ConvertToImmutable()
        {
            if (this._mutableMap != null)
            {
                // Need merging to provide this
                if (this._immutableMap != null) throw new NotImplementedException();

                this._immutableMap = this._mutableMap.ConvertToImmutable();
                this._mutableMap = null;
            }
        }

        public void ReadBinary(BinaryReader r)
        {
            this._immutableMap = new ImmutableItemMap<T>(this._provider);
            this._immutableMap.ReadBinary(r);
        }

        public void WriteBinary(BinaryWriter w)
        {
            ConvertToImmutable();
            this._immutableMap.WriteBinary(w);
        }
    }

    public struct MapEnumerator<U> : IEnumerator<U>
    {
        // ISSUE: Want to declare as nested class to access ItemMap privates, but getting a reliable IntelliSense crash in VS.
        private ImmutableItemMap<U> _map;
        private int _firstIndex;
        private int _currentIndex;
        private int _afterLastIndex;

        internal MapEnumerator(ImmutableItemMap<U> map, int firstIndex, int afterLastIndex)
        {
            this._map = map;
            this._firstIndex = firstIndex;
            this._currentIndex = firstIndex - 1;
            this._afterLastIndex = afterLastIndex;
        }

        public U Current
        {
            get { return this._map._provider[this._map._memberIndices[this._currentIndex]]; }
        }

        object IEnumerator.Current
        {
            get { return this._map._provider[this._map._memberIndices[this._currentIndex]]; }
        }

        public void Dispose()
        { }

        public bool MoveNext()
        {
            this._currentIndex++;
            return this._currentIndex < this._afterLastIndex;
        }

        public void Reset()
        {
            this._currentIndex = this._firstIndex - 1;
        }

        public int Count
        {
            get { return _afterLastIndex - _firstIndex; }
        }
    }
}


// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Elfie.Model.Map
{
    public struct MapEnumerator<U> : IEnumerator<U>
    {
        // ISSUE: Want to declare as nested class to access ItemMap privates, but getting a reliable IntelliSense crash in VS.
        private ImmutableItemMap<U> _map;
        private int _firstIndex;
        private int _currentIndex;
        private int _afterLastIndex;

        internal MapEnumerator(ImmutableItemMap<U> map, int firstIndex, int afterLastIndex)
        {
            _map = map;
            _firstIndex = firstIndex;
            _currentIndex = firstIndex - 1;
            _afterLastIndex = afterLastIndex;
        }

        public U Current
        {
            get { return _map._provider[_map._memberIndices[_currentIndex]]; }
        }

        public int CurrentIndex
        {
            get { return _map._memberIndices[_currentIndex]; }
        }

        object IEnumerator.Current
        {
            get { return _map._provider[_map._memberIndices[_currentIndex]]; }
        }

        public void Dispose()
        { }

        public bool MoveNext()
        {
            _currentIndex++;
            return _currentIndex < _afterLastIndex;
        }

        public void Reset()
        {
            _currentIndex = _firstIndex - 1;
        }

        public int Count
        {
            get { return _afterLastIndex - _firstIndex; }
        }
    }
}

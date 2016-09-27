// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Elfie.Extensions
{
    public static class IReadOnlyListExtensions
    {
        public static IEnumerator<T> GetDefaultEnumerator<T>(this IReadOnlyList<T> list)
        {
            return new ReadOnlyListEnumerator<T>(list);
        }
    }

    public class ReadOnlyListEnumerator<T> : IEnumerator<T>
    {
        private IReadOnlyList<T> _list;
        private int _index;

        public ReadOnlyListEnumerator(IReadOnlyList<T> list)
        {
            _list = list;
            _index = -1;
        }

        public T Current
        {
            get { return _list[_index]; }
        }

        object IEnumerator.Current
        {
            get { return _list[_index]; }
        }

        public void Dispose()
        { }

        public bool MoveNext()
        {
            _index++;
            return _index < _list.Count;
        }

        public void Reset()
        {
            _index = -1;
        }
    }
}

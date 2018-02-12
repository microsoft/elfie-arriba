// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;

using XForm.Columns;

namespace XForm.Data
{
    /// <summary>
    ///  SinglePageEnumerator is an IXArrayList which will only return the current single page
    ///  from the source to pipeline stages referencing it. Call 'SourceNext' to advance which page
    ///  the SinglePageEnumerator returns.
    ///  
    ///  Used to share an IXTable source with a second pipeline.
    /// </summary>
    public class SinglePageEnumerator : IXTable
    {
        private IXTable _source;
        private PagingColumn[] _columns;

        private int _currentPageCount;
        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public int CurrentRowCount { get; private set; }
        public int Count => _currentPageCount;

        public SinglePageEnumerator(IXTable source)
        {
            _source = source;
            _columns = source.Columns.Select((col) => new PagingColumn(col)).ToArray();
        }

        public IReadOnlyList<IXColumn> Columns => _columns;

        public int SourceNext(int desiredCount, CancellationToken cancellationToken)
        {
            Reset();

            // Get a page from the real source
            _currentPageCount = _source.Next(desiredCount, cancellationToken);

            return _currentPageCount;
        }

        public void Reset()
        {
            // Reset enumeration over the cached single page
            _currentEnumerateSelector = ArraySelector.All(_currentPageCount).Slice(0, 0);
        }

        public int Next(int desiredCount, CancellationToken cancellationToken)
        {
            // Iterate over the cached single page
            _currentEnumerateSelector = _currentEnumerateSelector.NextPage(_currentPageCount, desiredCount);
            _currentSelector = _currentEnumerateSelector;

            for (int i = 0; i < _columns.Length; ++i)
            {
                _columns[i].SetSelector(_currentEnumerateSelector);
            }

            return _currentEnumerateSelector.Count;
        }

        public void Dispose()
        {
            // Don't actually dispose anything
        }
    }
}

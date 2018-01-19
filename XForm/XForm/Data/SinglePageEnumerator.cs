// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

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

        private Func<XArray>[] _requestedGetters;
        private XArray[] _columnarrays;

        private int _currentPageCount;
        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public IReadOnlyList<ColumnDetails> Columns => _source.Columns;
        public int CurrentRowCount { get; private set; }
        public int Count => _currentPageCount;

        public SinglePageEnumerator(IXTable source)
        {
            _source = source;
            _requestedGetters = new Func<XArray>[source.Columns.Count];
            _columnarrays = new XArray[source.Columns.Count];
        }

        public int SourceNext(int desiredCount)
        {
            Reset();

            // Get a page from the real source
            _currentPageCount = _source.Next(desiredCount);

            // Clear cached arrays from last page
            Array.Clear(_columnarrays, 0, _columnarrays.Length);

            return _currentPageCount;
        }

        public Func<XArray> ColumnGetter(int columnIndex)
        {
            // Get and cache the real getter for this column, so the source knows to retrieve it
            if (_requestedGetters[columnIndex] == null) _requestedGetters[columnIndex] = _source.ColumnGetter(columnIndex);

            // Declare a remap array in case it's needed
            int[] remapArray = null;

            // Return the previously retrieved XArray for this page only
            return () =>
            {
                // Get the XArray for these rows, if we haven't before
                if (_columnarrays[columnIndex].Array == null) _columnarrays[columnIndex] = _requestedGetters[columnIndex]();

                // Get the values for the slice of rows currently being returned
                XArray raw = _columnarrays[columnIndex];
                return raw.Select(_currentSelector, ref remapArray);
            };
        }

        public void Reset()
        {
            // Reset enumeration over the cached single page
            _currentEnumerateSelector = ArraySelector.All(_currentPageCount).Slice(0, 0);
        }

        public int Next(int desiredCount)
        {
            // Iterate over the cached single page
            _currentEnumerateSelector = _currentEnumerateSelector.NextPage(_currentPageCount, desiredCount);
            _currentSelector = _currentEnumerateSelector;
            return _currentEnumerateSelector.Count;
        }

        public void Dispose()
        {
            // Don't actually dispose anything
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace XForm.Data
{
    /// <summary>
    ///  SinglePageEnumerator is an IDataBatchList which will only return the current single page
    ///  from the source to pipeline stages referencing it. Call 'SourceNext' to advance which page
    ///  the SinglePageEnumerator returns.
    ///  
    ///  Used to share an IDataBatchEnumerator source with a second pipeline.
    /// </summary>
    public class SinglePageEnumerator : IDataBatchList
    {
        private IDataBatchEnumerator _source;

        private Func<DataBatch>[] _requestedGetters;
        private DataBatch[] _columnBatches;

        private int _currentPageCount;
        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public IReadOnlyList<ColumnDetails> Columns => _source.Columns;
        public int CurrentBatchRowCount { get; private set; }
        public int Count => _currentPageCount;

        public SinglePageEnumerator(IDataBatchEnumerator source)
        {
            _source = source;
            _requestedGetters = new Func<DataBatch>[source.Columns.Count];
            _columnBatches = new DataBatch[source.Columns.Count];
        }

        public int SourceNext(int desiredCount)
        {
            Reset();

            // Get a page from the real source
            _currentPageCount = _source.Next(desiredCount);

            // Clear cached DataBatches from last page
            Array.Clear(_columnBatches, 0, _columnBatches.Length);

            return _currentPageCount;
        }

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Get and cache the real getter for this column, so the source knows to retrieve it
            if (_requestedGetters[columnIndex] == null) _requestedGetters[columnIndex] = _source.ColumnGetter(columnIndex);

            // Declare a remap array in case it's needed
            int[] remapArray = null;

            // Return the previously retrieved DataBatch for this page only
            return () =>
            {
                // Get the DataBatch for these rows, if we haven't before
                if (_columnBatches[columnIndex].Array == null) _columnBatches[columnIndex] = _requestedGetters[columnIndex]();

                // Get the values for the slice of rows currently being returned
                DataBatch raw = _columnBatches[columnIndex];
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

        public void Get(ArraySelector selector)
        {
            _currentSelector = selector;
        }

        public void Dispose()
        {
            // Don't actually dispose anything
        }
    }
}

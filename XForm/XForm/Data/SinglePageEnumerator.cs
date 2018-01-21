// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace XForm.Data
{
    internal class ColumnPager : IXColumn
    {
        private IXTable _table;
        private IXColumn _column;

        public ColumnPager(IXTable table, IXColumn column)
        {
            _table = table;
            _column = column;
        }

        public ColumnDetails ColumnDetails => _column.ColumnDetails;

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> sourceGetter = _column.CurrentGetter();
            int[] remapArray = null;

            return () => sourceGetter().Select(_table.CurrentSelector, ref remapArray);
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            // Seek is blocked by SinglePageEnumerator
            return null;
        }

        public Func<XArray> ValuesGetter()
        {
            return _column.ValuesGetter();
        }

        public Type IndicesType => _column.IndicesType;

        public Func<XArray> IndicesCurrentGetter()
        {
            Func<XArray> sourceGetter = _column.IndicesCurrentGetter();
            if (sourceGetter == null) return null;

            int[] remapArray = null;
            return () => sourceGetter().Select(_table.CurrentSelector, ref remapArray);
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            // Seek is blocked by SinglePageEnumerator
            return null;
        }
    }

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
        private ColumnPager[] _columns;

        private int _currentPageCount;
        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public int CurrentRowCount { get; private set; }
        public int Count => _currentPageCount;

        public SinglePageEnumerator(IXTable source)
        {
            _source = source;
            _columns = source.Columns.Select((col) => new ColumnPager(this, col)).ToArray();
        }

        public ArraySelector CurrentSelector => _currentEnumerateSelector;
        public IReadOnlyList<IXColumn> Columns => _columns;

        public int SourceNext(int desiredCount)
        {
            Reset();

            // Get a page from the real source
            _currentPageCount = _source.Next(desiredCount);

            return _currentPageCount;
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

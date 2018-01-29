// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Columns
{
    /// <summary>
    ///  PagingColumn pages through an inner array based on a set selector.
    /// </summary>
    internal class PagingColumn : IXColumn
    {
        private ArraySelector _currentSelector;
        private IXColumn _column;

        public PagingColumn(IXColumn column)
        {
            _column = column;
        }

        public void SetSelector(ArraySelector currentSelector)
        {
            _currentSelector = currentSelector;
        }

        public ColumnDetails ColumnDetails => _column.ColumnDetails;

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> sourceGetter = _column.CurrentGetter();
            int[] remapArray = null;

            return () => sourceGetter().Select(_currentSelector, ref remapArray);
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
            return () => sourceGetter().Select(_currentSelector, ref remapArray);
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            // Seek is blocked by SinglePageEnumerator
            return null;
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }

        public override string ToString()
        {
            return _column.ToString();
        }
    }
}

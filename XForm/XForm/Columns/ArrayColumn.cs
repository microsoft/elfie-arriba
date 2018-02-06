// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Columns
{
    /// <summary>
    ///  ArrayColumn is an IXColumn wrapping an available array.
    /// </summary>
    public class ArrayColumn : IXColumn
    {
        private ArraySelector _currentSelector;
        private XArray _allValues;
        public ColumnDetails ColumnDetails { get; private set; }

        private int[] _remapArray;

        public ArrayColumn(XArray allValues, ColumnDetails columnDetails)
        {
            _allValues = allValues;
            ColumnDetails = columnDetails;
        }

        public void SetSelector(ArraySelector currentSelector)
        {
            _currentSelector = currentSelector;
        }

        public Func<XArray> CurrentGetter()
        {
            return () => _allValues.Select(_currentSelector, ref _remapArray);
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            return (selector) => _allValues.Reselect(selector);
        }

        public Func<XArray> ValuesGetter()
        {
            return null;
        }

        public Type IndicesType => null;

        public Func<XArray> IndicesCurrentGetter()
        {
            return null;
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return null;
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }
    }
}

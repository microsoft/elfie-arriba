// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Columns
{
    /// <summary>
    ///  DeferredArrayColumn wraps an array not available until Next() is first called.
    /// </summary>
    public class DeferredArrayColumn : IXColumn
    {
        private ArraySelector _currentSelector;
        private XArray _allValues;
        public ColumnDetails ColumnDetails { get; private set; }

        private int[] _remapArray;

        public DeferredArrayColumn(ColumnDetails columnDetails)
        {
            ColumnDetails = columnDetails;
            _allValues = XArray.Empty;
        }

        public void SetValues(XArray values)
        {
            _allValues = values;
        }

        public void SetSelector(ArraySelector currentSelector)
        {
            if (currentSelector.Count != 0 && _allValues.Array == null) throw new InvalidOperationException("SetValues must be called before SetSelector on DeferredArrayColumn.");
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

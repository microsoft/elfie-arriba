// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Columns
{
    /// <summary>
    ///  SeekedColumn seeks to specific rows in Current set by 'Set'.
    /// </summary>
    public class SeekedColumn : IXColumn
    {
        private IXColumn _column;
        private ArraySelector _currentSelector;

        public SeekedColumn(IXColumn column)
        {
            _column = column;
        }

        public void Set(ArraySelector current)
        {
            _currentSelector = current;
        }

        public ColumnDetails ColumnDetails => _column.ColumnDetails;
        public Type IndicesType => _column.IndicesType;

        public Func<XArray> CurrentGetter()
        {
            // 'Current' will seek to the selected rows instead
            Func<ArraySelector, XArray> sourceSeeker = _column.SeekGetter();
            if (sourceSeeker == null) return null;

            return () => sourceSeeker(_currentSelector);
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            // Seek is disabled by a Seeking column (can't double seek)
            return null;
        }

        public Func<XArray> ValuesGetter()
        {
            return _column.ValuesGetter();
        }

        public Func<XArray> IndicesCurrentGetter()
        {
            // 'Current' will seek to the selected rows instead
            Func<ArraySelector, XArray> sourceSeeker = _column.IndicesSeekGetter();
            if (sourceSeeker == null) return null;

            return () => sourceSeeker(_currentSelector);
        }

        public Func<object> ComponentGetter(string componentName)
        {
            // Components in underlying column won't be seeked
            return null;
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            // Seek is disabled by a Seeking column (can't double seek)
            return null;
        }
    }
}

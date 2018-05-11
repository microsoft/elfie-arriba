// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Transforms;

namespace XForm.Columns
{
    /// <summary>
    ///  RemappedColumn is an IXColumn wrapper which remaps the returned values based on
    ///  a remapper filtered set.
    ///  
    ///  It is used by Where, Join, and Choose to filter rows to the set matching the verb.
    /// </summary>
    public class RemappedColumn : IXColumn
    {
        private IXColumn _column;
        private RowRemapper _remapper;

        // TODO: Re-add requesting more than the current desired count and paging through

        public RemappedColumn(IXColumn column, RowRemapper remapper)
        {
            _column = column;
            _remapper = remapper;
        }

        public ColumnDetails ColumnDetails => _column.ColumnDetails;

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> sourceGetter = _column.CurrentGetter();
            int[] remapArray = null;

            return () => _remapper.Remap(sourceGetter(), ref remapArray);
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            // Seeking isn't supported on remapped columns
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
            return () => _remapper.Remap(sourceGetter(), ref remapArray);
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            // Seeking isn't supported on remapped columns
            return null;
        }

        public Func<object> ComponentGetter(string componentName)
        {
            // Components in underlying column won't be remapped
            return null;
        }

        public override string ToString()
        {
            return _column.ToString();
        }
    }
}

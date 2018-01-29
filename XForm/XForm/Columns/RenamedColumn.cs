// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Query;

namespace XForm.Columns
{
    /// <summary>
    ///  Rename an underlying column and pass through other members.
    /// </summary>
    public class RenamedColumn : IXColumn
    {
        private IXColumn _column;
        public ColumnDetails ColumnDetails { get; private set; }

        private RenamedColumn(IXColumn column, string newName)
        {
            _column = column;
            ColumnDetails = column.ColumnDetails.Rename(newName);
        }

        public static IXColumn Build(IXColumn column, string newName)
        {
            if (column.ColumnDetails.Name.Equals(newName)) return column;
            return new RenamedColumn(column, newName);
        }

        public Func<XArray> CurrentGetter()
        {
            return _column.CurrentGetter();
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            return _column.SeekGetter();
        }

        public Func<XArray> ValuesGetter()
        {
            return _column.ValuesGetter();
        }

        public Type IndicesType => _column.IndicesType;

        public Func<XArray> IndicesCurrentGetter()
        {
            return _column.IndicesCurrentGetter();
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return _column.IndicesSeekGetter();
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return _column.ComponentGetter(componentName);
        }

        public override string ToString()
        {
            return $"{_column} AS {XqlScanner.Escape(ColumnDetails.Name, TokenType.ColumnName)}";
        }
    }
}

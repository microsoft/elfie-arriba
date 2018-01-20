// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Functions
{
    /// <summary>
    ///  Rename an underlying column and pass through other members.
    /// </summary>
    public class RenamedColumn : IXColumn
    {
        private IXColumn Column { get; set; }
        public ColumnDetails ColumnDetails { get; private set; }

        private RenamedColumn(IXColumn column, string newName)
        {
            Column = column;
            ColumnDetails = column.ColumnDetails.Rename(newName);
        }

        public static IXColumn Build(IXColumn column, string newName)
        {
            if (column.ColumnDetails.Name.Equals(newName)) return column;
            return new RenamedColumn(column, newName);
        }

        public ArraySelector CurrentSelector => Column.CurrentSelector;

        public Func<XArray> CurrentGetter()
        {
            return Column.CurrentGetter();
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            return Column.SeekGetter();
        }

        public Func<XArray> ValuesGetter()
        {
            return Column.ValuesGetter();
        }

        public Func<ArraySelector, XArray> IndicesGetter()
        {
            return Column.IndicesGetter();
        }
    }
}

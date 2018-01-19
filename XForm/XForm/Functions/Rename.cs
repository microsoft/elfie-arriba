// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Functions
{
    public class Rename : IXColumn
    {
        private IXColumn Column { get; set; }
        public ColumnDetails ColumnDetails { get; private set; }

        public Rename(IXColumn column, string newName)
        {
            Column = column;
            ColumnDetails = column.ColumnDetails.Rename(newName);
        }

        public Func<XArray> Getter()
        {
            return Column.Getter();
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Extensions;

namespace XForm.Transforms
{
    public class ColumnSelector : DataBatchEnumeratorWrapper
    {
        private List<ColumnDetails> _mappedColumns;
        private List<int> _columnInnerIndices;

        public ColumnSelector(IDataBatchEnumerator source, IEnumerable<string> columnNames) : base(source)
        {
            _mappedColumns = new List<ColumnDetails>();
            _columnInnerIndices = new List<int>();

            var sourceColumns = _source.Columns;
            foreach (string columnName in columnNames)
            {
                int index = sourceColumns.IndexOfColumn(columnName);
                _columnInnerIndices.Add(index);
                _mappedColumns.Add(sourceColumns[index]);
            }
        }

        public override IReadOnlyList<ColumnDetails> Columns => _mappedColumns;

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return _source.ColumnGetter(_columnInnerIndices[columnIndex]);
        }
    }
}

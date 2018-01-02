// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Functions
{
    public class Column : IDataBatchColumn
    {
        private string ColumnName { get; set; }
        private int ColumnIndex { get; set; }
        private IDataBatchEnumerator Source { get; set; }

        public ColumnDetails ColumnDetails => Source.Columns[ColumnIndex];

        public Column(IDataBatchEnumerator source, WorkflowContext context)
        {
            ColumnName = context.Parser.NextColumnName(source);
            ColumnIndex = source.Columns.IndexOfColumn(ColumnName);
            Source = source;
        }

        public Func<DataBatch> Getter()
        {
            return Source.ColumnGetter(ColumnIndex);
        }

        public override string ToString()
        {
            return XqlScanner.Escape(ColumnName, TokenType.ColumnName);
        }
    }
}

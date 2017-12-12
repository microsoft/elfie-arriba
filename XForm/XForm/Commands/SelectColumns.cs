// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Commands
{
    internal class SelectColumnsCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "columns";
        public string Usage => "'columns' [ColumnName], [ColumnName], ...";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            List<string> columnNames = new List<string>();
            do
            {
                columnNames.Add(context.Parser.NextColumnName(source));
            } while (context.Parser.HasAnotherPart);

            return new SelectColumns(source, columnNames);
        }
    }

    public class SelectColumns : DataBatchEnumeratorWrapper
    {
        private List<ColumnDetails> _mappedColumns;
        private List<int> _columnInnerIndices;

        public SelectColumns(IDataBatchEnumerator source, IEnumerable<string> columnNames) : base(source)
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

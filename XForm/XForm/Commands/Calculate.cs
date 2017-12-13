// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Commands
{
    internal class CalculateCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "calculate";
        public string Usage => "'calculate' [newColumnName] functionName([args])";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new Calculate(source,
                context.Parser.NextOutputColumnName(source),
                context.Parser.NextColumn(source, context));
        }
    }

    public class Calculate : DataBatchEnumeratorWrapper
    {
        private int _computedColumnIndex;
        private IDataBatchColumn _calculatedColumn;
        private List<ColumnDetails> _columns;

        public Calculate(IDataBatchEnumerator source, string outputColumnName, IDataBatchColumn column) : base(source)
        {
            _calculatedColumn = column;
            _columns = new List<ColumnDetails>(source.Columns);

            // Determine whether we're replacing or adding a column
            if (source.Columns.TryGetIndexOfColumn(outputColumnName, out _computedColumnIndex))
            {
                _columns[_computedColumnIndex] = _calculatedColumn.ColumnDetails.Rename(outputColumnName);
            }
            else
            {
                _columns.Add(_calculatedColumn.ColumnDetails.Rename(outputColumnName));
                _computedColumnIndex = source.Columns.Count;
            }
        }

        public override IReadOnlyList<ColumnDetails> Columns => _columns;

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Pass through columns other than the one being calculated
            if (columnIndex != _computedColumnIndex) return _source.ColumnGetter(columnIndex);

            // Otherwise, pass on the calculation
            return _calculatedColumn.Getter();
        }
    }
}

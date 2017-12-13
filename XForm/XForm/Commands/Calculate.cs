// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Extensions;
using XForm.Functions;
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
                context);
        }
    }

    public class Calculate : DataBatchEnumeratorWrapper
    {
        private int _computedColumnIndex;
        private IDataBatchFunction _function;
        private List<ColumnDetails> _columns;
        private int _currentCount;

        public Calculate(IDataBatchEnumerator source, string outputColumnName, WorkflowContext context) : base(source)
        {
            _function = context.Parser.NextFunction(source, context);
            _columns = new List<ColumnDetails>(source.Columns);

            // Determine whether we're replacing or adding a column
            if (source.Columns.TryGetIndexOfColumn(outputColumnName, out _computedColumnIndex))
            {
                _columns[_computedColumnIndex] = _function.ReturnType.Rename(outputColumnName);
            }
            else
            {
                _columns.Add(_function.ReturnType.Rename(outputColumnName));
                _computedColumnIndex = source.Columns.Count;
            }
        }

        public override IReadOnlyList<ColumnDetails> Columns => _columns;

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Pass through columns other than the one being calculated
            if (columnIndex != _computedColumnIndex) return _source.ColumnGetter(columnIndex);

            // Otherwise, pass on the calculation
            Func<int, DataBatch> getter = _function.Getter();
            return () => getter(_currentCount);
        }

        public override int Next(int desiredCount)
        {
            _currentCount = base.Next(desiredCount);
            return _currentCount;
        }
    }
}

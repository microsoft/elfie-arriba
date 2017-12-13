// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
            List<IDataBatchColumn> columns = new List<IDataBatchColumn>();
            do
            {
                IDataBatchColumn column = context.Parser.NextColumn(source, context);
                columns.Add(column);
                if (String.IsNullOrEmpty(column.ColumnDetails.Name)) throw new UsageException($"Column {columns.Count} passed to 'Column' wasn't assigned a name. Use 'AS [Name]' to assign names to every column selected.");
            } while (context.Parser.HasAnotherPart);

            return new SelectColumns(source, columns);
        }
    }

    public class SelectColumns : DataBatchEnumeratorWrapper
    {
        private List<IDataBatchColumn> _columns;
        private List<ColumnDetails> _details;

        public SelectColumns(IDataBatchEnumerator source, List<IDataBatchColumn> columns) : base(source)
        {
            _columns = columns;
            _details = new List<ColumnDetails>(columns.Select((col) => col.ColumnDetails));
        }

        public override IReadOnlyList<ColumnDetails> Columns => _details;

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return _columns[columnIndex].Getter();
        }
    }
}

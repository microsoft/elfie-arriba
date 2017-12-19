// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using XForm.Data;
using XForm.Query;

namespace XForm.Commands
{
    internal class SelectCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "select";
        public string Usage => "'select' [ColumnFunctionOrLiteral] (AS [Name])?, [ColumnFunctionOrLiteral], ...";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            List<IDataBatchColumn> columns = new List<IDataBatchColumn>();
            do
            {
                IDataBatchColumn column = context.Parser.NextColumn(source, context);
                columns.Add(column);
                if (String.IsNullOrEmpty(column.ColumnDetails.Name)) throw new ArgumentException($"Column {columns.Count} passed to 'Column' wasn't assigned a name. Use 'AS [Name]' to assign names to every column selected.");
            } while (context.Parser.HasAnotherPart);

            return new Select(source, columns);
        }
    }

    public class Select : DataBatchEnumeratorWrapper
    {
        private List<IDataBatchColumn> _columns;
        private List<ColumnDetails> _details;

        public Select(IDataBatchEnumerator source, List<IDataBatchColumn> columns) : base(source)
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

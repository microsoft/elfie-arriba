﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Verbs
{
    internal class RemoveCommandBuilder : IVerbBuilder
    {
        public string Verb => "remove";
        public string Usage => "'remove' [ColumnName], [ColumnName], ...";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            List<string> columnNames = new List<string>();
            while (context.Parser.HasAnotherPart)
            {
                columnNames.Add(context.Parser.NextColumnName(source));
            }

            return new Remove(source, columnNames);
        }
    }

    public class Remove : DataBatchEnumeratorWrapper
    {
        private List<ColumnDetails> _mappedColumns;
        private List<int> _columnInnerIndices;

        public Remove(IDataBatchEnumerator source, IEnumerable<string> columnNames) : base(source)
        {
            _mappedColumns = new List<ColumnDetails>();
            _columnInnerIndices = new List<int>();

            // Find the columns 
            HashSet<string> columnsToRemove = new HashSet<string>(columnNames, StringComparer.OrdinalIgnoreCase);
            foreach (ColumnDetails column in _source.Columns)
            {
                if (columnsToRemove.Contains(column.Name))
                {
                    columnsToRemove.Remove(column.Name);
                }
                else
                {
                    _columnInnerIndices.Add(_mappedColumns.Count);
                    _mappedColumns.Add(column);
                }
            }

            // Validate all columns to remove were found
            if (columnsToRemove.Count > 0) throw new ColumnNotFoundException($"Columns not found to remove: {string.Join(", ", columnsToRemove)}. Columns Available: {string.Join(", ", _mappedColumns.Select((cd) => cd.Name))}");
        }

        public override IReadOnlyList<ColumnDetails> Columns => _mappedColumns;

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return _source.ColumnGetter(_columnInnerIndices[columnIndex]);
        }
    }
}

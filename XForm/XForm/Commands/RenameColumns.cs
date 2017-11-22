// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Query;

namespace XForm.Commands
{
    internal class RenameColumnsCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "renameColumns" };
        public string Usage => "'renameColumns' [ColumnName] [NewColumnName], [ColumnName] [NewColumnName], ...";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            Dictionary<string, string> columnNameMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (!parser.IsLastLinePart)
            {
                columnNameMappings[parser.NextColumnName(source)] = parser.NextString();
            }

            return new RenameColumns(source, columnNameMappings);
        }
    }

    public class RenameColumns : DataBatchEnumeratorWrapper
    {
        private List<ColumnDetails> _mappedColumns;

        public RenameColumns(IDataBatchEnumerator source, Dictionary<string, string> columnNameMappings) : base(source)
        {
            _mappedColumns = new List<ColumnDetails>();

            foreach(ColumnDetails column in _source.Columns)
            {
                ColumnDetails mapped = column;
                string newName;
                if (columnNameMappings.TryGetValue(column.Name, out newName)) mapped = column.Rename(newName);

                _mappedColumns.Add(mapped);
            }
        }

        public override IReadOnlyList<ColumnDetails> Columns => _mappedColumns;
    }
}

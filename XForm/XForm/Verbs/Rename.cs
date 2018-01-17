// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Query;

namespace XForm.Verbs
{
    internal class RenameCommandBuilder : IVerbBuilder
    {
        public string Verb => "rename";
        public string Usage => "rename {Column} {NewName}, ...";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, XDatabaseContext context)
        {
            Dictionary<string, string> columnNameMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (context.Parser.HasAnotherPart)
            {
                columnNameMappings[context.Parser.NextColumnName(source)] = context.Parser.NextOutputColumnName(source);
            }

            return new Rename(source, columnNameMappings);
        }
    }

    public class Rename : DataBatchEnumeratorWrapper
    {
        private List<ColumnDetails> _mappedColumns;

        public Rename(IDataBatchEnumerator source, Dictionary<string, string> columnNameMappings) : base(source)
        {
            _mappedColumns = new List<ColumnDetails>();

            foreach (ColumnDetails column in _source.Columns)
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

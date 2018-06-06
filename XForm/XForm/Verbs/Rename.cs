// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Columns;
using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Verbs
{
    internal class RenameCommandBuilder : IVerbBuilder
    {
        public string Verb => "rename";
        public string Usage => "rename {Column} {NewName}, ...";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            Dictionary<string, string> columnNameMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (context.Parser.HasAnotherPart)
            {
                columnNameMappings[context.Parser.NextColumnName(source)] = context.Parser.NextOutputColumnName(source);
            }

            // Rename can be evaluated in parallel, so keep parallel
            return source.WrapParallel(context.Parser, (part) => new Rename(part, columnNameMappings));
        }
    }

    public class Rename : XTableWrapper
    {
        private List<IXColumn> _mappedColumns;

        public Rename(IXTable source, Dictionary<string, string> columnNameMappings) : base(source)
        {
            _mappedColumns = new List<IXColumn>();

            foreach (IXColumn column in _source.Columns)
            {
                IXColumn mapped = column;

                string newName;
                if (columnNameMappings.TryGetValue(column.ColumnDetails.Name, out newName)) mapped = RenamedColumn.Build(column, newName);

                _mappedColumns.Add(mapped);
            }
        }

        public override IReadOnlyList<IXColumn> Columns => _mappedColumns;
    }
}

// Copyright (c) Microsoft. All rights reserved.
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
        public string Usage => "remove {Column}, ...";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            List<string> columnNames = new List<string>();
            while (context.Parser.HasAnotherPart)
            {
                columnNames.Add(context.Parser.NextColumnName(source));
            }

            // Remove can be evaluated in parallel, so keep parallel
            return source.WrapParallel(context.Parser, (part) => new Remove(part, columnNames));
        }
    }

    public class Remove : XTableWrapper
    {
        private List<IXColumn> _remainingColumns;

        public Remove(IXTable source, IEnumerable<string> columnNames) : base(source)
        {
            _remainingColumns = new List<IXColumn>();

            // Find the columns 
            HashSet<string> columnsToRemove = new HashSet<string>(columnNames, StringComparer.OrdinalIgnoreCase);
            foreach (IXColumn column in _source.Columns)
            {
                if (!columnsToRemove.Remove(column.ColumnDetails.Name))
                {
                    _remainingColumns.Add(column);
                }
            }

            // Validate all columns to remove were found
            if (columnsToRemove.Count > 0) throw new ColumnNotFoundException($"Columns not found to remove: {string.Join(", ", columnsToRemove)}. Columns Available: {string.Join(", ", _remainingColumns.Select((col) => col.ColumnDetails.Name))}");
        }

        public override IReadOnlyList<IXColumn> Columns => _remainingColumns;
    }
}

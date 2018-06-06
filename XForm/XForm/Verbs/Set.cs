// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using XForm.Columns;
using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Verbs
{
    internal class SetCommandBuilder : IVerbBuilder
    {
        public string Verb => "set";
        public string Usage => "set {NewName} {Col|Func|Const}";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            // Set can be evaluated in parallel, so keep parallel
            return source.WrapParallel(context.Parser, (part) =>
                new Set(
                    part,
                    context.Parser.NextOutputColumnName(part),
                    context.Parser.NextColumn(part, context))
            );
        }
    }

    public class Set : XTableWrapper
    {
        private int _computedColumnIndex;
        private IXColumn _calculatedColumn;
        private List<IXColumn> _columns;

        public Set(IXTable source, string outputColumnName, IXColumn column) : base(source)
        {
            _calculatedColumn = column;
            _columns = new List<IXColumn>(source.Columns);

            // Determine whether we're replacing or adding a column
            if (source.Columns.TryGetIndexOfColumn(outputColumnName, out _computedColumnIndex))
            {
                _columns[_computedColumnIndex] = RenamedColumn.Build(_calculatedColumn, outputColumnName);
            }
            else
            {
                _columns.Add(RenamedColumn.Build(_calculatedColumn, outputColumnName));
                _computedColumnIndex = source.Columns.Count;
            }
        }

        public override IReadOnlyList<IXColumn> Columns => _columns;
    }
}

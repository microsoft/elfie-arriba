// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using XForm.Data;
using XForm.Query;

namespace XForm.Verbs
{
    internal class SelectCommandBuilder : IVerbBuilder
    {
        public string Verb => "select";
        public string Usage => "select {Col|Func|Const} (AS {Name})?, ...";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            List<IXColumn> columns = new List<IXColumn>();
            do
            {
                IXColumn column = context.Parser.NextColumn(source, context);
                columns.Add(column);
                if (String.IsNullOrEmpty(column.ColumnDetails.Name)) throw new ArgumentException($"Column {columns.Count} passed to 'Column' wasn't assigned a name. Use 'AS [Name]' to assign names to every column selected.");
            } while (context.Parser.HasAnotherPart);

            return new Select(source, columns);
        }
    }

    public class Select : XTableWrapper
    {
        private List<IXColumn> _columns;

        public Select(IXTable source, List<IXColumn> columns) : base(source)
        {
            _columns = columns;
        }

        public override IReadOnlyList<IXColumn> Columns => _columns;
    }
}

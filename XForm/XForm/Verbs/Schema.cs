// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using XForm.Data;
using XForm.Query;

namespace XForm.Verbs
{
    internal class SchemaCommandBuilder : IVerbBuilder
    {
        public string Verb => "schema";
        public string Usage => "schema";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            return new SchemaTransformer(source);
        }
    }

    /// <summary>
    ///  Return the Schema of a Source (Column Name, Type, Nullable).
    /// </summary>
    public class SchemaTransformer : XTableWrapper
    {
        private ColumnDetails[] _columns;
        private XArray[] _results;

        public SchemaTransformer(IXTable source) : base(source)
        {
            _columns = new ColumnDetails[2];
            _columns[0] = new ColumnDetails("Name", typeof(string));
            _columns[1] = new ColumnDetails("Type", typeof(string));
        }

        public override IReadOnlyList<ColumnDetails> Columns => _columns;

        public override Func<XArray> ColumnGetter(int columnIndex)
        {
            return () => _results[columnIndex];
        }

        public override void Reset()
        {
            _results = null;
        }

        public override int Next(int desiredCount)
        {
            if (_results == null)
            {
                _results = new XArray[3];
                _results[0] = XArray.All(_source.Columns.Select((cd) => cd.Name).ToArray());
                _results[1] = XArray.All(_source.Columns.Select((cd) => cd.Type.Name.ToString()).ToArray());

                return _source.Columns.Count;
            }

            return 0;
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;

using XForm.Columns;
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
        private ArrayColumn[] _columns;
        private ArraySelector _enumerateSelector;

        public SchemaTransformer(IXTable source) : base(source)
        {
            _columns = new ArrayColumn[2];
            _columns[0] = new ArrayColumn(XArray.All(_source.Columns.Select((col) => col.ColumnDetails.Name).ToArray()), new ColumnDetails("Name", typeof(string)));
            _columns[1] = new ArrayColumn(XArray.All(_source.Columns.Select((col) => col.ColumnDetails.Type.Name.ToString()).ToArray()), new ColumnDetails("Type", typeof(string)));

            _enumerateSelector = ArraySelector.All(0);
        }

        public override IReadOnlyList<IXColumn> Columns => _columns;

        public override void Reset()
        {
            _enumerateSelector = ArraySelector.All(0);

            _columns[0].SetSelector(_enumerateSelector);
            _columns[1].SetSelector(_enumerateSelector);
        }

        public override int Next(int desiredCount, CancellationToken cancellationToken)
        {
            _enumerateSelector = _enumerateSelector.NextPage(_source.Columns.Count, desiredCount);

            _columns[0].SetSelector(_enumerateSelector);
            _columns[1].SetSelector(_enumerateSelector);

            return _enumerateSelector.Count;
        }
    }
}

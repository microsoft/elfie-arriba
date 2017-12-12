// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using XForm.Data;
using XForm.Query;

namespace XForm.Commands
{
    internal class SchemaCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "schema";
        public string Usage => "'schema'";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new SchemaTransformer(source);
        }
    }

    /// <summary>
    ///  Return the Schema of a Source (Column Name, Type, Nullable).
    /// </summary>
    public class SchemaTransformer : DataBatchEnumeratorWrapper
    {
        private ColumnDetails[] _columns;
        private DataBatch[] _results;

        public SchemaTransformer(IDataBatchEnumerator source) : base(source)
        {
            _columns = new ColumnDetails[3];
            _columns[0] = new ColumnDetails("Name", typeof(string), false);
            _columns[1] = new ColumnDetails("Type", typeof(string), false);
            _columns[2] = new ColumnDetails("Nullable", typeof(bool), false);
        }

        public override IReadOnlyList<ColumnDetails> Columns => _columns;

        public override Func<DataBatch> ColumnGetter(int columnIndex)
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
                _results = new DataBatch[3];
                _results[0] = DataBatch.All(_source.Columns.Select((cd) => cd.Name).ToArray());
                _results[1] = DataBatch.All(_source.Columns.Select((cd) => cd.Type.Name.ToString()).ToArray());
                _results[2] = DataBatch.All(_source.Columns.Select((cd) => cd.Nullable).ToArray());

                return _source.Columns.Count;
            }

            return 0;
        }
    }
}

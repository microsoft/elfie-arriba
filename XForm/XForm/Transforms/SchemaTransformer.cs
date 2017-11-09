using System;
using System.Collections.Generic;
using System.Linq;
using XForm.Data;

namespace XForm.Transforms
{
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

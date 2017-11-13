using System;
using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Transforms
{
    public class WhereFilter : DataBatchEnumeratorWrapper
    {
        private int _filterColumnIndex;
        private Func<DataBatch> _filterColumnGetter;
        private Action<DataBatch, RowRemapper> _comparer;
        private RowRemapper _mapper;

        public WhereFilter(IDataBatchEnumerator source, string columnName, CompareOperator op, object value) : base(source)
        {
            // Find the column we're filtering on
            _filterColumnIndex = source.Columns.IndexOfColumn(columnName);
            ColumnDetails filterColumn = _source.Columns[_filterColumnIndex];

            // Cache the ColumnGetter
            this._filterColumnGetter = source.ColumnGetter(_filterColumnIndex);

            // Build a Comparer for the desired type and get the function for the desired compare operator
            this._comparer = ComparerFactory.Build(filterColumn.Type, op, TypeConverterFactory.ConvertSingle(value, filterColumn.Type));

            // Build a mapper to hold matching rows and remap source batches
            this._mapper = new RowRemapper();
        }

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Keep a column-specific array for remapping indices
            int[] remapArray = null;

            // Retrieve the column getter for this column
            Func<DataBatch> getter = (columnIndex == _filterColumnIndex ? _filterColumnGetter : _source.ColumnGetter(columnIndex));

            return () =>
            {
                // Get the batch from the source for this column
                DataBatch batch = getter();

                // Remap the DataBatch indices for this column for the rows which matched the clause
                return _mapper.Remap(batch, ref remapArray);
            };
        }

        public override int Next(int desiredCount)
        {
            while(_source.Next(desiredCount) > 0)
            {
                // Get a batch of rows from the filter column
                DataBatch filterColumnBatch = _filterColumnGetter();

                // Identify rows matching this criterion
                _comparer(filterColumnBatch, _mapper);

                // Stop if we got rows, otherwise get the next source batch
                if (_mapper.Count > 0) return _mapper.Count;
            }

            return 0;
        }
    }
}

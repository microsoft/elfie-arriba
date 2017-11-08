using System;
using System.Collections.Generic;
using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Transforms
{
    public class WhereFilter : IDataBatchSource
    {
        private IDataBatchSource _source;

        private Func<DataBatch> _filterColumnGetter;
        private Action<DataBatch, RowRemapper> _comparer;
        private RowRemapper _mapper;

        public WhereFilter(IDataBatchSource source, string columnName, CompareOperator op, object value)
        {
            this._source = source;

            // Find the column we're filtering on
            int columnIndex = source.Columns.IndexOfColumn(columnName);
            ColumnDetails filterColumn = source.Columns[columnIndex];

            // Cache the ColumnGetter
            this._filterColumnGetter = source.ColumnGetter(columnIndex);

            // Build a Comparer for the desired type and get the function for the desired compare operator
            this._comparer = ComparerFactory.Build(filterColumn.Type, op, TypeConverterFactory.ConvertSingle(value, filterColumn.Type));

            // Build a mapper to hold matching rows and remap source batches
            this._mapper = new RowRemapper();
        }

        public IReadOnlyList<ColumnDetails> Columns => _source.Columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Keep a column-specific array for remapping indices
            int[] remapArray = null;

            return () =>
            {
                // Get the batch from the source for this column
                DataBatch batch = _source.ColumnGetter(columnIndex)();

                // Remap the DataBatch indices for this column for the rows which matched the clause
                return _mapper.Remap(batch, ref remapArray);
            };
        }

        public void Reset()
        {
            _source.Reset();
        }

        public int Next(int desiredCount)
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

        public void Dispose()
        {
            if(_source != null)
            {
                _source.Dispose();
                _source = null;
            }
        }
    }
}

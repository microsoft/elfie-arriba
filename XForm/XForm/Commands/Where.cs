// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;
using XForm.Transforms;
using XForm.Types;

namespace XForm.Commands
{
    internal class WhereCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "where";
        public string Usage => "'where' [columnName] [operator] [value]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new Where(source,
                context.Parser.NextColumnName(source),
                context.Parser.NextCompareOperator(),
                context.Parser.NextLiteralValue()
            );
        }
    }

    public class Where : DataBatchEnumeratorWrapper
    {
        private int _filterColumnIndex;
        private Func<DataBatch> _filterColumnGetter;
        private Action<DataBatch, RowRemapper> _comparer;
        private RowRemapper _mapper;

        public Where(IDataBatchEnumerator source, string columnName, CompareOperator op, object value) : base(source)
        {
            // Find the column we're filtering on
            _filterColumnIndex = source.Columns.IndexOfColumn(columnName);
            ColumnDetails filterColumn = _source.Columns[_filterColumnIndex];

            // Cache the ColumnGetter
            _filterColumnGetter = source.ColumnGetter(_filterColumnIndex);

            // Build a Comparer for the desired type and get the function for the desired compare operator
            object compareToValue = TypeConverterFactory.ConvertSingle(value, filterColumn.Type);

            // Null comparison is generic
            if (compareToValue == null)
            {
                if (op == CompareOperator.Equals) _comparer = WhereIsNull;
                else if (op == CompareOperator.NotEquals) _comparer = WhereIsNotNull;
                else throw new ArgumentException($"where \"{columnName}\" {op} null is invalid; only equals and not equals are supported against null.");
            }
            else
            {
                _comparer = TypeProviderFactory.Get(filterColumn.Type).TryGetComparer(op, compareToValue);
            }

            // Build a mapper to hold matching rows and remap source batches
            _mapper = new RowRemapper();
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
            while (_source.Next(desiredCount) > 0)
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

        private void WhereIsNull(DataBatch source, RowRemapper remapper)
        {
            remapper.ClearAndSize(source.Count);

            // If nothing was null in the source batch, there are no matches
            if (source.IsNull == null) return;

            // Otherwise, add rows where the value was marked null
            for (int i = 0; i < source.Count; ++i)
            {
                int index = source.Index(i);
                if (source.IsNull[index]) remapper.Add(i);
            }
        }

        private void WhereIsNotNull(DataBatch source, RowRemapper remapper)
        {
            remapper.ClearAndSize(source.Count);

            // If nothing was null in the source batch, every row matches
            if (source.IsNull == null)
            {
                for (int i = 0; i < source.Count; ++i)
                {
                    remapper.Add(i);
                }

                return;
            }

            // Otherwise, add rows where the value was marked null
            for (int i = 0; i < source.Count; ++i)
            {
                int index = source.Index(i);
                if (!source.IsNull[index]) remapper.Add(i);
            }
        }
    }
}

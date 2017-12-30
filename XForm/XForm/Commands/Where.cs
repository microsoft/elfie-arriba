// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Functions;
using XForm.Query;
using XForm.Transforms;
using XForm.Types;

namespace XForm.Commands
{
    internal class WhereCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "where";
        public string Usage => "'where' [columnFunctionOrLiteral] [operator] [columnFunctionOrLiteral]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new Where(source,
                context.Parser.NextColumn(source, context),
                context.Parser.NextCompareOperator(),
                context.Parser.NextColumn(source, context)
            );
        }
    }

    public class Where : DataBatchEnumeratorWrapper
    {
        private Func<DataBatch> _leftGetter;
        private Func<DataBatch> _rightGetter;
        private Action<DataBatch, DataBatch, RowRemapper> _comparer;
        private RowRemapper _mapper;

        public Where(IDataBatchEnumerator source, IDataBatchColumn left, CompareOperator op, IDataBatchColumn right) : base(source)
        {
            // If the left side is a constant and the operator can be swapped, move it to the right side.
            // Comparers can check if the right side is constant and run a faster loop when that's the case.
            if (left is Constant && !(right is Constant))
            {
                if (op.TryInvertCompareOperator(out op))
                {
                    Func<DataBatch> swap = _leftGetter;
                    _leftGetter = _rightGetter;
                    _rightGetter = swap;
                }
            }

            // Convert the right side to the left side type if required
            // This means constants will always be casted to the other side type.
            if (left.ColumnDetails.Type != right.ColumnDetails.Type)
            {
                right = XForm.Functions.Cast.Build(source, right, left.ColumnDetails.Type, null, true);
            }

            // Get the left and right getters
            _leftGetter = left.Getter();
            _rightGetter = right.Getter();

            // Null comparison is generic
            if ((right is Constant && ((Constant)right).IsNull) || (left is Constant && ((Constant)left).IsNull))
            {
                if (op == CompareOperator.Equal) _comparer = WhereIsNull;
                else if (op == CompareOperator.NotEqual) _comparer = WhereIsNotNull;
                else throw new ArgumentException($"Only equals and not equals operators are supported against null.");
            }
            else
            {
                // Get a comparer which can compare the values
                _comparer = TypeProviderFactory.Get(left.ColumnDetails.Type).TryGetComparer(op);
            }

            // Build a mapper to hold matching rows and remap source batches
            _mapper = new RowRemapper();
        }

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Keep a column-specific array for remapping indices
            int[] remapArray = null;

            // Retrieve the column getter for this column
            Func<DataBatch> getter = _source.ColumnGetter(columnIndex);

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
                // Get the pair of values to compare
                DataBatch left = _leftGetter();
                DataBatch right = _rightGetter();

                // Identify rows matching this criterion
                _comparer(left, right, _mapper);

                // Stop if we got rows, otherwise get the next source batch
                int count = _mapper.Count;
                if (count > 0) return count;
            }

            return 0;
        }

        private void WhereIsNull(DataBatch source, DataBatch unused, RowRemapper remapper)
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

        private void WhereIsNotNull(DataBatch source, DataBatch unused, RowRemapper remapper)
        {
            remapper.ClearAndSize(source.Count);

            // If nothing was null in the source batch, every row matches
            if (source.IsNull == null)
            {
                remapper.All(source.Count);
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

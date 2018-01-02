// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Query;
using XForm.Query.Expression;
using XForm.Transforms;

namespace XForm.Commands
{
    internal class WhereCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "where";
        public string Usage => "'where' [columnFunctionOrLiteral] [operator] [columnFunctionOrLiteral]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new Where(source, context.Parser.NextExpression(source, context));
        }
    }

    public class Where : DataBatchEnumeratorWrapper
    {
        private IExpression _expression;
        private BitVector _vector;
        private RowRemapper _mapper;

        public Where(IDataBatchEnumerator source, IExpression expression) : base(source)
        {
            _expression = expression;

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
            int outerCount;
            while ((outerCount = _source.Next(desiredCount)) > 0)
            {
                Allocator.AllocateToSize(ref _vector, outerCount);
                _vector.None();
                _expression.Evaluate(_vector);

                // Stop if we got rows, otherwise get the next source batch
                _mapper.SetMatches(_vector);
                int count = _mapper.Count;
                if (count > 0) return count;
            }

            return 0;
        }        
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
using XForm.Functions;
using XForm.Types;

namespace XForm.Query.Expression
{
    internal class TermExpression : IExpression
    {
        private IDataBatchColumn _left;
        private CompareOperator _cOp;
        private IDataBatchColumn _right;

        private Func<DataBatch> _leftGetter;
        private Func<DataBatch> _rightGetter;
        private ComparerExtensions.Comparer _comparer;

        public TermExpression(IDataBatchEnumerator source, IDataBatchColumn left, CompareOperator op, IDataBatchColumn right)
        {
            // Disallow constant <op> constant [likely error not wrapping column name]
            if (left is Constant && right is Constant) throw new ArgumentException($"({left} {op.ToQueryForm()} {right}) is comparing two constants. Wrap [ColumnNames] in braces.");

            // Save arguments as-is for ToString()
            _left = left;
            _cOp = op;
            _right = right;

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

            // Disallow unquoted constants used as strings
            if (right is Constant && left.ColumnDetails.Type == typeof(String8) && right.ColumnDetails.Type == typeof(String8))
            {
                Constant cRight = (Constant)right;
                if (!cRight.IsNull && cRight.WasUnwrappedLiteral)
                {
                    throw new ArgumentException($"{right} is compared to a string, but is unquoted. Strings must be quoted.");
                }
            }

            // Convert the right side to the left side type if required
            // This means constants will always be casted to the other side type.
            if (left.ColumnDetails.Type != right.ColumnDetails.Type)
            {
                right = XForm.Functions.Cast.Build(source, right, left.ColumnDetails.Type, ValueKinds.Invalid);
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
                if (_comparer == null) throw new ArgumentException($"No comparer found for type {left.ColumnDetails.Type.Name}.");
            }
        }

        public void Evaluate(BitVector result)
        {
            // Get the pair of values to compare
            DataBatch left = _leftGetter();
            DataBatch right = _rightGetter();

            // Identify rows matching this criterion
            _comparer(left, right, result);
        }

        private void WhereIsNull(DataBatch source, DataBatch unused, BitVector vector)
        {
            // If nothing was null in the source batch, there are no matches
            if (source.IsNull == null) return;

            // Otherwise, add rows where the value was marked null
            for (int i = 0; i < source.Count; ++i)
            {
                int index = source.Index(i);
                if (source.IsNull[index]) vector.Set(i);
            }
        }

        private void WhereIsNotNull(DataBatch source, DataBatch unused, BitVector vector)
        {
            // If nothing was null in the source batch, every row matches
            if (source.IsNull == null)
            {
                vector.All(source.Count);
                return;
            }

            // Otherwise, add rows where the value was marked null
            for (int i = 0; i < source.Count; ++i)
            {
                int index = source.Index(i);
                if (!source.IsNull[index]) vector.Set(i);
            }
        }

        public override string ToString()
        {
            return $"{_left} {_cOp.ToQueryForm()} {_right}";
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
using XForm.Functions;
using XForm.Types;
using XForm.Types.Comparers;
using XForm.Extensions;

namespace XForm.Query.Expression
{
    internal class TermExpression : IExpression
    {
        private IXColumn _left;
        private CompareOperator _cOp;
        private IXColumn _right;

        private Func<XArray> _leftGetter;
        private Func<XArray> _rightGetter;
        private ComparerExtensions.Comparer _comparer;

        public TermExpression(IXTable source, IXColumn left, CompareOperator op, IXColumn right)
        {
            // Save arguments as-is for ToString()
            _left = left;
            _cOp = op;
            _right = right;

            // Disallow constant <op> constant [likely error not wrapping column name]
            if (_left.IsConstantColumn() && _right.IsConstantColumn()) throw new ArgumentException($"({left} {op.ToQueryForm()} {right}) is comparing two constants. Wrap [ColumnNames] in braces.");

            // If the left side is a constant and the operator can be swapped, move it to the right side.
            // Comparers can check if the right side is constant and run a faster loop when that's the case.
            if (_left.IsConstantColumn() && !(_right.IsConstantColumn()))
            {
                if (op.TryInvertCompareOperator(out op))
                {
                    _left = right;
                    _right = left;
                }
            }

            // Disallow unquoted constants used as strings
            if (_right.IsConstantColumn() && _left.ColumnDetails.Type == typeof(String8) && _right.ColumnDetails.Type == typeof(String8))
            {
                Constant cRight = _right as Constant;
                if (cRight != null && !cRight.IsNull && cRight.WasUnwrappedLiteral)
                {
                    throw new ArgumentException($"{right} is compared to a string, but is unquoted. Strings must be quoted.");
                }
            }

            // Convert the right side to the left side type if required
            // This means constants will always be casted to the other side type.
            if (_left.ColumnDetails.Type != _right.ColumnDetails.Type)
            {
                _right = XForm.Functions.CastedColumn.Build(source, _right, _left.ColumnDetails.Type, ValueKinds.Invalid);
            }

            // Get the left and right getters
            _leftGetter = _left.CurrentGetter();
            _rightGetter = _right.CurrentGetter();

            // Null comparison is generic
            if (_right.IsNullConstant())
            {
                if (op == CompareOperator.Equal) _comparer = WhereIsNull;
                else if (op == CompareOperator.NotEqual) _comparer = WhereIsNotNull;
                else throw new ArgumentException($"Only equals and not equals operators are supported against null.");
            }
            else if(_left.IsNullConstant())
            {
                _left = _right;
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

            // Optimize Enum to Constant comparisons to use the underlying indices
            if (_left.IsEnumColumn() && _right.IsConstantColumn())
            {
                // Get an optimized comparer against the indices rather than values
                _comparer = SetComparer.ConvertToEnumIndexComparer(_left, _comparer, ref _right, source);

                // Get the indices on the left side
                _leftGetter = _left.IndicesCurrentGetter();

                // Use the updated value for the right side
                _rightGetter = _right.CurrentGetter();
            }
        }

        public void Evaluate(BitVector result)
        {
            // Get the pair of values to compare
            XArray left = _leftGetter();
            XArray right = _rightGetter();

            // Identify rows matching this criterion
            _comparer(left, right, result);
        }

        private void WhereIsNull(XArray source, XArray unused, BitVector vector)
        {
            // If nothing was null in the source xarray, there are no matches
            if (source.IsNull == null) return;

            // Otherwise, add rows where the value was marked null
            for (int i = 0; i < source.Count; ++i)
            {
                int index = source.Index(i);
                if (source.IsNull[index]) vector.Set(i);
            }
        }

        private void WhereIsNotNull(XArray source, XArray unused, BitVector vector)
        {
            // If nothing was null in the source xarray, every row matches
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

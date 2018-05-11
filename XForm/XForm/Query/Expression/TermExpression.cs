// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Columns;
using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Types;
using XForm.Types.Comparers;

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
        private Action<BitVector> _evaluate;

        public TermExpression(IXTable source, IXColumn left, CompareOperator op, IXColumn right)
        {
            _evaluate = EvaluateNormal;

            // Save arguments as-is for ToString()
            _left = left;
            _cOp = op;
            _right = right;

            // Disallow constant <op> constant [likely error not wrapping column name]
            if (_left is ConstantColumn && _right is ConstantColumn) throw new ArgumentException($"({left} {op.ToQueryForm()} {right}) is comparing two constants. Wrap [ColumnNames] in braces.");

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
                ConstantColumn cRight = _right as ConstantColumn;
                if (cRight != null && !cRight.IsNull && cRight.WasUnwrappedLiteral)
                {
                    throw new ArgumentException($"{right} is compared to a string, but is unquoted. Strings must be quoted.");
                }
            }

            // Convert the right side to the left side type if required
            // This means constants will always be casted to the other side type.
            if (_left.ColumnDetails.Type != _right.ColumnDetails.Type)
            {
                _right = CastedColumn.Build(source, _right, _left.ColumnDetails.Type, ValueKinds.Invalid);
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
            else if (_left.IsNullConstant())
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
                IXColumn replacedRight = _right;
                _comparer = SetComparer.ConvertToEnumIndexComparer(_left, _comparer, ref replacedRight, source);

                // Get the indices on the left side
                _leftGetter = _left.IndicesCurrentGetter();

                // Use the updated value for the right side
                _rightGetter = replacedRight.CurrentGetter();
            }

            // Allow String8 to constant Contains queries to compare on the raw byte[] and int[]
            if (op == CompareOperator.Contains && _right.IsConstantColumn() && _left.ColumnDetails.Type == typeof(String8) && !_left.IsEnumColumn())
            {
                Func<object> rawGetter = _left.ComponentGetter(ColumnComponent.String8Raw);

                if (rawGetter != null)
                {
                    String8 rightValue = (String8)_right.ValuesGetter()().Array.GetValue(0);
                    String8Comparer string8Comparer = new String8Comparer();

                    _evaluate = (vector) =>
                    {
                        String8Raw raw = (String8Raw)rawGetter();
                        string8Comparer.WhereContains(raw, rightValue, vector);
                    };
                }
            }
        }

        public void Evaluate(BitVector result)
        {
            _evaluate(result);
        }

        private void EvaluateNormal(BitVector result)
        {
            // Get the pair of values to compare
            XArray left = _leftGetter();
            XArray right = _rightGetter();

            // Identify rows matching this criterion
            _comparer(left, right, result);
        }

        private void WhereIsNull(XArray source, XArray unused, BitVector vector)
        {
            BoolComparer.WhereNull(source, true, vector);
        }

        private void WhereIsNotNull(XArray source, XArray unused, BitVector vector)
        {
            BoolComparer.WhereNull(source, false, vector);
        }

        public override string ToString()
        {
            return $"{_left} {_cOp.ToQueryForm()} {_right}";
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Columns;
using XForm.Data;
using XForm.Query;

namespace XForm.Types.Comparers
{
    /// <summary>
    ///  SetComparer is used to optimize comparisons involving EnumColumns.
    ///  It converts operations on the values (where [ClientBrowser]: "Chrome") to operations on the indices ([ClientBrowser.Indices] IN (1, 2, 3).
    ///  This converts comparisons to byte compares, and frequently allows using native accelerated comparers or hardcoded 'All' or 'None'.
    /// </summary>
    internal class SetComparer
    {
        private BitVector _set;
        private bool[] _array;

        private SetComparer(BitVector set)
        {
            _set = set;

            _array = null;
            _set.ToArray(ref _array);
        }

        /// <summary>
        ///  ConvertToEnumIndexComparer takes a comparer on the values of the enum and translates it to a comparer
        ///  which can operate on the byte[] indices instead.
        /// </summary>
        /// <param name="leftColumn">EnumColumn being compared</param>
        /// <param name="currentComparer">Current Comparison function requested by TermExpression</param>
        /// <param name="rightColumn">Constant being compared against</param>
        /// <param name="source">IXTable containing comparison</param>
        /// <returns>Comparer to compare the (updated) right Constant to the EnumColumn.Indices (rather than Values)</returns>
        public static ComparerExtensions.Comparer ConvertToEnumIndexComparer(IXColumn leftColumn, ComparerExtensions.Comparer currentComparer, ref IXColumn rightColumn, IXTable source)
        {
            Func<XArray> valuesGetter = leftColumn.ValuesGetter();
            if (valuesGetter == null) throw new ArgumentException("ConvertToEnumIndexComparer is only valid for columns implementing Values.");

            // Get all distinct values from the left side
            XArray left = valuesGetter();

            // If there are no values, return none
            if (left.Count == 0) return None;

            // Get right side and compare
            XArray right = rightColumn.ValuesGetter()();
            BitVector set = new BitVector(left.Count);
            currentComparer(left, right, set);

            // NOTE: When EnumColumn values are sorted, can convert comparisons to non-equality native accelerated compare.

            if (set.Count == 0)
            {
                // If there were no matches, always return none
                return None;
            }
            else if (set.Count == left.Count)
            {
                // If everything matched, always return everything
                return All;
            }
            else if (set.Count == 1)
            {
                // Convert the constant to the one matching index and make the comparison for index equals that
                rightColumn = new ConstantColumn(source, (byte)set.GetSingle(), typeof(byte));

                return TypeProviderFactory.Get(typeof(byte)).TryGetComparer(CompareOperator.Equal);
            }
            else if (set.Count == left.Count - 1)
            {
                set.Not();

                // Convert the constant to the one non-matching index and make the comparison for index doesn't equal that
                rightColumn = new ConstantColumn(source, (byte)set.GetSingle(), typeof(byte));

                return TypeProviderFactory.Get(typeof(byte)).TryGetComparer(CompareOperator.NotEqual);
            }
            else
            {
                // Otherwise, build a matcher for values in the set
                return new SetComparer(set).Evaluate;
            }
        }

        public static void All(XArray left, XArray unused, BitVector vector)
        {
            vector.All(left.Count);
        }

        public static void None(XArray left, XArray unused, BitVector vector)
        { }

        public void Evaluate(XArray left, XArray unused, BitVector vector)
        {
            byte[] leftArray = (byte[])left.Array;

            // Check how the arrays are configured and run the fastest loop possible for the configuration.
            if (left.HasNulls)
            {
                // Slowest Path: Null checks and look up indices on both sides
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    if (left.NullRows[leftIndex]) continue;
                    if (_array[leftArray[leftIndex]]) vector.Set(i);
                }
            }
            else if (left.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides.
                for (int i = 0; i < left.Count; ++i)
                {
                    if (_array[leftArray[left.Index(i)]]) vector.Set(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant.
                int zeroOffset = left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (_array[leftArray[i]]) vector.Set(i - zeroOffset);
                }
            }
            else
            {
                // Single Static comparison.
                if (_array[leftArray[left.Selector.StartIndexInclusive]])
                {
                    vector.All(left.Count);
                }
            }
        }
    }
}

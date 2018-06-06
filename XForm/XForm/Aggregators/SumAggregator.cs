// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Columns;
using XForm.Data;

namespace XForm.Aggregators
{
    internal class SumBuilder : IAggregatorBuilder
    {
        public string Name => "Sum";
        public string Usage => "Sum({Col|Func|Const})";

        public IAggregator Build(IXTable source, XDatabaseContext context)
        {
            IXColumn column = CastedColumn.Build(source, context.Parser.NextColumn(source, context), typeof(long));
            return new SumAggregator(column);
        }
    }

    public class SumAggregator : IAggregator
    {
        private IXColumn _sumColumn;
        private Func<XArray> _sumCurrentGetter;

        private bool[] _isNullPerBucket;
        private long[] _sumPerBucket;
        private int _distinctCount;

        public ColumnDetails ColumnDetails { get; private set; }
        public XArray Values => (_sumPerBucket == null ? XArray.Empty : XArray.All(_sumPerBucket, _distinctCount, _isNullPerBucket));

        public SumAggregator(IXColumn sumOverColumn)
        {
            _sumColumn = sumOverColumn;
            _sumCurrentGetter = sumOverColumn.CurrentGetter();

            ColumnDetails = new ColumnDetails($"{sumOverColumn.ColumnDetails.Name}.Sum", typeof(long));
        }

        public void Add(XArray rowIndices, int newDistinctCount)
        {
            _distinctCount = newDistinctCount;
            Allocator.ExpandToSize(ref _sumPerBucket, newDistinctCount);

            XArray sumValues = _sumCurrentGetter();

            if (rowIndices.Array is int[])
            {
                AddInt(rowIndices, sumValues, newDistinctCount);
            }
            else if (rowIndices.Array is byte[])
            {
                AddByte(rowIndices, sumValues, newDistinctCount);
            }
            else
            {
                AddUShort(rowIndices, sumValues, newDistinctCount);
            }
        }

        private void AddInt(XArray rowIndices, XArray sumValues, int newDistinctCount)
        {
            long[] sumArray = (long[])sumValues.Array;
            int[] indicesArray = (int[])rowIndices.Array;

            if (sumValues.HasNulls)
            {
                // Nulls: Find nulls and mark those buckets null
                Allocator.AllocateToSize(ref _isNullPerBucket, newDistinctCount);
                for (int i = 0; i < rowIndices.Count; ++i)
                {
                    int bucketIndex = rowIndices.Index(i);
                    int sumIndex = sumValues.Index(i);
                    if (sumValues.NullRows[sumIndex]) _isNullPerBucket[indicesArray[bucketIndex]] = true;

                    _sumPerBucket[indicesArray[bucketIndex]] += sumArray[sumIndex];
                }
            }
            else if (rowIndices.Selector.Indices != null || sumValues.Selector.Indices != null)
            {
                // Indices: Look up raw indices on both sides
                for (int i = 0; i < rowIndices.Count; ++i)
                {
                    _sumPerBucket[indicesArray[rowIndices.Index(i)]] += sumArray[sumValues.Index(i)];
                }
            }
            else if (sumValues.Selector.IsSingleValue == false)
            {
                // Non-Indexed: Loop over arrays directly
                int indicesOffset = rowIndices.Selector.StartIndexInclusive;
                int sumOffset = sumValues.Selector.StartIndexInclusive;

                for (int i = 0; i < rowIndices.Count; ++i)
                {
                    _sumPerBucket[indicesArray[i + indicesOffset]] += sumArray[i + sumOffset];
                }
            }
            else if (rowIndices.Selector.IsSingleValue == false)
            {
                // Single Sum Value: Add constant to each bucket
                long sumValue = sumArray[sumValues.Index(0)];
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _sumPerBucket[indicesArray[i]] += sumValue;
                }
            }
            else
            {
                // Single Bucket, Single Sum: Add (Value * RowCount) to target bucket
                _sumPerBucket[indicesArray[rowIndices.Index(0)]] += sumValues.Count * sumArray[sumValues.Index(0)];
            }
        }

        private void AddByte(XArray rowIndices, XArray sumValues, int newDistinctCount)
        {
            long[] sumArray = (long[])sumValues.Array;
            byte[] indicesArray = (byte[])rowIndices.Array;

            if (sumValues.HasNulls)
            {
                // Nulls: Find nulls and mark those buckets null
                Allocator.AllocateToSize(ref _isNullPerBucket, newDistinctCount);
                for (int i = 0; i < rowIndices.Count; ++i)
                {
                    int bucketIndex = rowIndices.Index(i);
                    int sumIndex = sumValues.Index(i);
                    if (sumValues.NullRows[sumIndex]) _isNullPerBucket[indicesArray[bucketIndex]] = true;

                    _sumPerBucket[indicesArray[bucketIndex]] += sumArray[sumIndex];
                }
            }
            else if (rowIndices.Selector.Indices != null || sumValues.Selector.Indices != null)
            {
                // Indices: Look up raw indices on both sides
                for (int i = 0; i < rowIndices.Count; ++i)
                {
                    _sumPerBucket[indicesArray[rowIndices.Index(i)]] += sumArray[sumValues.Index(i)];
                }
            }
            else if (sumValues.Selector.IsSingleValue == false)
            {
                // Non-Indexed: Loop over arrays directly
                int indicesOffset = rowIndices.Selector.StartIndexInclusive;
                int sumOffset = sumValues.Selector.StartIndexInclusive;

                for (int i = 0; i < rowIndices.Count; ++i)
                {
                    _sumPerBucket[indicesArray[i + indicesOffset]] += sumArray[i + sumOffset];
                }
            }
            else if (rowIndices.Selector.IsSingleValue == false)
            {
                // Single Sum Value: Add constant to each bucket
                long sumValue = sumArray[sumValues.Index(0)];
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _sumPerBucket[indicesArray[i]] += sumValue;
                }
            }
            else
            {
                // Single Bucket, Single Sum: Add (Value * RowCount) to target bucket
                _sumPerBucket[indicesArray[rowIndices.Index(0)]] += sumValues.Count * sumArray[sumValues.Index(0)];
            }
        }

        private void AddUShort(XArray rowIndices, XArray sumValues, int newDistinctCount)
        {
            long[] sumArray = (long[])sumValues.Array;
            ushort[] indicesArray = (ushort[])rowIndices.Array;

            if (sumValues.HasNulls)
            {
                // Nulls: Find nulls and mark those buckets null
                Allocator.AllocateToSize(ref _isNullPerBucket, newDistinctCount);
                for (int i = 0; i < rowIndices.Count; ++i)
                {
                    int bucketIndex = rowIndices.Index(i);
                    int sumIndex = sumValues.Index(i);
                    if (sumValues.NullRows[sumIndex]) _isNullPerBucket[indicesArray[bucketIndex]] = true;

                    _sumPerBucket[indicesArray[bucketIndex]] += sumArray[sumIndex];
                }
            }
            else if (rowIndices.Selector.Indices != null || sumValues.Selector.Indices != null)
            {
                // Indices: Look up raw indices on both sides
                for (int i = 0; i < rowIndices.Count; ++i)
                {
                    _sumPerBucket[indicesArray[rowIndices.Index(i)]] += sumArray[sumValues.Index(i)];
                }
            }
            else if (sumValues.Selector.IsSingleValue == false)
            {
                // Non-Indexed: Loop over arrays directly
                int indicesOffset = rowIndices.Selector.StartIndexInclusive;
                int sumOffset = sumValues.Selector.StartIndexInclusive;

                for (int i = 0; i < rowIndices.Count; ++i)
                {
                    _sumPerBucket[indicesArray[i + indicesOffset]] += sumArray[i + sumOffset];
                }
            }
            else if (rowIndices.Selector.IsSingleValue == false)
            {
                // Single Sum Value: Add constant to each bucket
                long sumValue = sumArray[sumValues.Index(0)];
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _sumPerBucket[indicesArray[i]] += sumValue;
                }
            }
            else
            {
                // Single Bucket, Single Sum: Add (Value * RowCount) to target bucket
                _sumPerBucket[indicesArray[rowIndices.Index(0)]] += sumValues.Count * sumArray[sumValues.Index(0)];
            }
        }
    }
}

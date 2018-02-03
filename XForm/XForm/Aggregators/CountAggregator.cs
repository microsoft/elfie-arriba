// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using XForm.Data;

namespace XForm.Aggregators
{
    internal class CountBuilder : IAggregatorBuilder
    {
        public string Name => "Count";
        public string Usage => "Count()";

        public IAggregator Build(IXTable source, XDatabaseContext context)
        {
            return new CountAggregator();
        }
    }

    public class CountAggregator : IAggregator
    {
        private int[] _countPerBucket;
        private int _distinctCount;

        public ColumnDetails ColumnDetails { get; private set; }
        public XArray Values => XArray.All(_countPerBucket, _distinctCount);

        public CountAggregator()
        {
            ColumnDetails = new ColumnDetails("Count", typeof(int));
        }

        public void Add(XArray rowIndices, int newDistinctCount)
        {
            _distinctCount = newDistinctCount;
            Allocator.ExpandToSize(ref _countPerBucket, newDistinctCount);

            if (rowIndices.Array is int[])
            {
                AddInt(rowIndices, newDistinctCount);
            }
            else if (rowIndices.Array is byte[])
            {
                AddByte(rowIndices, newDistinctCount);
            }
            else
            {
                AddUShort(rowIndices, newDistinctCount);
            }
        }

        private void AddInt(XArray rowIndices, int newDistinctCount)
        {
            int[] array = (int[])rowIndices.Array;
            if (rowIndices.Selector.Indices != null)
            {
                // Indexed XArray - look up indexed values
                int[] indices = rowIndices.Selector.Indices;
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _countPerBucket[array[indices[i]]]++;
                }
            }
            else if (rowIndices.Selector.IsSingleValue == false)
            {
                // Non-Indexed XArray - loop from Start to End
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _countPerBucket[array[i]]++;
                }
            }
            else
            {
                // All rows are one value - add to that count
                _countPerBucket[array[rowIndices.Index(0)]] += rowIndices.Count;
            }
        }

        private void AddByte(XArray rowIndices, int newDistinctCount)
        {
            byte[] array = (byte[])rowIndices.Array;
            if (rowIndices.Selector.Indices != null)
            {
                // Indexed XArray - look up indexed values
                int[] indices = rowIndices.Selector.Indices;
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _countPerBucket[array[indices[i]]]++;
                }
            }
            else if (rowIndices.Selector.IsSingleValue == false)
            {
                // Non-Indexed XArray - loop from Start to End
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _countPerBucket[array[i]]++;
                }
            }
            else
            {
                // All rows are one value - add to that count
                _countPerBucket[array[rowIndices.Index(0)]] += rowIndices.Count;
            }
        }

        private void AddUShort(XArray rowIndices, int newDistinctCount)
        {
            ushort[] array = (ushort[])rowIndices.Array;
            if (rowIndices.Selector.Indices != null)
            {
                // Indexed XArray - look up indexed values
                int[] indices = rowIndices.Selector.Indices;
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _countPerBucket[array[indices[i]]]++;
                }
            }
            else if (rowIndices.Selector.IsSingleValue == false)
            {
                // Non-Indexed XArray - loop from Start to End
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _countPerBucket[array[i]]++;
                }
            }
            else
            {
                // All rows are one value - add to that count
                _countPerBucket[array[rowIndices.Index(0)]] += rowIndices.Count;
            }
        }
    }
}

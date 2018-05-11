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

    public class CountAggregator : IAggregator, IFoundIndicesTracker
    {
        private int[] _countPerBucket;
        private int _distinctCount;

        public CountAggregator()
        {
            ColumnDetails = new ColumnDetails("Count", typeof(int));
        }

        public ColumnDetails ColumnDetails { get; private set; }
        public XArray Values => (_countPerBucket == null ? XArray.Empty : XArray.All(_countPerBucket, _distinctCount));
        public int TotalRowCount { get; private set; }

        public ArraySelector FoundIndices
        {
            get
            {
                if (_countPerBucket == null) return ArraySelector.All(0);

                // Count each bucket which had more than zero rows and keep the in-order indices of them
                int distinctCountFound = 0;
                int[] foundIndexSelector = new int[_countPerBucket.Length];
                for (int i = 0; i < _countPerBucket.Length; ++i)
                {
                    if (_countPerBucket[i] > 0)
                    {
                        foundIndexSelector[distinctCountFound] = i;
                        distinctCountFound++;
                    }
                }

                // Build a selector to filter the keys and aggregates to the non-zero row set (or keep all of them, if all were non-zero)
                return (distinctCountFound == _distinctCount ? ArraySelector.All(distinctCountFound) : ArraySelector.Map(foundIndexSelector, distinctCountFound));
            }
        }

        public void Add(XArray rowIndices, int newDistinctCount)
        {
            TotalRowCount += rowIndices.Count;

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

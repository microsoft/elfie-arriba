using System;
using XForm.Data;

namespace XForm.Aggregators
{
    internal class SumBuilder : IAggregatorBuilder
    {
        public string Name => "Sum";
        public string Usage => "Sum({Col|Func|Const})";

        public IAggregator Build(IXTable source, XDatabaseContext context)
        {
            return new Sum(context.Parser.NextColumn(source, context, typeof(int)));
        }
    }

    public class Sum : IAggregator
    {
        private IXColumn _sumColumn;
        private Func<XArray> _sumCurrentGetter;

        private long[] _sumPerBucket;
        private int _distinctCount;

        public ColumnDetails ColumnDetails { get; private set; }
        public XArray Values => XArray.All(_sumPerBucket, _distinctCount);

        public Sum(IXColumn sumOverColumn)
        {
            _sumColumn = sumOverColumn;
            _sumCurrentGetter = sumOverColumn.CurrentGetter();

            ColumnDetails = new ColumnDetails($"{sumOverColumn.ColumnDetails.Name}.Sum", typeof(long));
        }

        public void Add(XArray rowIndices, int newDistinctCount)
        {
            _distinctCount = newDistinctCount;
            Allocator.ExpandToSize(ref _sumPerBucket, newDistinctCount);

            XArray valuesToSum = _sumCurrentGetter();

            int[] sumArray = (int[])valuesToSum.Array;

            if (rowIndices.Array is int[])
            {
                int[] array = (int[])rowIndices.Array;
                int offset = rowIndices.Selector.StartIndexInclusive;
                for (int i = 0; i < rowIndices.Count; ++i)
                {
                    _sumPerBucket[array[i + offset]] += sumArray[valuesToSum.Index(i)];
                }
            }
            else if(rowIndices.Array is byte[])
            {
                byte[] array = (byte[])rowIndices.Array;
                int offset = rowIndices.Selector.StartIndexInclusive;
                for (int i = 0; i < rowIndices.Count; ++i)
                {
                    _sumPerBucket[array[i + offset]] += sumArray[valuesToSum.Index(i)];
                }
            }
            else
            {
                ushort[] array = (ushort[])rowIndices.Array;
                int offset = rowIndices.Selector.StartIndexInclusive;
                for (int i = 0; i < rowIndices.Count; ++i)
                {
                    _sumPerBucket[array[i + offset]] += sumArray[valuesToSum.Index(i)];
                }
            }
        }
    }
}

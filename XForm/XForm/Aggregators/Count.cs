using XForm.Data;

namespace XForm.Aggregators
{
    internal class CountBuilder : IAggregatorBuilder
    {
        public string Name => "Count";
        public string Usage => "Count()";

        public IAggregator Build(IXTable source, XDatabaseContext context)
        {
            return new Count();
        }
    }

    public class Count : IAggregator
    {
        private int[] _countPerBucket;
        private int _distinctCount;

        public ColumnDetails ColumnDetails { get; private set; }
        public XArray Values => XArray.All(_countPerBucket, _distinctCount);

        public Count()
        {
            ColumnDetails = new ColumnDetails("Count", typeof(int));
        }

        public void Add(XArray rowIndices, int newDistinctCount)
        {
            _distinctCount = newDistinctCount;
            Allocator.ExpandToSize(ref _countPerBucket, newDistinctCount);

            if (rowIndices.Array is int[])
            {
                int[] array = (int[])rowIndices.Array;
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _countPerBucket[array[i]]++;
                }
            }
            else if (rowIndices.Array is byte[])
            {
                byte[] array = (byte[])rowIndices.Array;
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _countPerBucket[array[i]]++;
                }
            }
            else
            {
                ushort[] array = (ushort[])rowIndices.Array;
                for (int i = rowIndices.Selector.StartIndexInclusive; i < rowIndices.Selector.EndIndexExclusive; ++i)
                {
                    _countPerBucket[array[i]]++;
                }
            }
        }
    }
}

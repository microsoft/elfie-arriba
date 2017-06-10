using System;
using System.Threading.Tasks;
using V5.Extensions;

namespace V5.Data
{
    public class SortBucketColumn<T> where T : IComparable<T>
    {
        public T[] BucketMinimumValue;
        public bool[] IsMultiValueBucket;
        public int[] BucketRowCount;

        public byte[] BucketIndexPerRow;

        public void Build(T[] values, byte bucketCount, Random r)
        {
            // Sample 10x bucket count values
            T[] sample = values.Sample(10 * bucketCount, r);
            Array.Sort(sample);

            // Try to get n+1 bucket boundary values
            this.BucketMinimumValue = new T[bucketCount + 1];
            this.BucketMinimumValue[0] = sample[0];
            this.BucketMinimumValue[bucketCount] = sample[sample.Length - 1];

            int bucketsFilled = 1;
            int nextSample = 0;
            while (true)
            {
                int incrementCount = ((sample.Length - nextSample) / (bucketCount - bucketsFilled)) + 1;
                nextSample += incrementCount;

                if (nextSample >= sample.Length) break;

                T value = sample[nextSample];
                if (!value.Equals(this.BucketMinimumValue[bucketsFilled - 1]))
                {
                    this.BucketMinimumValue[bucketsFilled] = value;
                    bucketsFilled++;

                    if (bucketsFilled == bucketCount) break;
                }
            }

            // Capture the set actually filled
            if (bucketsFilled < bucketCount)
            {
                T[] actualBuckets = new T[bucketsFilled];
                Array.Copy(this.BucketMinimumValue, actualBuckets, bucketsFilled);
                this.BucketMinimumValue = actualBuckets;
            }

            this.IsMultiValueBucket = new bool[this.BucketMinimumValue.Length];
            this.BucketRowCount = new int[this.BucketMinimumValue.Length];
            this.BucketIndexPerRow = new byte[values.Length];

            int parallelCount = 4;
            int parallelPageSize = values.Length / parallelCount;

            if (parallelCount <= 1)
            {
                T min = this.BucketMinimumValue[0];
                T max = this.BucketMinimumValue[this.BucketMinimumValue.Length - 1];

                BucketRows(values, 0, values.Length, this.BucketRowCount, ref min, ref max);

                this.BucketMinimumValue[0] = min;
                this.BucketMinimumValue[this.BucketMinimumValue.Length - 1] = max;
            }
            else
            {
                Parallel.For(0, parallelCount, (page) =>
                {
                    int index = parallelPageSize * page;
                    int length = parallelPageSize;
                    if (page == parallelCount - 1 && (values.Length & 1) == 1) length++;

                    T min = this.BucketMinimumValue[0];
                    T max = this.BucketMinimumValue[this.BucketMinimumValue.Length - 1];

                    int[] bucketRowCount = new int[this.BucketMinimumValue.Length];
                    BucketRows(values, index, length, bucketRowCount, ref min, ref max);

                    lock (this)
                    {
                        if (min.CompareTo(this.BucketMinimumValue[0]) < 0) this.BucketMinimumValue[0] = min;
                        if (max.CompareTo(this.BucketMinimumValue[this.BucketMinimumValue.Length - 1]) > 0) this.BucketMinimumValue[this.BucketMinimumValue.Length - 1] = max;

                        for (int i = 0; i < this.BucketMinimumValue.Length; ++i)
                        {
                            this.BucketRowCount[i] += bucketRowCount[i];
                        }
                    }
                });
            }
        }

        public void BucketRows(T[] values, int index, int length, int[] bucketRowCount, ref T min, ref T max)
        {
            int end = index + length;
            for (int i = index; i < end; ++i)
            {
                T value = values[i];

                bool isExact;
                int bucketIndex = BucketForValue(value, out isExact);

                if (!isExact)
                {
                    if (bucketIndex < 0)
                    {
                        if (min.CompareTo(value) < 0) min = value;
                        bucketIndex = 0;
                    }
                    else if (bucketIndex >= this.BucketMinimumValue.Length - 1)
                    {
                        if (max.CompareTo(value) > 0) max = value;
                        bucketIndex--;
                    }

                    this.IsMultiValueBucket[bucketIndex] = true;
                }

                this.BucketIndexPerRow[i] = (byte)bucketIndex;
                bucketRowCount[bucketIndex]++;
            }
        }

        public int BucketForValue(T value, out bool isExact)
        {
            int bucketIndex = Array.BinarySearch(this.BucketMinimumValue, value);
            isExact = bucketIndex >= 0;

            if (bucketIndex < 0) bucketIndex = ~bucketIndex - 1;

            return bucketIndex;
        }
    }
}

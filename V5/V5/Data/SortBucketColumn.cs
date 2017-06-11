using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using V5.Extensions;

namespace V5.Data
{
    public class SortBucketColumn<T> where T : IComparable<T>
    {
        public T[] Minimum;
        public bool[] IsMultiValue;
        public int[] RowCount;

        public byte[] RowBucketIndex;

        internal SortBucketColumn(T[] minimums, byte[] rowBucketIndex)
        {
            this.Minimum = minimums;
            this.RowBucketIndex = rowBucketIndex;

            this.IsMultiValue = new bool[minimums.Length];
            this.RowCount = new int[minimums.Length];
        }

        public T Min
        {
            get => this.Minimum[0];
            set => this.Minimum[0] = value;
        }

        public T Max
        {
            get => this.Minimum[this.Minimum.Length - 1];
            set => this.Minimum[this.Minimum.Length - 1] = value;
        }

        public int Total
        {
            get => this.RowCount.Sum();
        }

        internal void Merge(SortBucketColumn<T> other)
        {
            // Merge min and max
            if (other.Min.CompareTo(this.Min) < 0) this.Min = other.Min;
            if (other.Max.CompareTo(this.Max) > 0) this.Max = other.Max;

            // Merge IsMultiValue, RowCount
            for (int i = 0; i < this.Minimum.Length; ++i)
            {
                this.IsMultiValue[i] |= other.IsMultiValue[i];
                this.RowCount[i] += other.RowCount[i];
            }
        }

        public static T[] ChooseBuckets(T[] values, int bucketCount, Random r)
        {
            // Get 10 a sample ten times the desired bucket count, sorted
            T[] sample = values.Sample(10 * bucketCount, r);
            Array.Sort(sample);

            // Try to get n+1 bucket boundary values
            T[] buckets = new T[bucketCount + 1];
            buckets[0] = sample[0];
            buckets[bucketCount] = sample[sample.Length - 1];

            int bucketsFilled = 1;
            int nextSample = 0;
            while (true)
            {
                int incrementCount = ((sample.Length - nextSample) / (bucketCount - bucketsFilled)) + 1;
                nextSample += incrementCount;

                if (nextSample >= sample.Length) break;

                T value = sample[nextSample];
                if (!value.Equals(buckets[bucketsFilled - 1]))
                {
                    buckets[bucketsFilled] = value;
                    bucketsFilled++;

                    if (bucketsFilled == bucketCount) break;
                }
            }

            // Capture the set actually filled
            if (bucketsFilled < bucketCount)
            {
                T[] actualBuckets = new T[bucketsFilled];
                Array.Copy(buckets, actualBuckets, bucketsFilled);
                buckets = actualBuckets;
            }

            return buckets;
        }

        public static SortBucketColumn<T> Build(T[] values, byte bucketCount, Random r, int parallelCount = 4)
        {
            // Choose bucket ranges [serially]
            T[] buckets = ChooseBuckets(values, bucketCount, r);

            // Build a single array for the bucket per row
            byte[] rowBuckets = new byte[values.Length];

            SortBucketColumn<T> result = new SortBucketColumn<T>(buckets, rowBuckets);

            if (parallelCount <= 1)
            {
                // If non-parallel, bucket every row
                result.BucketRows(values, 0, values.Length);
            }
            else
            {
                int parallelPageSize = values.Length / parallelCount;

                Parallel.For(0, parallelCount, (page) =>
                {
                    // Pick a distinct range of rows
                    int index = parallelPageSize * page;
                    int length = parallelPageSize;
                    if (page == parallelCount - 1 && (values.Length & 1) == 1) length++;

                    // Copy the bucket minimums (to avoid collisions when writing Min and Max)
                    T[] bucketCopy = new T[buckets.Length];
                    Array.Copy(buckets, bucketCopy, buckets.Length);

                    // Build an inner SBC and bucket a slice of rows
                    // NOTE: RowBuckets is shared because each thread writes a distinct range
                    SortBucketColumn<T> slice = new SortBucketColumn<T>(bucketCopy, rowBuckets);
                    slice.BucketRows(values, index, length);

                    // Merge the result into our main set
                    lock (result)
                    {
                        result.Merge(slice);
                    }
                });
            }

            return result;
        }

        private void BucketRows(T[] values, int index, int length)
        {
            int end = index + length;


            if (typeof(T) == typeof(long))
            {
                // TODO: Need to set isMultiValue
                SortBucketColumnN.Bucket((long[])(Array)values, index, length, (long[])(Array)this.Minimum, this.RowBucketIndex, this.RowCount);

                // Verify consistency
                //int errorCount = 0;
                //for (int i = index; i < end; ++i)
                //{
                //    int bucketChosen = this.RowBucketIndex[i];

                //    bool isExact;
                //    int bucketManaged = BucketForValue(values[i], out isExact);
                //    if (bucketManaged < 0) bucketManaged = 0;

                //    if (bucketChosen != bucketManaged)
                //    {
                //        errorCount++;
                //    }
                //}

                return;
            }

            for (int i = index; i < end; ++i)
            {
                T value = values[i];

                bool isExact;
                int bucketIndex = BucketForValue(value, out isExact);

                if (!isExact)
                {
                    if (bucketIndex < 0)
                    {
                        if (this.Min.CompareTo(value) < 0) this.Min = value;
                        bucketIndex = 0;
                    }
                    else if (bucketIndex >= this.Minimum.Length - 1)
                    {
                        if (this.Max.CompareTo(value) > 0) this.Max = value;
                        bucketIndex--;
                    }

                    this.IsMultiValue[bucketIndex] = true;
                }

                this.RowBucketIndex[i] = (byte)bucketIndex;
                this.RowCount[bucketIndex]++;
            }
        }

        public int BucketForValue(T value, out bool isExact)
        {
            int bucketIndex = Array.BinarySearch(this.Minimum, value);
            isExact = bucketIndex >= 0;

            if (bucketIndex < 0) bucketIndex = ~bucketIndex - 1;

            return bucketIndex;
        }


        public static T[] EytzingerSort(T[] array)
        {
            int outCount = 0;
            T[] output = new T[array.Length];

            Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();
            queue.Enqueue(Tuple.Create(0, array.Length - 1));

            while(queue.Count > 0)
            {
                Tuple<int, int> value = queue.Dequeue();
                int index = (value.Item1 + value.Item2) / 2;
                output[outCount++] = array[index];

                if (value.Item1 < index)
                {
                    queue.Enqueue(Tuple.Create(value.Item1, index - 1));
                }

                if(value.Item2 > index)
                { 
                    queue.Enqueue(Tuple.Create(index + 1, value.Item2));
                }
            }

            return output;
        }
    }
}

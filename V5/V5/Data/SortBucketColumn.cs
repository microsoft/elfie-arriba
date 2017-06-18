using System;
using System.IO;
using System.Threading.Tasks;
using V5.Extensions;
using V5.Serialization;

namespace V5.Data
{
    public class SortBucketColumn<T> where T : IComparable<T>
    {
        public string Name { get; private set; }

        // SortBucketColumn splits items into N buckets and each of these arrays has N+1 values.
        //  - Minimum[Length - 1] is the maximum value seen in any row.
        //  - IsMultiValue[Length - 1] is undefined.
        //  - RowCount[Length - 1] is the total row count.
        public T[] Minimum;
        public bool[] IsMultiValue;
        public int[] RowCount;

        public byte[] RowBucketIndex;

        private SortBucketColumn()
        { }

        internal SortBucketColumn(string name, T[] minimums)
        {
            this.Name = name;
            this.Minimum = minimums;
            this.IsMultiValue = new bool[minimums.Length - 1];
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
            get => this.RowCount[this.RowCount.Length - 1];
        }

        public static SortBucketColumn<T> Build(string name, T[] values, int bucketCount, Random r, int parallelCount = 1)
        {
            // Choose bucket ranges [serially]
            T[] buckets = ChooseBuckets(values, bucketCount, r);

            // Build a single array for the bucket per row
            byte[] rowBuckets = new byte[values.Length];

            SortBucketColumn<T> result = new SortBucketColumn<T>(name, buckets);
            result.RowBucketIndex = rowBuckets;

            if (parallelCount <= 1)
            {
                // If non-parallel, bucket every row
                result.BucketRows(values, 0, values.Length, rowBuckets);
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
                    SortBucketColumn<T> slice = new SortBucketColumn<T>(name, bucketCopy);
                    slice.BucketRows(values, index, length, rowBuckets);

                    // Merge the result into our main set
                    lock (result)
                    {
                        result.Merge(slice);
                    }
                });
            }

            return result;
        }

        public static T[] ChooseBuckets(T[] values, int bucketCount, Random r)
        {
            // Get 10 a sample ten times the desired bucket count, sorted
            T[] sample = values.Sample(10 * bucketCount, r);
            Array.Sort(sample);

            // Allocate bucketCount + 1 buckets
            T[] buckets = new T[bucketCount + 1];

            // Put the minimum value in the first bucket
            buckets[0] = sample[0];

            // Try to find bucketCount distinct values
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

                    if (bucketsFilled > bucketCount) break;
                }
            }

            // Set bucketCount+1 to the maximum value seen
            buckets[bucketsFilled] = sample[sample.Length - 1];
            bucketsFilled++;

            // Capture the set actually filled
            if (bucketsFilled < bucketCount)
            {
                T[] actualBuckets = new T[bucketsFilled];
                Array.Copy(buckets, actualBuckets, bucketsFilled);
                buckets = actualBuckets;
            }

            return buckets;
        }

        private void BucketRows(T[] values, int index, int length, byte[] rowBuckets)
        {
            this.RowBucketIndex = rowBuckets;
            SortBucketColumnN.Bucket<T>(values, index, length, this.Minimum, this.RowBucketIndex, this.RowCount, this.IsMultiValue);
        }

        private void Merge(SortBucketColumn<T> other)
        {
            // Merge min and max
            if (other.Min.CompareTo(this.Min) < 0) this.Min = other.Min;
            if (other.Max.CompareTo(this.Max) > 0) this.Max = other.Max;

            // Merge IsMultiValue, RowCount
            for (int i = 0; i < this.Minimum.Length - 1; ++i)
            {
                this.IsMultiValue[i] |= other.IsMultiValue[i];
                this.RowCount[i] += other.RowCount[i];
            }

            this.RowCount[this.RowCount.Length - 1] += other.RowCount[other.RowCount.Length - 1];
        }

        public int BucketForValue(T value, out bool isExact)
        {
            int index = SortBucketColumnN.BucketIndex<T>(this.Minimum, value);

            isExact = (index < 0);
            if (isExact) index = ~index;
            return index;
        }

        public static SortBucketColumn<T> Read(string partitionPath, string columnName)
        {
            SortBucketColumn<T> result = new SortBucketColumn<T>(columnName, BinarySerializer.Read<T>(Path.Combine(partitionPath, columnName, "SV")));

            result.IsMultiValue = BinarySerializer.Read<bool>(Path.Combine(partitionPath, columnName, "SSV"));
            result.RowCount = BinarySerializer.Read<int>(Path.Combine(partitionPath, columnName, "SSC"));

            result.RowBucketIndex = BinarySerializer.Read<byte>(Path.Combine(partitionPath, columnName, "SSR"));

            return result;
        }

        public void Write(string partitionPath)
        {
            BinarySerializer.Write(Path.Combine(partitionPath, this.Name, "SV"), this.Minimum);
            BinarySerializer.Write(Path.Combine(partitionPath, this.Name, "SSV"), this.IsMultiValue);
            BinarySerializer.Write(Path.Combine(partitionPath, this.Name, "SC"), this.RowCount);

            BinarySerializer.Write(Path.Combine(partitionPath, this.Name, "SSR"), this.RowBucketIndex);
        }
    }
}

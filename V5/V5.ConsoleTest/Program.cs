using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using V5.Extensions;

namespace V5.ConsoleTest
{
    public class SortBucketColumn<T>
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
                if(!value.Equals(this.BucketMinimumValue[bucketsFilled - 1]))
                {
                    this.BucketMinimumValue[bucketsFilled] = value;
                    bucketsFilled++;

                    if (bucketsFilled == bucketCount) break;
                }
            }

            // Capture the set actually filled
            if(bucketsFilled < bucketCount)
            {
                T[] actualBuckets = new T[bucketsFilled];
                Array.Copy(this.BucketMinimumValue, actualBuckets, bucketsFilled);
                this.BucketMinimumValue = actualBuckets;
            }

            this.IsMultiValueBucket = new bool[this.BucketMinimumValue.Length];
            this.BucketRowCount = new int[this.BucketMinimumValue.Length];

            BucketRows(values);
        }

        public void BucketRows(T[] values)
        {
            this.BucketIndexPerRow = new byte[values.Length];

            for(int i = 0; i < values.Length; ++i)
            {
                T value = values[i];
                int bucketIndex = Array.BinarySearch(this.BucketMinimumValue, value);

                if (bucketIndex < 0)
                {
                    bucketIndex = ~bucketIndex - 1;

                    if(bucketIndex < 0)
                    {
                        this.BucketMinimumValue[0] = value;
                        bucketIndex = 0;
                    }
                    else if(bucketIndex == this.BucketMinimumValue.Length - 1)
                    {
                        this.BucketMinimumValue[this.BucketMinimumValue.Length - 1] = value;
                    }

                    this.IsMultiValueBucket[bucketIndex] = true;
                }

                this.BucketIndexPerRow[i] = (byte)bucketIndex;
                this.BucketRowCount[bucketIndex]++;
            }
        }
    }

    public class PersonDatabase
    {
        public DateTime[] BirthDate;
        public DateTime[] WhenAdded;
        public int[] ZipCode;

        public SortBucketColumn<DateTime> BirthDateBuckets;
        public SortBucketColumn<DateTime> WhenAddedBuckets;
        public SortBucketColumn<int> ZipCodeBuckets;

        public PersonDatabase(long capacity)
        {
            this.BirthDate = new DateTime[capacity];
            this.WhenAdded = new DateTime[capacity];
            this.ZipCode = new int[capacity];
        }

        public int Count => this.BirthDate.Length;

        public void Load(string filePath)
        {
            this.BirthDate = BinarySerializer.Read<long>(Path.Combine(filePath, "BirthDate", "V.d64.bin")).ToDateTimeArray();
            this.WhenAdded = BinarySerializer.Read<long>(Path.Combine(filePath, "WhenAdded", "V.d64.bin")).ToDateTimeArray();
            this.ZipCode = BinarySerializer.Read<int>(Path.Combine(filePath, "ZipCode", "V.i32.bin"));
        }

        public void Index(Random r)
        {
            this.BirthDateBuckets = new SortBucketColumn<DateTime>();
            this.BirthDateBuckets.Build(this.BirthDate, 255, r);

            this.WhenAddedBuckets = new SortBucketColumn<DateTime>();
            this.WhenAddedBuckets.Build(this.WhenAdded, 255, r);

            this.ZipCodeBuckets = new SortBucketColumn<int>();
            this.ZipCodeBuckets.Build(this.ZipCode, 255, r);
        }

        public void Save(string filePath)
        {
            BinarySerializer.Write(Path.Combine(filePath, "BirthDate", "V.d64.bin"), BirthDate.ToPrimitiveArray());
            BinarySerializer.Write(Path.Combine(filePath, "WhenAdded", "V.d64.bin"), WhenAdded.ToPrimitiveArray());
            BinarySerializer.Write(Path.Combine(filePath, "ZipCode", "V.i32.bin"), this.ZipCode);
        }
    }

    class Program
    {
        public const string PartitionPath = @"..\..\..\..\DiskCache\Tables\Person\0";

        static void Main(string[] args)
        {
            long rowCount = 8 * 1000 * 1000; // 0x1 << 23
            PersonDatabase db = new PersonDatabase(rowCount);

            if (Directory.Exists(PartitionPath))
            {
                using (new TraceWatch("Loading Database..."))
                {
                    db.Load(PartitionPath);
                    Trace.WriteLine($" -> {db.Count:n0} rows");
                }
            }
            else
            { 
                using (new TraceWatch($"Generating {rowCount.CountString()} sample rows..."))
                {
                    Random r = new Random(5);
                    for (long i = 0; i < rowCount; ++i)
                    {
                        Person p = new Person(r);
                        db.BirthDate[i] = p.BirthDate;
                        db.WhenAdded[i] = p.WhenAdded;
                        db.ZipCode[i] = p.ZipCode;
                    }
                }

                using (new TraceWatch("Saving Database..."))
                {
                    db.Save(PartitionPath);
                }
            }

            using (new TraceWatch("Indexing Database..."))
            {
                db.Index(new Random(0));
            }

            Benchmark.Compare("BirthDate >= 1980-01-01 AND ZIP >= 90000", 100, db.Count, new string[] { "Managed Hand-Coded" },
                () => CountCustom(db));

            //byte rangeStart = 0;
            //byte rangeEnd = 4;

            //int rm = CountManaged(test, rangeStart, rangeEnd);
            //int rn = String8Native.CountInRange(test, rangeStart, rangeEnd);
            //Console.WriteLine($"Managed: {rm:n0}, Native: {rn:n0}");

            //Benchmark.Compare("Count bytes in range", 100, length, new string[] { "Managed", "Native" },
            //    () => CountManaged(test, rangeStart, rangeEnd),
            //    () => String8Native.CountInRange(test, rangeStart, rangeEnd)
            //);

            //uint[] managedVector = new uint[length >> 5];
            //uint[] nativeVector = new uint[length >> 5];
            //sbyte edge = 100;

            //Array.Clear(managedVector, 0, managedVector.Length);
            //WhereGreaterThan(test, edge, managedVector);
            //int managed = Count(managedVector);

            //Array.Clear(nativeVector, 0, nativeVector.Length);
            //ArraySearch.WhereGreaterThan(test, edge, nativeVector);
            //int native = Count(nativeVector);

            //if(!AreEqual(managedVector, nativeVector))
            //{
            //    Console.WriteLine("ERROR");
            //}

            //Console.WriteLine($"Managed: {managed}, Native: {native}");

            //Benchmark.Compare("Find Items in Range", 100, length, new string[] { "Managed", "Native" },
            //    () => WhereGreaterThan(test, edge, managedVector),
            //    () => ArraySearch.WhereGreaterThan(test, edge, nativeVector)
            //);
        }

        private static int CountCustom(PersonDatabase db)
        {
            DateTime birthdayMinimum = new DateTime(1980, 01, 01);
            int zipMinimum = 90000;

            int count = 0;
            for(int i = 0; i < db.Count; ++i)
            {
                if (db.BirthDate[i] >= birthdayMinimum && db.ZipCode[i] >= zipMinimum) count++;
            }
            return count;
        }


        private static int CountManaged(byte[] test, byte rangeStart, byte rangeEnd)
        {
            int count = 0;

            for (int i = 0; i < test.Length; ++i)
            {
                byte value = test[i];
                if (value >= rangeStart && value <= rangeEnd) count++;
            }

            return count;
        }

        private static void WhereGreaterThan(byte[] test, sbyte value, uint[] resultVector)
        {
            for (int i = 0; i < test.Length; ++i)
            {
                if ((sbyte)test[i] > value)
                {
                    resultVector[i >> 5] |= (0x1U << (i & 31));
                }
            }
        }

        private static int Count(uint[] resultVector)
        {
            int count = 0;

            for(int i = 0; i < resultVector.Length; ++i)
            {
                uint segment = resultVector[i];

                for(int j = 0; j < 32; ++j)
                {
                    if ((segment & (0x1U << j)) != 0) count++;
                }
            }

            return count;
        }

        private static bool AreEqual(uint[] left, uint[] right)
        {
            if (left.Length != right.Length) return false;

            for(int i = 0; i < left.Length; ++i)
            {
                if (left[i] != right[i]) return false;
            }

            return true;
        }
    }
}

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using V5.Data;
using V5.Extensions;

namespace V5.ConsoleTest
{
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
            this.BirthDateBuckets = SortBucketColumn<DateTime>.Build(this.BirthDate, 255, r);
            this.WhenAddedBuckets = SortBucketColumn<DateTime>.Build(this.WhenAdded, 255, r);
            this.ZipCodeBuckets = SortBucketColumn<int>.Build(this.ZipCode, 255, r);
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

            int managedMatches = CountCustom(db);
            int nativeMatches = CountNative(db, new uint[db.Count + 31 >> 5]);

            Benchmark.Compare("BirthDate > 1980-01-01 AND ZIP > 90000", 100, db.Count, new string[] { "Managed Hand-Coded", "Native Hand-Coded" },
                () => CountCustom(db),
                () => CountNative(db, new uint[db.Count + 31 >> 5]));



            uint[] managedVector = new uint[rowCount + 31 >> 5];
            uint[] nativeVector = new uint[rowCount + 31 >> 5];
            byte edge = 220;

            Array.Clear(managedVector, 0, managedVector.Length);
            WhereGreaterThan(db.BirthDateBuckets.RowBucketIndex, edge, managedVector);
            int managed = Count(managedVector);

            for (int i = 0; i < nativeVector.Length; ++i)
            {
                nativeVector[i] = uint.MaxValue;
            }

            ArraySearch.AndWhereGreaterThan(db.BirthDateBuckets.RowBucketIndex, edge, nativeVector);
            int native = Count(nativeVector);

            if (!AreEqual(managedVector, nativeVector))
            {
                Console.WriteLine("ERROR");
            }

            //Console.WriteLine($"Managed: {managed}, Native: {native}");

            Benchmark.Compare("Find Items in Range", 100, rowCount, new string[] { "Managed", "Native" },
                () => WhereGreaterThan(db.BirthDateBuckets.RowBucketIndex, edge, managedVector),
                () => ArraySearch.AndWhereGreaterThan(db.BirthDateBuckets.RowBucketIndex, edge, nativeVector)
            );
        }

        private static int CountCustom(PersonDatabase db)
        {
            DateTime birthdayMinimum = new DateTime(1960, 01, 01);
            int zipMinimum = 60000;

            int count = 0;
            for(int i = 0; i < db.Count; ++i)
            {
                if (db.ZipCode[i] > zipMinimum && db.BirthDate[i] > birthdayMinimum) count++;
            }
            return count;
        }

        private static int CountNative(PersonDatabase db, uint[] bitVector)
        {
            // Set to all
            for(int i = 0; i < bitVector.Length; ++i)
            {
                bitVector[i] = uint.MaxValue;
            }

            DateTime birthdayMinimum = new DateTime(1960, 01, 01);
            int zipMinimum = 60000;

            bool isBirthdayExact;
            int birthdayBucket = db.BirthDateBuckets.BucketForValue(birthdayMinimum, out isBirthdayExact);
            if (birthdayBucket < 0 || birthdayBucket > db.BirthDateBuckets.Minimum.Length - 1) return 0;

            bool isZipExact;
            int zipBucket = db.ZipCodeBuckets.BucketForValue(zipMinimum, out isZipExact);
            if (zipBucket < 0 || zipBucket > db.ZipCodeBuckets.Minimum.Length - 1) return 0;

            ArraySearch.AndWhereGreaterThan(db.BirthDateBuckets.RowBucketIndex, (byte)birthdayBucket, bitVector);
            ArraySearch.AndWhereGreaterThan(db.ZipCodeBuckets.RowBucketIndex, (byte)zipBucket, bitVector);

            return ArraySearch.Count(bitVector);
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

        private static void WhereGreaterThan(byte[] test, byte value, uint[] resultVector)
        {
            for (int i = 0; i < test.Length; ++i)
            {
                if (test[i] > value)
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

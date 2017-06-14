using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using V5.Collections;
using V5.Data;
using V5.Extensions;

namespace V5.ConsoleTest
{
    public class PersonDatabase
    {
        public const int ParallelCount = 1;

        public long[] BirthDate;
        public long[] WhenAdded;
        public int[] ZipCode;

        public SortBucketColumn<long> BirthDateBuckets;
        public SortBucketColumn<long> WhenAddedBuckets;
        public SortBucketColumn<int> ZipCodeBuckets;

        public PersonDatabase(long capacity)
        {
            this.BirthDate = new long[capacity];
            this.WhenAdded = new long[capacity];
            this.ZipCode = new int[capacity];
        }

        public int Count => this.BirthDate.Length;

        public void Load(string filePath)
        {
            this.BirthDate = BinarySerializer.Read<long>(Path.Combine(filePath, "BirthDate", "V.d64.bin"));//.ToDateTimeArray();
            this.WhenAdded = BinarySerializer.Read<long>(Path.Combine(filePath, "WhenAdded", "V.d64.bin"));//.ToDateTimeArray();
            this.ZipCode = BinarySerializer.Read<int>(Path.Combine(filePath, "ZipCode", "V.i32.bin"));
        }

        public void Index(Random r)
        {
            this.BirthDateBuckets = SortBucketColumn<long>.Build(this.BirthDate, 255, r, ParallelCount);
            this.WhenAddedBuckets = SortBucketColumn<long>.Build(this.WhenAdded, 255, r, ParallelCount);
            this.ZipCodeBuckets = SortBucketColumn<int>.Build(this.ZipCode, 255, r, ParallelCount);
        }

        public void Save(string filePath)
        {
            BinarySerializer.Write(Path.Combine(filePath, "BirthDate", "V.d64.bin"), BirthDate);//.ToPrimitiveArray());
            BinarySerializer.Write(Path.Combine(filePath, "WhenAdded", "V.d64.bin"), WhenAdded);//.ToPrimitiveArray());
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
                        db.BirthDate[i] = p.BirthDate.ToUniversalTime().Ticks;
                        db.WhenAdded[i] = p.WhenAdded.ToUniversalTime().Ticks;
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

            IndexSet managedSet = new IndexSet(0, db.Count);
            IndexSet nativeSet = new IndexSet(0, db.Count).All();
            IndexSet scratchSet = new IndexSet(0, db.Count);
            int managedMatches = CountCustom(db);

            Benchmark.Compare("IndexSet Ops", 1000, db.Count, new string[] { "All", "None", "Count" },
                () => nativeSet.All(),
                () => nativeSet.None(),
                () => { int x = nativeSet.Count; }
            );

            Benchmark.Compare("BirthDate > 1980-01-01 AND ZIP > 90000", 100, db.Count, new string[] { "Managed Hand-Coded", "Native Hand-Coded", "Native separate and" },
                () => CountCustom(db),
                () => CountNative(db, nativeSet),
                () => CountNativeSeparate(db, nativeSet, scratchSet)
            );

            byte edge = 250;

            managedSet.None();
            WhereGreaterThan(db.BirthDateBuckets.RowBucketIndex, edge, managedSet);

            nativeSet.All().And(db.BirthDateBuckets.RowBucketIndex, Query.Operator.GreaterThan, edge);

            if (!managedSet.Equals(nativeSet) || managedSet.Count != nativeSet.Count)
            {
                Console.WriteLine("ERROR");
            }

            ulong[] directVector = new ulong[db.Count + 63 >> 6];

            Benchmark.Compare("Find Items in Range", 100, rowCount, new string[] { "Managed", "Native", "NativeDirect" },
                () => WhereGreaterThan(db.BirthDateBuckets.RowBucketIndex, edge, managedSet),
                () => nativeSet.And(db.BirthDateBuckets.RowBucketIndex, Query.Operator.GreaterThan, edge),
                () => IndexSetN.AndWhereGreaterThan(db.BirthDateBuckets.RowBucketIndex, edge, directVector)
            );
        }

        private static int CountCustom(PersonDatabase db)
        {
            long birthdayMinimum = new DateTime(1960, 01, 01).ToUniversalTime().Ticks;
            int zipMinimum = 60000;

            int count = 0;
            for (int i = 0; i < db.Count; ++i)
            {
                if (db.ZipCode[i] > zipMinimum && db.BirthDate[i] > birthdayMinimum) count++;
            }
            return count;
        }

        private static int CountNative(PersonDatabase db, IndexSet matches)
        {
            long birthdayMinimum = new DateTime(1960, 01, 01).ToUniversalTime().Ticks;
            int zipMinimum = 60000;

            bool isBirthdayExact;
            int birthdayBucket = db.BirthDateBuckets.BucketForValue(birthdayMinimum, out isBirthdayExact);
            if (birthdayBucket < 0 || birthdayBucket > db.BirthDateBuckets.Minimum.Length - 1) return 0;

            bool isZipExact;
            int zipBucket = db.ZipCodeBuckets.BucketForValue(zipMinimum, out isZipExact);
            if (zipBucket < 0 || zipBucket > db.ZipCodeBuckets.Minimum.Length - 1) return 0;

            matches.All()
                .And(db.BirthDateBuckets.RowBucketIndex, Query.Operator.GreaterThan, (byte)birthdayBucket)
                .And(db.ZipCodeBuckets.RowBucketIndex, Query.Operator.GreaterThan, (byte)zipBucket);

            return matches.Count;
        }

        private static int CountNativeSeparate(PersonDatabase db, IndexSet matches1, IndexSet matches2)
        {
            long birthdayMinimum = new DateTime(1960, 01, 01).ToUniversalTime().Ticks;
            int zipMinimum = 60000;

            bool isBirthdayExact;
            int birthdayBucket = db.BirthDateBuckets.BucketForValue(birthdayMinimum, out isBirthdayExact);
            if (birthdayBucket < 0 || birthdayBucket > db.BirthDateBuckets.Minimum.Length - 1) return 0;

            bool isZipExact;
            int zipBucket = db.ZipCodeBuckets.BucketForValue(zipMinimum, out isZipExact);
            if (zipBucket < 0 || zipBucket > db.ZipCodeBuckets.Minimum.Length - 1) return 0;

            matches1.All().And(db.BirthDateBuckets.RowBucketIndex, Query.Operator.GreaterThan, (byte)birthdayBucket);
            matches2.All().And(db.ZipCodeBuckets.RowBucketIndex, Query.Operator.GreaterThan, (byte)zipBucket);

            matches1.And(matches2);

            return matches1.Count;
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

        private static void WhereGreaterThan(byte[] test, byte value, IndexSet result)
        {
            for (int i = 0; i < test.Length; ++i)
            {
                if (test[i] > value)
                {
                    result[i] = true;
                }
            }
        }
    }
}

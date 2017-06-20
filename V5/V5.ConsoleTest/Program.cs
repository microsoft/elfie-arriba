using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using V5;
using V5.Collections;
using V5.Data;

namespace V5.ConsoleTest
{
    public class PersonDatabase
    {
        public const int ParallelCount = 2;

        public PrimitiveColumn<long> BirthDate;
        public PrimitiveColumn<long> WhenAdded;
        public PrimitiveColumn<int> ZipCode;

        public SortBucketColumn<long> BirthDateBuckets;
        public SortBucketColumn<long> WhenAddedBuckets;
        public SortBucketColumn<int> ZipCodeBuckets;

        public PersonDatabase(long capacity)
        {
            this.BirthDate = new PrimitiveColumn<long>("BirthDate", new long[capacity]);
            this.WhenAdded = new PrimitiveColumn<long>("WhenAdded", new long[capacity]);
            this.ZipCode = new PrimitiveColumn<int>("ZipCode", new int[capacity]);
        }

        public uint Count => (uint)this.BirthDate.Count;

        public void Index(Random r)
        {
            this.BirthDateBuckets = SortBucketColumn<long>.Build("BirthDate", this.BirthDate.Values, 255, r, ParallelCount);
            this.WhenAddedBuckets = SortBucketColumn<long>.Build("WhenAdded", this.WhenAdded.Values, 255, r, ParallelCount);
            this.ZipCodeBuckets = SortBucketColumn<int>.Build("ZipCode", this.ZipCode.Values, 255, r, ParallelCount);
        }

        public void Load(string filePath)
        {
            this.BirthDate = PrimitiveColumn<long>.Read(filePath, "BirthDate");
            this.WhenAdded = PrimitiveColumn<long>.Read(filePath, "WhenAdded");
            this.ZipCode = PrimitiveColumn<int>.Read(filePath, "ZipCode");

            this.BirthDateBuckets = SortBucketColumn<long>.Read(filePath, "BirthDate");
            this.WhenAddedBuckets = SortBucketColumn<long>.Read(filePath, "WhenAdded");
            this.ZipCodeBuckets = SortBucketColumn<int>.Read(filePath, "ZipCode");
        }

        public void Save(string filePath)
        {
            this.BirthDate.Write(filePath);
            this.WhenAdded.Write(filePath);
            this.ZipCode.Write(filePath);

            this.BirthDateBuckets.Write(filePath);
            this.WhenAddedBuckets.Write(filePath);
            this.ZipCodeBuckets.Write(filePath);
        }
    }

    class Program
    {
        public const string PartitionPath = @"..\..\..\DiskCache\Tables\Person\0";

        static void Main(string[] args)
        {
            bool isBoolPrimitive = typeof(bool).IsPrimitive;

            long rowCount = 8 * 1000 * 1000; // 0x1 << 23
            PersonDatabase db = new PersonDatabase(rowCount);

            if (Directory.Exists(PartitionPath))
            {
                using (new TraceWatch("Loading Database..."))
                {
                    db.Load(PartitionPath);
                    Trace.WriteLine($" -> {db.Count:n0} rows");
                }

                using (new TraceWatch("Indexing Database [not needed in load]..."))
                {
                    db.Index(new Random(0));
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
                        db.BirthDate.Values[i] = p.BirthDate.ToUniversalTime().Ticks;
                        db.WhenAdded.Values[i] = p.WhenAdded.ToUniversalTime().Ticks;
                        db.ZipCode.Values[i] = p.ZipCode;
                    }
                }

                using (new TraceWatch("Indexing Database..."))
                {
                    db.Index(new Random(0));
                }

                using (new TraceWatch("Saving Database..."))
                {
                    db.Save(PartitionPath);
                }
            }

            IndexSet managedSet = new IndexSet(db.Count);
            IndexSet nativeSet = new IndexSet(db.Count).All(db.Count);
            IndexSet scratchSet = new IndexSet(db.Count);
            int managedMatches = CountManagedDirectArrays(db, managedSet);

            Benchmark.Compare("IndexSet Ops", 1000, db.Count, new string[] { "All", "None", "Count" },
                () => nativeSet.All(db.Count),
                () => nativeSet.None(),
                () => { int x = nativeSet.Count; }
            );

            Benchmark.Compare("BirthDate > 1960-01-01 AND ZIP > 60000", 20, db.Count, new string[] { "Managed Hand-Coded", "Managed Column", "Native Hand-Coded", "Native separate and" },
                () => CountManagedDirectArrays(db, managedSet),
                () => CountManagedColumn(db, managedSet),
                () => CountNative(db, nativeSet),
                () => CountNativeSeparate(db, nativeSet, scratchSet)
            );

            byte edge = 250;

            managedSet.None();
            WhereGreaterThan(db.BirthDateBuckets.RowBucketIndex, edge, managedSet);

            nativeSet.All(db.Count).And(db.BirthDateBuckets.RowBucketIndex, CompareOperator.GreaterThan, edge);

            if (!managedSet.Equals(nativeSet) || managedSet.Count != nativeSet.Count)
            {
                Console.WriteLine("ERROR");
            }

            ulong[] directVector = new ulong[db.Count + 63 >> 6];

            Benchmark.Compare("Find Items in Range", 100, rowCount, new string[] { "Managed", "Native" },
                () => WhereGreaterThan(db.BirthDateBuckets.RowBucketIndex, edge, managedSet),
                () => nativeSet.And(db.BirthDateBuckets.RowBucketIndex, CompareOperator.GreaterThan, edge)
            );
        }

        private static int CountManagedDirectArrays(PersonDatabase db, IndexSet matches)
        {
            long birthdayMinimum = new DateTime(1960, 01, 01).ToUniversalTime().Ticks;
            int zipMinimum = 60000;

            long[] birthDates = db.BirthDate.Values;
            int[] zipCodes = db.ZipCode.Values;

            for (int i = 0; i < db.Count; ++i)
            {
                if (zipCodes[i] > zipMinimum && birthDates[i] > birthdayMinimum) matches[i] = true;
            }
            return matches.Count;
        }

        private static int CountManagedColumn(PersonDatabase db, IndexSet matches)
        {
            long birthdayMinimum = new DateTime(1960, 01, 01).ToUniversalTime().Ticks;
            int zipMinimum = 60000;

            matches.All(db.Count);
            db.BirthDate.And(matches, CompareOperator.GreaterThan, birthdayMinimum);
            db.WhenAdded.And(matches, CompareOperator.GreaterThan, zipMinimum);

            return matches.Count;
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

            matches.All(db.Count)
                .And(db.BirthDateBuckets.RowBucketIndex, CompareOperator.GreaterThan, (byte)birthdayBucket)
                .And(db.ZipCodeBuckets.RowBucketIndex, CompareOperator.GreaterThan, (byte)zipBucket);

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

            matches1.All(db.Count).And(db.BirthDateBuckets.RowBucketIndex, CompareOperator.GreaterThan, (byte)birthdayBucket);
            matches2.All(db.Count).And(db.ZipCodeBuckets.RowBucketIndex, CompareOperator.GreaterThan, (byte)zipBucket);

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

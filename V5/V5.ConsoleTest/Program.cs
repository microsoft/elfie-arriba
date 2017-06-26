using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using V5;
using V5.Collections;
using V5.ConsoleTest.Model;
using V5.Data;

namespace V5.ConsoleTest
{
    public class WebRequestDatabase
    {
        public const int ParallelCount = 2;

        public PrimitiveColumn<long> EventTime;
        public PrimitiveColumn<ushort> HttpStatus;
        public PrimitiveColumn<int> ResponseBytes;

        public SortBucketColumn<long> EventTimeBuckets;
        public SortBucketColumn<ushort> HttpStatusBuckets;
        public SortBucketColumn<int> ResponseBytesBuckets;

        public WebRequestDatabase(long capacity)
        {
            this.EventTime = new PrimitiveColumn<long>("EventTime", new long[capacity]);
            this.HttpStatus = new PrimitiveColumn<ushort>("HttpStatus", new ushort[capacity]);
            this.ResponseBytes = new PrimitiveColumn<int>("ResponseBytes", new int[capacity]);
        }

        public uint Count => (uint)this.EventTime.Count;

        public void Index(Random r)
        {
            this.EventTimeBuckets = SortBucketColumn<long>.Build("EventTime", this.EventTime.Values, 255, r, ParallelCount);
            this.HttpStatusBuckets = SortBucketColumn<ushort>.Build("HttpStatus", this.HttpStatus.Values, 255, r, ParallelCount);
            this.ResponseBytesBuckets = SortBucketColumn<int>.Build("ResponseBytes", this.ResponseBytes.Values, 255, r, ParallelCount);
        }

        public void Load(string filePath)
        {
            this.EventTime = PrimitiveColumn<long>.Read(filePath, "EventTime");
            this.HttpStatus = PrimitiveColumn<ushort>.Read(filePath, "HttpStatus");
            this.ResponseBytes = PrimitiveColumn<int>.Read(filePath, "ResponseBytes");

            this.EventTimeBuckets = SortBucketColumn<long>.Read(filePath, "EventTime");
            this.HttpStatusBuckets = SortBucketColumn<ushort>.Read(filePath, "HttpStatus");
            this.ResponseBytesBuckets = SortBucketColumn<int>.Read(filePath, "ResponseBytes");
        }

        public void Save(string filePath)
        {
            this.EventTime.Write(filePath);
            this.HttpStatus.Write(filePath);
            this.ResponseBytes.Write(filePath);

            this.EventTimeBuckets.Write(filePath);
            this.HttpStatusBuckets.Write(filePath);
            this.ResponseBytesBuckets.Write(filePath);
        }
    }

    class Program
    {
        public const string PartitionPath = @"..\..\..\DiskCache\Tables\Person\0";

        static void Main(string[] args)
        {
            //PerformanceTests();
            //return;

            int rowCount = 8 * 1000 * 1000;
            WebRequestDatabase db = new WebRequestDatabase(rowCount);
            V0.WebRequestDatabase db0 = new V0.WebRequestDatabase();

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
                List<WebRequest> data = null;

                using (new TraceWatch($"Generating {rowCount:n0} sample rows..."))
                {
                    WebRequestGenerator g = new WebRequestGenerator(new Random(5), DateTime.UtcNow.AddMonths(-6), 250);
                    data = g.Next(rowCount);
                }

                db0.Requests = data;

                using (new TraceWatch("Copying into Database..."))
                {
                    for (int i = 0; i < rowCount; ++i)
                    {
                        WebRequest row = data[i];
                        db.EventTime.Values[i] = row.EventTime.Ticks;
                        db.HttpStatus.Values[i] = row.HttpStatus;
                        db.ResponseBytes.Values[i] = row.ResponseBytes;
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

            IndexSet set = new IndexSet(db.Count);
            Span<int> page = new Span<int>(new int[4096]);

            Benchmark.Compare("HttpStatus = 404 AND ResponseBytes > 1000", 20, db.Count, new string[] { "Managed Direct", "Managed Column", "V5.Native" },
                () => QueryManagedDirect(db, set),
                () => QueryManagedColumn(db, set, page),
                () => QueryV5(db, set, page)
            );
        }

        static void PerformanceTests()
        {
            int iterations = 100;
            uint size = 8 * 1000 * 1000;

            int sum;
            IndexSet set = new IndexSet(size);
            Span<int> page = new Span<int>(new int[4096]);

            byte[] bucketSample = new byte[size];
            Span<byte> bucketSpan = new Span<byte>(bucketSample);

            Random r = new Random(6);
            r.NextBytes(bucketSample);

            Benchmark.Compare("Span Operations", iterations, size, new string[] { "Array For", "Array ForEach", "Span For", "Span ForEach" },
                () => { sum = 0; for (int i = 0; i < bucketSample.Length; ++i) { sum += bucketSample[i]; } return sum; },
                () => { sum = 0; foreach (int item in bucketSample) { sum += item; } return sum; },
                () => { sum = 0; for (int i = 0; i < bucketSpan.Length; ++i) { sum += bucketSpan[i]; } return sum; },
                () => { sum = 0; foreach (int item in bucketSpan) { sum += item; } return sum; }
            );

            Benchmark.Compare("IndexSet Operations", iterations, size, new string[] { "All", "None", "Count", "WhereGreaterThan" },
                () => set.All(size),
                () => set.None(),
                () => set.Count,
                () => set.And(bucketSample, CompareOperator.GreaterThan, (byte)200)
            );

            set.None();
            Benchmark.Compare("IndexSet Page", iterations, size, new string[] { "Page None" }, () => PageAll(set, page));

            set.None();
            for (int i = 0; i < set.Capacity; i += 50)
            {
                set[i] = true;
            }
            Benchmark.Compare("IndexSet Page", iterations, size, new string[] { "Page 1/50" }, () => PageAll(set, page));

            set.None();
            for (int i = 0; i < set.Capacity; i += 10)
            {
                set[i] = true;
            }
            Benchmark.Compare("IndexSet Page", iterations, size, new string[] { "Page 1/10" }, () => PageAll(set, page));

            set.All(size);
            Benchmark.Compare("IndexSet Page", iterations, size, new string[] { "Page All" }, () => PageAll(set, page));
        }

        private static int PageAll(IndexSet set, Span<int> page)
        {
            int count = 0;

            int next = 0;
            while (next != -1)
            {
                next = set.Page(ref page, next);
                count += page.Length;
            }

            return count;
        }

        private static int QueryManagedDirect(V0.WebRequestDatabase db, IndexSet matches)
        {
            for (int i = 0; i < db.Requests.Count; ++i)
            {
                if (db.Requests[i].HttpStatus == 404 && db.Requests[i].ResponseBytes > 1000) matches[i] = true;
            }

            return matches.Count;
        }

        private static int QueryManagedDirect(WebRequestDatabase db, IndexSet matches)
        {
            for (int i = 0; i < db.Count; ++i)
            {
                if (db.HttpStatus.Values[i] == 404 && db.ResponseBytes.Values[i] > 1000) matches[i] = true;
            }

            return matches.Count;
        }

        private static int QueryManagedColumn(WebRequestDatabase db, IndexSet matches, Span<int> page)
        {
            matches.All(db.Count);
            db.HttpStatus.And(matches, CompareOperator.Equals, 404);
            db.ResponseBytes.And(matches, CompareOperator.GreaterThan, 1000);

            return matches.Count;
        }

        private static int QueryV5(WebRequestDatabase db, IndexSet matches, Span<int> page)
        {
            // Look up the buckets for HttpStatus 404 and ResponseBytes 1000
            bool isHttpStatusExact;
            int httpStatusBucket = db.HttpStatusBuckets.BucketForValue(404, out isHttpStatusExact);
            bool needHttpStatusPostScan = (isHttpStatusExact == false && db.HttpStatusBuckets.IsMultiValue[httpStatusBucket]);

            bool isResponseBytesExact;
            int responseBytesBucket = db.ResponseBytesBuckets.BucketForValue(1000, out isResponseBytesExact);
            bool needResponseBytesPostScan = (isResponseBytesExact == false && db.ResponseBytesBuckets.IsMultiValue[responseBytesBucket]);

            // Get matches in those bucket ranges and intersect them
            matches.All(db.Count);
            matches.And(db.HttpStatusBuckets.RowBucketIndex, CompareOperator.Equals, (byte)httpStatusBucket);
            matches.And(db.ResponseBytesBuckets.RowBucketIndex, CompareOperator.GreaterThan, (byte)responseBytesBucket);

            //return matches.Count;

            // If no post-scans were required, return the bit vector count
            if (!needHttpStatusPostScan && !needResponseBytesPostScan) return matches.Count;

            // Otherwise, page through results and post-filter on required clauses
            // [NOTE: We should prefer to scan twice and only filter boundary bucket rows when there are many matches]
            int count = 0;
            int matchesBefore = 0;

            int next = 0;
            while (next != -1)
            {
                next = matches.Page(ref page, next);
                matchesBefore += page.Length;

                if (needHttpStatusPostScan) db.HttpStatus.And(ref page, CompareOperator.Equals, 404);
                if (needResponseBytesPostScan) db.ResponseBytes.And(ref page, CompareOperator.GreaterThan, 1000);

                count += page.Length;
            }

            // Return the final count
            return count;
        }
    }
}

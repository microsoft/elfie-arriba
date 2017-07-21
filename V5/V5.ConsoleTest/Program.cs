using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using V5;
using V5.Collections;
using V5.ConsoleTest.Model;
using V5.Data;
using V5.Serialization;

namespace V5.ConsoleTest
{
    public class WebRequestDatabase
    {
        public const int ParallelCount = 4;

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

        public int Count => this.EventTime.Count;

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
        public const int ParallelCount = 2;
        public const string PartitionPath = @"..\..\..\DiskCache\Tables\Person\0";

        static void Main(string[] args)
        {
            PerformanceTests();
            return;

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

            IndexSet[] sets = new IndexSet[ParallelCount];
            Span<int>[] pages = new Span<int>[ParallelCount];
            for(int i = 0; i < ParallelCount; ++i)
            {
                sets[i] = new IndexSet(db.Count / ParallelCount);
                pages[i] = new Span<int>(new int[4096]);
            }

            Benchmark.Compare("HttpStatus = 404 AND ResponseBytes > 1000", 20, db.Count, new string[] { "Managed Direct", "Managed Column", "V5.Native" },
                () => QueryManagedDirect(db, set),
                () => QueryManagedColumn(db, set, page),
                () => QueryV5(db, sets, pages)
            );

            PerformanceTests();
        }

        private static void CompressValues(ushort[] values, int bitsPerValue, byte[] destination, int valueIndex)
        {
            if ((values.Length * bitsPerValue) % 8 != 0) throw new InvalidOperationException("CompressValues must compress in blocks which produce an even number of bytes. Compress multiples of 8 rows.");

            // Prepare a mask to get the bottom 'bitsPerValue' bits from each item
            byte valueMask = (byte)(0xFF >> (8 - bitsPerValue));

            // Calculate the first write position in the output array
            int bitIndex = 0;
            int destinationIndex = (valueIndex * bitsPerValue) / 8;
            byte current = 0;

            // Copy each item into the correct slice of bits
            for(int itemIndex = 0; itemIndex < values.Length; ++itemIndex)
            {
                // Get the bits for the next item
                byte valueBits = (byte)(values[itemIndex] & valueMask);
                
                // Write them at the next position
                current |= (byte)(valueBits << bitIndex);

                // Calculate the start of the next value
                bitIndex += bitsPerValue;
                if(bitIndex >= 8)
                {
                    // If it's in the next byte, commit the last one
                    destination[destinationIndex] = current;

                    destinationIndex++;
                    bitIndex -= 8;

                    // Start the next byte with any item bits which overlap into it
                    current = (byte)(bitIndex == 0 ? 0 : valueBits >> (bitsPerValue - bitIndex));
                }
            }

            // Write the last value, if needed
            if (current != 0) destination[destinationIndex] = current;
        }

        static void PerformanceTests()
        {
            int iterations = 250;
            int size = 64 * 1000 * 1000;
            int bitsPerValue = 4;

            IndexSet set = new IndexSet(size);
            IndexSet other = new IndexSet(size);
            Span<int> page = new Span<int>(new int[4096]);

            byte[] bucketSample = new byte[(size * bitsPerValue) / 8];
            Span<byte> bucketSpan = new Span<byte>(bucketSample);

            ushort[] values = new ushort[16];
            for (int i = 0; i < size; i += 16)
            {
                // Come up with 16 indices (zero to the max for the bitsPerValue)
                for(int j = 0; j < 16; ++j)
                {
                    values[j] = (ushort)((i + j) % (1 << bitsPerValue));
                }

                // Compress into byte[]
                CompressValues(values, bitsPerValue, bucketSample, i);
            }

            Scenario[] scenarios = (Scenario[])Enum.GetValues(typeof(Scenario));
            for(int i = scenarios.Length - 1; i >= 0; --i)
            {
                TrySingleAndParallel(scenarios[i], bucketSample, bitsPerValue, size, iterations);
            }

            //long[] array = new long[size];
            //Benchmark.Compare("ArrayExtensions", 10, size, new string[] { "WriteArray", "ReadArray" },
            //    () => 
            //    {
            //        using (BinaryWriter w = new BinaryWriter(File.OpenWrite("Sample.bin")))
            //        { BinarySerializer.Write(w, array); }
            //        return "";
            //    },
            //    () =>
            //    {
            //        using (BinaryReader r = new BinaryReader(File.OpenRead("Sample.bin")))
            //        { array = BinarySerializer.ReadArray<long>(r, r.BaseStream.Length); }
            //        return "";
            //    }
            //    );

            //int sum;
            //Benchmark.Compare("Span Operations", iterations, size, new string[] { "Array For", "Array ForEach", "Span For", "Span ForEach" },
            //    () => { sum = 0; for (int i = 0; i < bucketSample.Length; ++i) { sum += bucketSample[i]; } return sum; },
            //    () => { sum = 0; foreach (int item in bucketSample) { sum += item; } return sum; },
            //    () => { sum = 0; for (int i = 0; i < bucketSpan.Length; ++i) { sum += bucketSpan[i]; } return sum; },
            //    () => { sum = 0; foreach (int item in bucketSpan) { sum += item; } return sum; }
            //);


            //ushort[] bigBucketSample = new ushort[size];
            //Span<ushort> bigSpan = new Span<ushort>(bigBucketSample);

            //for (int i = 0; i < size; ++i)
            //{
            //    bigBucketSample[i] = (ushort)((i & 1) == 0 ? (i & 65535) : (65535 - i & 65535));
            //}

            //IndexSet[] sets2 = new IndexSet[2];
            //for (int i = 0; i < sets2.Length; ++i)
            //{
            //    sets2[i] = new IndexSet(size / 2);
            //}

            //IndexSet[] sets4 = new IndexSet[4];
            //for (int i = 0; i < sets4.Length; ++i)
            //{
            //    sets4[i] = new IndexSet(size / 4);
            //}

            //Benchmark.Compare("IndexSet Operations", iterations, size, new string[] { /*"All", "None", "And", "Count", */"Where1b", "Where1b x2", "Where1b x4", "Where2b", "Where2b x2", "Where2b x4" /*, $"Where Parallel x{ParallelCount}"*/ },
            //    //() => set.All(size),
            //    //() => set.None(),
            //    //() => set.And(other),
            //    //() => set.Count,
            //    () => set.Where(BooleanOperator.Set, bucketSample, CompareOperator.GreaterThan, (byte)200),
            //    () => ParallelWhere(bucketSample, (byte)200, sets2),
            //    () => ParallelWhere(bucketSample, (byte)200, sets4),
            //    () => set.Where(BooleanOperator.Set, bigBucketSample, CompareOperator.GreaterThan, (ushort)65000),
            //    () => ParallelWhere(bigBucketSample, (ushort)65000, sets2),
            //    () => ParallelWhere(bigBucketSample, (ushort)65000, sets4)
            //);

            //set.None();
            //Benchmark.Compare("IndexSet Page", iterations, size, new string[] { "Page None" }, () => PageAll(set, page));

            //set.None();
            //for (int i = 0; i < set.Capacity; i += 50)
            //{
            //    set[i] = true;
            //}
            //Benchmark.Compare("IndexSet Page", iterations, size, new string[] { "Page 1/50" }, () => PageAll(set, page));

            //set.None();
            //for (int i = 0; i < set.Capacity; i += 10)
            //{
            //    set[i] = true;
            //}
            //Benchmark.Compare("IndexSet Page", iterations, size, new string[] { "Page 1/10" }, () => PageAll(set, page));

            //set.All(size);
            //Benchmark.Compare("IndexSet Page", iterations, size, new string[] { "Page All" }, () => PageAll(set, page));
        }

        private static void TrySingleAndParallel(Scenario scenario, byte[] array, int bitsPerValue, int rowCount, int iterations)
        {
            Benchmark.Compare(scenario.ToString(), iterations, rowCount, new string[] { "x1", "x2", "x4" },
                    () => V5.Test.Bandwidth(scenario, array, bitsPerValue, 0, rowCount),
                    () => ParallelBandwidth(scenario, array, bitsPerValue, rowCount, 2),
                    () => ParallelBandwidth(scenario, array, bitsPerValue, rowCount, 4)
                );
        }

        private static object ParallelBandwidth(Scenario scenario, byte[] array, int bitsPerValue, int rowCount, int parallelCount)
        {
            long result = 0;

            // Get an even multiple of 64 rows to parallelize across
            int segmentLength = (rowCount / parallelCount) & ~63;

            Parallel.For(0, parallelCount, (i) =>
            {
                int offset = i * segmentLength;
                int length = (i == parallelCount - 1 ? rowCount - offset : segmentLength);

                long part = Test.Bandwidth(scenario, array, bitsPerValue, offset, length);

                lock(array)
                {
                    result += part;
                }
            });

            return result;
        }

        private static object ParallelWhere<T>(T[] column, T value, IndexSet[] sets)
        {
            Parallel.For(0, sets.Length, (i) =>
            {
                int length = column.Length / sets.Length;
                int offset = i * length;

                sets[i].Where(BooleanOperator.And, column, CompareOperator.GreaterThan, value, offset, length);
            });

            int sum = 0;
            //for(int i = 0; i < sets.Length; ++i)
            //{
            //    sum += sets[i].Count;
            //}
            return sum;
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
            db.HttpStatus.Where(matches, BooleanOperator.And, CompareOperator.Equals, 404);
            db.ResponseBytes.Where(matches, BooleanOperator.And, CompareOperator.GreaterThan, 1000);

            return matches.Count;
        }

        private static int QueryV5(WebRequestDatabase db, IndexSet[] matches, Span<int>[] pages)
        {
            // Look up the buckets for HttpStatus 404 and ResponseBytes 1000
            
            // TODO: We don't need the post-scan for the ResponseBytes column, but we don't realize it.
            // The bucket boundaries are 999 and 1,001, so the 1,001 bucket, while not equal to the query value, is the first in-range value.

            bool isHttpStatusExact;
            int httpStatusBucket = db.HttpStatusBuckets.BucketForValue(404, out isHttpStatusExact);
            bool needHttpStatusPostScan = (isHttpStatusExact == false && db.HttpStatusBuckets.IsMultiValue[httpStatusBucket]);

            bool isResponseBytesExact;
            int responseBytesBucket = db.ResponseBytesBuckets.BucketForValue(1000, out isResponseBytesExact);
            bool needResponseBytesPostScan = (isResponseBytesExact == false && db.ResponseBytesBuckets.IsMultiValue[responseBytesBucket]);

            object locker = new object();
            int total = 0;

            Parallel.For(0, matches.Length, (i) =>
            {
                int length = db.Count / matches.Length;
                int offset = i * length;

                // Get matches in those bucket ranges and intersect them
                matches[i].Where(BooleanOperator.Set, db.HttpStatusBuckets.RowBucketIndex, CompareOperator.Equals, (byte)httpStatusBucket, offset, length);
                matches[i].Where(BooleanOperator.And, db.ResponseBytesBuckets.RowBucketIndex, CompareOperator.GreaterThan, (byte)responseBytesBucket, offset, length);

                // If no post-scans were required, return the bit vector count
                if (!needHttpStatusPostScan && !needResponseBytesPostScan)
                {
                    lock (locker)
                    {
                        total += matches[i].Count;
                    }

                    return;
                }

                // Otherwise, page through results and post-filter on required clauses
                // [NOTE: We should prefer to scan twice and only filter boundary bucket rows when there are many matches]
                int count = 0;
                int matchesBefore = 0;

                int next = 0;
                while (next != -1)
                {
                    next = matches[i].Page(ref pages[i], next);
                    matchesBefore += pages[i].Length;

                    if (needHttpStatusPostScan) db.HttpStatus.Where(ref pages[i], BooleanOperator.And, CompareOperator.Equals, 404, offset);
                    if (needResponseBytesPostScan) db.ResponseBytes.Where(ref pages[i], BooleanOperator.And, CompareOperator.GreaterThan, 1000, offset);

                    count += pages[i].Length;
                }

                lock(locker)
                {
                    total += count;
                }
            });

            // Return the final count
            return total;
        }

        private static void GenerateSampleCsv()
        {
            Random r = new Random(5);
            int rowCount = 1000 * 1000;

            using (ITabularWriter writer = TabularFactory.BuildWriter($"WebRequests.V0.{rowCount}.csv"))
            {
                WebRequestGenerator generator = new WebRequestGenerator(r, DateTime.UtcNow.AddSeconds(-rowCount / 250), 250);
                generator.WriteTo(writer, rowCount);
            }
        }
    }
}

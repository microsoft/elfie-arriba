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
using V5.Test.Collections;
using V5.Test.Performance;

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
            WherePerformance();
            IndexSetOperations();
            HashSetPerformance();
            BinarySerializationPerformance();
            PrototypeQueryPerformance();
            return;
        }

        static void PrototypeQueryPerformance()
        {
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

            using (Benchmarker b = new Benchmarker("HttpStatus = 404 AND ResponseBytes > 1000, 8M items"))
            {
                b.Measure("ManagedDirect", db.Count, () =>
                {
                    for (int i = 0; i < db.Count; ++i)
                    {
                        if (db.HttpStatus.Values[i] == 404 && db.ResponseBytes.Values[i] > 1000) set[i] = true;
                    }

                    return set;
                });

                b.Measure("ManagedColumn", db.Count, () =>
                {
                    db.HttpStatus.Where(set, BooleanOperator.Set, CompareOperator.Equals, 404);
                    db.ResponseBytes.Where(set, BooleanOperator.And, CompareOperator.GreaterThan, 1000);

                    return set;
                });

                b.Measure("V5", db.Count, () => QueryV5(db, set, page, 0, db.Count));
            }
        }

        private static object QueryV5(WebRequestDatabase db, IndexSet set, Span<int> page, int offset, int length)
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

            set.Where(BooleanOperator.Set, db.HttpStatusBuckets.RowBucketIndex, CompareOperator.Equals, (byte)httpStatusBucket, offset, length);
            set.Where(BooleanOperator.And, db.ResponseBytesBuckets.RowBucketIndex, CompareOperator.GreaterThan, (byte)responseBytesBucket, offset, length);

            // If no post-scan required, return the bit vector
            if (!needHttpStatusPostScan && !needResponseBytesPostScan) return set;

            // Otherwise, page through results and post-filter on required clauses
            // [NOTE: We should prefer to scan twice and only filter boundary bucket rows when there are many matches]
            int count = 0;

            int next = 0;
            while (next != -1)
            {
                next = set.Page(ref page, next);

                if (needHttpStatusPostScan) db.HttpStatus.Where(ref page, BooleanOperator.And, CompareOperator.Equals, 404);
                if (needResponseBytesPostScan) db.ResponseBytes.Where(ref page, BooleanOperator.And, CompareOperator.GreaterThan, 1000);

                count += page.Length;
            }

            // Return the final count
            return count;
        }

        static void BinarySerializationPerformance()
        {
            long[] array = new long[16 * 1024 * 1024];
            using (Benchmarker b = new Benchmarker("ArrayExtensions"))
            {
                b.Measure("WriteArray", array.Length, () =>
                {
                    using (BinaryWriter w = new BinaryWriter(File.OpenWrite("Sample.bin")))
                    { BinarySerializer.Write(w, array); }
                    return "";
                });

                b.Measure("ReadArray", array.Length, () =>
                {
                    using (BinaryReader r = new BinaryReader(File.OpenRead("Sample.bin")))
                    { array = BinarySerializer.ReadArray<long>(r, r.BaseStream.Length); }
                    return "";
                });
            }
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
            for (int itemIndex = 0; itemIndex < values.Length; ++itemIndex)
            {
                // Get the bits for the next item
                byte valueBits = (byte)(values[itemIndex] & valueMask);

                // Write them at the next position
                current |= (byte)(valueBits << bitIndex);

                // Calculate the start of the next value
                bitIndex += bitsPerValue;
                if (bitIndex >= 8)
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

        private static object HashSet(int[] values, out HashSet<int> output)
        {
            HashSet<int> set = null;

            var measurements = Memory.Measure(() =>
            {
                set = new HashSet<int>();

                for (int i = 0; i < values.Length; ++i)
                {
                    set.Add(values[i]);
                }

                bool containsAll = true;
                for (int i = 0; i < values.Length; ++i)
                {
                    containsAll &= set.Contains(values[i]);
                }

                return set;
            });

            output = set;
            return measurements.MemoryUsedBytes.SizeString();
        }

        private static object Search(int[] values, HashSet<int> set)
        {
            bool containsAll = true;
            bool missingExpected = true;
            for (int i = 0; i < values.Length; ++i)
            {
                containsAll &= set.Contains(values[i]);
                missingExpected &= !set.Contains(values[i] + 1);
            }
            return containsAll & missingExpected;
        }

        private static object HashSet(int[] values, out HashSet5<int> output)
        {
            HashSet5<int> set = null;

            var measurements = Memory.Measure(() =>
            {
                set = new HashSet5<int>(values.Length);

                for (int i = 0; i < values.Length; ++i)
                {
                    set.Add(values[i]);
                }

                bool containsAll = true;
                for (int i = 0; i < values.Length; ++i)
                {
                    containsAll &= set.Contains(values[i]);
                }

                return set;
            });

            output = set;
            return measurements.MemoryUsedBytes.SizeString();
        }

        private static object Search(int[] values, HashSet5<int> set)
        {
            bool containsAll = true;
            bool missingExpected = true;
            for (int i = 0; i < values.Length; ++i)
            {
                containsAll &= set.Contains(values[i]);
                missingExpected &= !set.Contains(values[i] + 1);
            }
            return containsAll & missingExpected;
        }

        static void HashSetPerformance()
        {
            int size = 16 * 1000 * 1000;

            Random r = new Random(5);
            int[] sample = new int[size];
            for (int i = 0; i < sample.Length; ++i)
            {
                // Random values with all high bits set
                //sample[i] = r.Next() << 1;

                // Random positive only - bad if high bit important for bucket
                sample[i] = r.Next();

                // Incrementing integers - great if choosing buckets with low bits
                //sample[i] = i;

                //sample[i] = i << 4;
            }

            HashSet5<int> set1 = null;
            HashSet<int> set2 = null;

            using (Benchmarker b = new Benchmarker($"HashSet5, {size / (1000 * 1000)}M"))
            {
                b.Measure("HashSet5.Add", size, () => HashSet(sample, out set1));
                b.Measure("HashSet.Add", size, () => HashSet(sample, out set2));

                b.Measure("HashSet5.Search", size, () => Search(sample, set1));
                b.Measure("HashSet.Search", size, () => Search(sample, set2));
            }

            Trace.WriteLine($"Mean: {set1.DistanceMean():n2}, Max Probe: {set1.MaxProbeLength}");
        }

        static void IndexSetOperations()
        {
            int size = 16 * 1000 * 1000;

            IndexSet set = new IndexSet(size);
            Span<int> page = new Span<int>(new int[4096]);

            IndexSet set50 = new IndexSet(size);
            for (int i = 0; i < set50.Capacity; i += 50) set50[i] = true;

            IndexSet set10 = new IndexSet(size);
            for (int i = 0; i < set10.Capacity; i += 10) set10[i] = true;

            using (Benchmarker b = new Benchmarker("IndexSet Operations"))
            {
                b.Measure("All", size, () => set.All(size));
                b.Measure("None", size, () => set.None());
                b.Measure("And", size, () => set.And(set50));
                b.Measure("Count", size, () => set.Count);

                set.None();
                b.Measure("Page None", size, () => IndexSetTests.PageAll(set, page));
                b.Measure("Page 1/50", size, () => IndexSetTests.PageAll(set50, page));
                b.Measure("Page 1/10", size, () => IndexSetTests.PageAll(set10, page));
            }
        }

        static void WherePerformance()
        {
            int size = 64 * 1000 * 1000;
            int bitsPerValue = 4;

            byte[] bucketSample = new byte[(size * bitsPerValue) / 8];
            Span<byte> bucketSpan = new Span<byte>(bucketSample);

            ushort[] values = new ushort[16];
            for (int i = 0; i < size; i += 16)
            {
                // Come up with 16 indices (zero to the max for the bitsPerValue)
                for (int j = 0; j < 16; ++j)
                {
                    values[j] = (ushort)((i + j) % (1 << bitsPerValue));
                }

                // Compress into byte[]
                CompressValues(values, bitsPerValue, bucketSample, i);
            }

            RawBitVector vector = new RawBitVector(size / 64);

            IndexSet set = new IndexSet(size);
            ushort[] bigBucketSample = new ushort[size];

            for (int i = 0; i < size; ++i)
            {
                bigBucketSample[i] = (ushort)((i & 1) == 0 ? (i & 65535) : (65535 - i & 65535));
            }

            using (Benchmarker b = new Benchmarker($"Basics Tests, {bitsPerValue} bit x {size / (1000 * 1000)}M"))
            {
                Scenario[] scenarios = (Scenario[])Enum.GetValues(typeof(Scenario));
                for (int i = scenarios.Length - 1; i >= 0; --i)
                {
                    b.MeasureParallel(scenarios[i].ToString(), size, (offset, length) => { Basics.Bandwidth(scenarios[i], bucketSample, bitsPerValue, offset, length, vector.Vector); return vector; });
                }

                b.MeasureParallel("Where1b", bucketSample.Length, (offset, length) => set.Where(BooleanOperator.Set, bucketSample, CompareOperator.LessThan, (byte)1, offset, length));
                b.MeasureParallel("Where2b", bigBucketSample.Length, (offset, length) => set.Where(BooleanOperator.Set, bigBucketSample, CompareOperator.LessThan, (ushort)1, offset, length));
            }
        }

        private class RawBitVector
        {
            public ulong[] Vector;

            public RawBitVector(int count)
            {
                this.Vector = new ulong[count];
            }

            public override string ToString()
            {
                return Basics.Count(Vector).ToString("n0");
            }
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

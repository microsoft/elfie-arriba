// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Model.Correctors;
using Arriba.Model.Expressions;
using Arriba.Structures;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  DistributionQuery returns a distribution of values for a given column sampled from a given query.
    ///  It creates equal-sized buckets between the 10th and 90th percentile values and returns
    ///  the bucket boundary values and the count of items within each.
    ///  
    ///  It is used to provide Inline Insights for "[Column] > " for appropriate-typed columns.
    /// </summary>
    public class DistributionQuery : IQuery<DataBlockResult>
    {
        private Array Buckets { get; set; }

        public int BucketCount { get; set; }
        public bool Inclusive { get; set; }
        public string Column { get; set; }
        public string TableName { get; set; }
        public IExpression Where { get; set; }
        public bool RequireMerge => false;

        public DistributionQuery() : base()
        {
            this.BucketCount = 7;
        }

        public DistributionQuery(string columnName, string where, bool inclusive)
        {
            this.Column = columnName;
            this.Where = QueryParser.Parse(where);
            this.Inclusive = inclusive;

            this.BucketCount = 7;
        }

        public void OnBeforeQuery(ITable table)
        {
            // Find approximate 10th and 90th percentile values for this query
            PercentilesQuery pq = new PercentilesQuery();
            pq.Column = this.Column;
            pq.TableName = this.TableName;
            pq.Where = this.Where;
            pq.Percentiles = new double[] { 0.10, 0.90 };

            DataBlockResult result = table.Query(pq);
            if (result.Values != null)
            {
                // Try to choose buckets if the 10th and 90th percentile values were returned [returns null for unsupported types]
                Bucketer bucketer = NativeContainer.CreateTypedInstance<Bucketer>(typeof(Bucketer<>), ((Table)table).GetColumnType(this.Column));
                this.Buckets = bucketer.GetBuckets(result.Values, this.Inclusive, this.BucketCount);
            }
        }

        public void Correct(ICorrector corrector)
        {
            if (corrector == null) throw new ArgumentNullException("corrector");
            this.Where = corrector.Correct(this.Where);
        }

        public DataBlockResult Compute(Partition p)
        {
            if (p == null) throw new ArgumentNullException("p");
            DataBlockResult result = new DataBlockResult(this);

            // Verify the column exists
            if (!p.ContainsColumn(this.Column))
            {
                result.Details.AddError(ExecutionDetails.ColumnDoesNotExist, this.Column);
                return result;
            }

            // Verify we were able to get percentile values
            if (this.Buckets == null)
            {
                result.Details.AddError(ExecutionDetails.ColumnDoesNotSupportOperator, "percentile", this.Column);
                return result;
            }

            // Find the set of items matching the where clause
            ShortSet whereSet = new ShortSet(p.Count);
            this.Where.TryEvaluate(p, whereSet, result.Details);

            IUntypedColumn column = p.Columns[this.Column];

            if (result.Details.Succeeded)
            {
                Bucketer bucketer = NativeContainer.CreateTypedInstance<Bucketer>(typeof(Bucketer<>), column.ColumnType);
                result.Values = bucketer.Bucket(column.InnerColumn, whereSet, this.Buckets, this.Inclusive);
                result.Total = whereSet.Count();
            }

            return result;
        }

        public DataBlockResult Merge(DataBlockResult[] partitionResults)
        {
            if (partitionResults == null) throw new ArgumentNullException("partitionResults");
            if (partitionResults.Length == 0) throw new ArgumentException("Length==0 not supported", "partitionResults");
            if (!partitionResults[0].Details.Succeeded) return partitionResults[0];

            DataBlockResult mergedResult = new DataBlockResult(this);
            mergedResult.Values = new DataBlock(new string[] { "Bucket", "RowCount" }, this.Buckets.Length + 1);

            // Copy the first partition values
            for (int i = 0; i < mergedResult.Values.RowCount; ++i)
            {
                mergedResult.Values[i, 0] = partitionResults[0].Values[i, 0];
                mergedResult.Values[i, 1] = partitionResults[0].Values[i, 1];
            }

            mergedResult.Details.Merge(partitionResults[0].Details);
            mergedResult.Total += partitionResults[0].Total;

            // Add the rest
            for (int partitionIndex = 1; partitionIndex < partitionResults.Length; ++partitionIndex)
            {
                DataBlockResult result = partitionResults[partitionIndex];

                for (int i = 0; i < mergedResult.Values.RowCount; ++i)
                {
                    mergedResult.Values[i, 1] = (int)mergedResult.Values[i, 1] + (int)result.Values[i, 1];
                }

                mergedResult.Details.Merge(result.Details);
                mergedResult.Total += result.Total;
            }

            return mergedResult;
        }

        internal abstract class Bucketer
        {
            public abstract Array GetBuckets(DataBlock percentileResults, bool inclusive, int bucketCount);
            public abstract DataBlock Bucket(IColumn column, ShortSet whereSet, Array buckets, bool inclusive);
        }

        internal class Bucketer<T> : Bucketer where T : IComparable<T>
        {
            public override DataBlock Bucket(IColumn c, ShortSet whereSet, Array b, bool inclusive)
            {
                IColumn<T> column = (IColumn<T>)c;
                T[] buckets = (T[])b;

                DataBlock result = new DataBlock(new string[] { "Bucket", "RowCount" }, buckets.Length + 1);

                int[] counts = new int[buckets.Length + 1];

                if (inclusive)
                {
                    for (int i = 0; i < column.Count; ++i)
                    {
                        ushort lid = (ushort)i;
                        if (whereSet.Contains(lid))
                        {
                            T value = column[lid];

                            int j = 0;
                            for (; j < buckets.Length; ++j)
                            {
                                if (value.CompareTo(buckets[j]) <= 0)
                                {
                                    counts[j]++;
                                    break;
                                }
                            }

                            if (j == buckets.Length) counts[buckets.Length]++;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < column.Count; ++i)
                    {
                        ushort lid = (ushort)i;
                        if (whereSet.Contains(lid))
                        {
                            T value = column[lid];

                            int j = 0;
                            for (; j < buckets.Length; ++j)
                            {
                                if (value.CompareTo(buckets[j]) < 0)
                                {
                                    counts[j]++;
                                    break;
                                }
                            }

                            if (j == buckets.Length) counts[buckets.Length]++;
                        }
                    }
                }

                for (int i = 0; i < buckets.Length; ++i)
                {
                    result[i, 0] = buckets[i];
                    result[i, 1] = counts[i];
                }

                result[buckets.Length, 0] = null;
                result[buckets.Length, 1] = counts[buckets.Length];

                return result;
            }

            public override Array GetBuckets(DataBlock percentileResults, bool inclusive, int bucketCount)
            {
                T[] buckets = new T[bucketCount];

                buckets[0] = (T)percentileResults[0, 1];
                buckets[bucketCount - 1] = (T)percentileResults[percentileResults.RowCount - 1, 1];

                if (buckets[0].Equals(buckets[bucketCount - 1])) return new T[1] { buckets[0] };

                // Cast to a specific type at the array level to avoid per item casting
                if (typeof(T).Equals(typeof(long)))
                {
                    GetBucketsLong((long[])(Array)buckets);
                }
                else if (typeof(T).Equals(typeof(int)))
                {
                    GetBucketsInt((int[])(Array)buckets);
                }
                else if (typeof(T).Equals(typeof(short)))
                {
                    GetBucketsShort((short[])(Array)buckets);
                }
                else if (typeof(T).Equals(typeof(byte)))
                {
                    GetBucketsByte((byte[])(Array)buckets);
                }
                else if (typeof(T).Equals(typeof(ulong)))
                {
                    GetBucketsULong((ulong[])(Array)buckets);
                }
                else if (typeof(T).Equals(typeof(uint)))
                {
                    GetBucketsUint((uint[])(Array)buckets);
                }
                else if (typeof(T).Equals(typeof(ushort)))
                {
                    GetBucketsUShort((ushort[])(Array)buckets);
                }
                else if (typeof(T).Equals(typeof(double)))
                {
                    GetBucketsDouble((double[])(Array)buckets);
                }
                else if (typeof(T).Equals(typeof(float)))
                {
                    GetBucketsFloat((float[])(Array)buckets);
                }
                else if (typeof(T).Equals(typeof(DateTime)))
                {
                    GetBucketsDateTime((DateTime[])(Array)buckets);
                }
                else if (typeof(T).Equals(typeof(TimeSpan)))
                {
                    GetBucketsTimeSpan((TimeSpan[])(Array)buckets);
                }
                else
                {
                    // For unsupported types, return null.
                    // Compute will write an "unsupported operation" error on ExecutionDetails in this case
                    return null;
                }

                return buckets;
            }

            private static TimeSpan[] GetBucketsTimeSpan(TimeSpan[] buckets)
            {
                // Find the range and interval between buckets
                TimeSpan range = buckets[buckets.Length - 1] - buckets[0];
                TimeSpan interval = TimeSpan.FromTicks(range.Ticks / (buckets.Length - 1));

                // Round the buckets
                buckets[0] = Round(buckets[0], interval);
                buckets[buckets.Length - 1] = Round(buckets[buckets.Length - 1], interval);
                interval = Round(interval, interval);

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }

            private static DateTime[] GetBucketsDateTime(DateTime[] buckets)
            {
                // Find the range and interval between buckets
                TimeSpan range = buckets[buckets.Length - 1] - buckets[0];
                TimeSpan interval = TimeSpan.FromTicks(range.Ticks / (buckets.Length - 1));

                // Round the buckets
                buckets[0] = Round(buckets[0], interval);
                buckets[buckets.Length - 1] = Round(buckets[buckets.Length - 1], interval);
                interval = Round(interval, interval);

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }

            private static byte[] GetBucketsByte(byte[] buckets)
            {
                // Find the range and interval between buckets
                byte range = (byte)(buckets[buckets.Length - 1] - buckets[0]);
                byte interval = (byte)(range / (byte)(buckets.Length - 1));

                // Round the buckets
                buckets[0] = (byte)Round(buckets[0]);
                buckets[buckets.Length - 1] = (byte)Round(buckets[buckets.Length - 1]);
                interval = (byte)Round(interval);

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = (byte)(buckets[i - 1] + interval);
                }

                return buckets;
            }

            private static short[] GetBucketsShort(short[] buckets)
            {
                // Find the range and interval between buckets
                short range = (short)(buckets[buckets.Length - 1] - buckets[0]);
                short interval = (short)(range / (short)(buckets.Length - 1));

                // Round the buckets
                buckets[0] = (short)Round(buckets[0]);
                buckets[buckets.Length - 1] = (short)Round(buckets[buckets.Length - 1]);
                interval = (short)Round(interval);

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = (short)(buckets[i - 1] + interval);
                }

                return buckets;
            }

            private static ushort[] GetBucketsUShort(ushort[] buckets)
            {
                // Find the range and interval between buckets
                ushort range = (ushort)(buckets[buckets.Length - 1] - buckets[0]);
                ushort interval = (ushort)(range / (ushort)(buckets.Length - 1));

                // Round the buckets
                buckets[0] = (ushort)Round(buckets[0]);
                buckets[buckets.Length - 1] = (ushort)Round(buckets[buckets.Length - 1]);
                interval = (ushort)Round(interval);

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = (ushort)(buckets[i - 1] + interval);
                }

                return buckets;
            }

            private static int[] GetBucketsInt(int[] buckets)
            {
                // Find the range and interval between buckets
                int range = buckets[buckets.Length - 1] - buckets[0];
                int interval = range / (buckets.Length - 1);

                // Round the buckets
                buckets[0] = (int)Round(buckets[0]);
                buckets[buckets.Length - 1] = (int)Round(buckets[buckets.Length - 1]);
                interval = (int)Round(interval);

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }

            private static uint[] GetBucketsUint(uint[] buckets)
            {
                // Find the range and interval between buckets
                uint range = buckets[buckets.Length - 1] - buckets[0];
                uint interval = range / (uint)(buckets.Length - 1);

                // Round the buckets
                buckets[0] = (uint)Round(buckets[0]);
                buckets[buckets.Length - 1] = (uint)Round(buckets[buckets.Length - 1]);
                interval = (uint)Round(interval);

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }

            private static long[] GetBucketsLong(long[] buckets)
            {
                // Find the range and interval between buckets
                long range = buckets[buckets.Length - 1] - buckets[0];
                long interval = range / (long)(buckets.Length - 1);

                // Round the buckets
                buckets[0] = Round(buckets[0]);
                buckets[buckets.Length - 1] = Round(buckets[buckets.Length - 1]);
                interval = Round(interval);

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }

            private static ulong[] GetBucketsULong(ulong[] buckets)
            {
                // Find the range and interval between buckets
                ulong range = buckets[buckets.Length - 1] - buckets[0];
                ulong interval = range / (ulong)(buckets.Length - 1);

                // Round the buckets
                buckets[0] = (ulong)Round(buckets[0]);
                buckets[buckets.Length - 1] = (ulong)Round(buckets[buckets.Length - 1]);
                interval = (ulong)Round(interval);

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }

            private static float[] GetBucketsFloat(float[] buckets)
            {
                // Find the range and interval between buckets
                float range = buckets[buckets.Length - 1] - buckets[0];
                float interval = range / (float)(buckets.Length - 1);

                // Round the buckets
                buckets[0] = (float)Round(buckets[0]);
                buckets[buckets.Length - 1] = (float)Round(buckets[buckets.Length - 1]);
                interval = (float)Round(interval);

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }

            private static double[] GetBucketsDouble(double[] buckets)
            {
                // Find the range and interval between buckets
                double range = buckets[buckets.Length - 1] - buckets[0];
                double interval = range / (double)(buckets.Length - 1);

                // Round the buckets
                buckets[0] = (double)Round(buckets[0]);
                buckets[buckets.Length - 1] = (double)Round(buckets[buckets.Length - 1]);
                interval = (double)Round(interval);

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }

            #region Round
            internal static DateTime Round(DateTime value, TimeSpan interval)
            {
                if (interval.TotalDays >= 1)
                {
                    value = value.Date;
                }
                else if (interval.TotalHours >= 1)
                {
                    value = value.Date.AddHours(Math.Round(value.TimeOfDay.TotalHours));
                }
                else if (interval.TotalMinutes >= 1)
                {
                    value = value.Date.AddMinutes(Math.Round(value.TimeOfDay.TotalMinutes));
                }

                return value;
            }

            internal static TimeSpan Round(TimeSpan value, TimeSpan interval)
            {
                if (interval.TotalDays >= 1)
                {
                    value = TimeSpan.FromDays(Math.Round(value.TotalDays));
                }
                else if (interval.TotalHours >= 1)
                {
                    value = TimeSpan.FromHours(Math.Round(value.TotalHours));
                }
                else if (interval.TotalMinutes >= 1)
                {
                    value = TimeSpan.FromMinutes(Math.Round(value.TotalMinutes));
                }

                return value;
            }

            internal static long Round(long value)
            {
                long scale = 1;
                while (value > scale * 1000)
                {
                    scale *= 10;
                }

                if (scale > 1)
                {
                    value -= value % scale;
                }

                return value;
            }

            internal static ulong Round(ulong value)
            {
                ulong scale = 1;
                while (value > scale * 1000)
                {
                    scale *= 10;
                }

                if (scale > 1)
                {
                    value -= value % scale;
                }

                return value;
            }

            internal static double Round(double value)
            {
                double scale = 1;
                while (value > scale * 1000)
                {
                    scale *= 10;
                }

                if (scale > 1)
                {
                    value -= value % scale;
                }
                else if (value > 100)
                {
                    value = Math.Round(value, 0);
                }
                else if (value > 10)
                {
                    value = Math.Round(value, 1);
                }
                else if (value > 1)
                {
                    value = Math.Round(value, 2);
                }
                else
                {
                    value = Math.Round(value, 3);
                }

                return value;
            }
            #endregion
        }
    }
}

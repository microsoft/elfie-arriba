// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Model.Correctors;
using Arriba.Model.Expressions;
using Arriba.Structures;
using Arriba.Extensions;

namespace Arriba.Model.Query
{
    public class DistributionQuery : IQuery<DataBlockResult>
    {
        private Array Buckets { get; set; }

        public bool Inclusive { get; set; }
        public string Column { get; set; }
        public string TableName { get; set; }
        public IExpression Where { get; set; }
        public bool RequireMerge => false;

        public DistributionQuery() : base()
        { }

        public DistributionQuery(string columnName, string where, bool inclusive)
        {
            this.Column = columnName;
            this.Where = QueryParser.Parse(where);
            this.Inclusive = inclusive;
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
            if (result.Details.Succeeded)
            {
                Bucketer bucketer = NativeContainer.CreateTypedInstance<Bucketer>(typeof(Bucketer<>), ((Table)table).GetColumnType(this.Column));
                this.Buckets = bucketer.GetBuckets(result.Values);
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
            if(this.Buckets == null)
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
                    mergedResult.Values[i, 1] = (ulong)mergedResult.Values[i, 1] + (ulong)result.Values[i, 1];
                }

                mergedResult.Details.Merge(result.Details);
                mergedResult.Total += result.Total;
            }

            return mergedResult;
        }

        private abstract class Bucketer
        {
            public abstract Array GetBuckets(DataBlock percentileResults);
            public abstract DataBlock Bucket(IColumn column, ShortSet whereSet, Array buckets, bool inclusive);
        }

        private class Bucketer<T> : Bucketer where T : IComparable<T>
        {
            public override DataBlock Bucket(IColumn c, ShortSet whereSet, Array b, bool inclusive)
            {
                IColumn<T> column = (IColumn<T>)c;
                T[] buckets = (T[])b;

                DataBlock result = new DataBlock(new string[] { "Bucket", "RowCount" }, buckets.Length + 1);

                ulong[] counts = new ulong[buckets.Length + 1];

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

            public override Array GetBuckets(DataBlock percentileResults)
            {
                T[] buckets = new T[7];

                buckets[0] = (T)percentileResults[0, 1];
                buckets[6] = (T)percentileResults[1, 1];

                // Cast to a specific type at the array level to avoid per item casting
                if (typeof(T).Equals(typeof(long)))
                {
                    GetBucketsLong((long[])(Array)buckets);
                }
                else if (typeof(T).Equals(typeof(int)))
                {
                    GetBucketsInt((int[])(Array)buckets);
                }
                //else if (typeof(T).Equals(typeof(short)))
                //{
                //    GetBucketsShort((short[])(Array)buckets);
                //}
                //else if (typeof(T).Equals(typeof(byte)))
                //{
                //    GetBucketsByte((byte[])(Array)buckets);
                //}
                //else if (typeof(T).Equals(typeof(ulong)))
                //{
                //    GetBucketsULong((ulong[])(Array)buckets);
                //}
                else if (typeof(T).Equals(typeof(uint)))
                {
                    GetBucketsUint((uint[])(Array)buckets);
                }
                //else if (typeof(T).Equals(typeof(ushort)))
                //{
                //    GetBucketsUShort((ushort[])(Array)buckets);
                //}
                //else if (typeof(T).Equals(typeof(double)))
                //{
                //    GetBucketsDouble((double[])(Array)buckets);
                //}
                //else if (typeof(T).Equals(typeof(float)))
                //{
                //    GetBucketsFloat((float[])(Array)buckets);
                //}
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
                    throw new NotImplementedException(StringExtensions.Format("{0} is unable to aggregate type {1}.", this.GetType().Name, buckets.GetValue(0).GetType().Name));
                }

                return buckets;
            }

            private static TimeSpan[] GetBucketsTimeSpan(TimeSpan[] buckets)
            {
                // Find the range and interval between buckets
                TimeSpan range = buckets[buckets.Length - 1] - buckets[0];
                TimeSpan interval = TimeSpan.FromTicks(range.Ticks / buckets.Length);

                // Round the buckets
                if(interval.TotalDays >= buckets.Length)
                {
                    buckets[0] = TimeSpan.FromDays(Math.Round(buckets[0].TotalDays));
                    buckets[buckets.Length - 1] = TimeSpan.FromDays(Math.Round(buckets[buckets.Length - 1].TotalDays));
                    interval = TimeSpan.FromDays(Math.Round(interval.TotalDays));
                }
                else if(interval.TotalHours >= buckets.Length)
                {
                    buckets[0] = TimeSpan.FromHours(Math.Round(buckets[0].TotalHours));
                    buckets[buckets.Length - 1] = TimeSpan.FromHours(Math.Round(buckets[buckets.Length - 1].TotalHours));
                    interval = TimeSpan.FromHours(Math.Round(interval.TotalHours));
                }
                else
                {
                    buckets[0] = TimeSpan.FromMinutes(Math.Round(buckets[0].TotalMinutes));
                    buckets[buckets.Length - 1] = TimeSpan.FromMinutes(Math.Round(buckets[buckets.Length - 1].TotalMinutes));
                    interval = TimeSpan.FromMinutes(Math.Round(interval.TotalMinutes));
                }

                // Set the buckets
                for(int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }

            private static DateTime[] GetBucketsDateTime(DateTime[] buckets)
            {
                // Find the range and interval between buckets
                TimeSpan range = buckets[buckets.Length - 1] - buckets[0];
                TimeSpan interval = TimeSpan.FromTicks(range.Ticks / buckets.Length);

                // Round the buckets
                if (interval.TotalDays >= buckets.Length)
                {
                    buckets[0] = buckets[0].Date;
                    buckets[buckets.Length - 1] = buckets[buckets.Length - 1].Date;
                    interval = TimeSpan.FromDays(Math.Round(interval.TotalDays));
                }
                else if (interval.TotalHours >= buckets.Length)
                {
                    buckets[0] = buckets[0].Date.AddHours(Math.Round(buckets[0].TimeOfDay.TotalHours));
                    buckets[buckets.Length - 1] = buckets[buckets.Length - 1].Date.AddHours(Math.Round(buckets[buckets.Length - 1].TimeOfDay.TotalHours));
                    interval = TimeSpan.FromDays(Math.Round(interval.TotalDays));
                }
                else
                {
                    buckets[0] = buckets[0].Date.AddMinutes(Math.Round(buckets[0].TimeOfDay.TotalMinutes));
                    buckets[buckets.Length - 1] = buckets[buckets.Length - 1].Date.AddMinutes(Math.Round(buckets[buckets.Length - 1].TimeOfDay.TotalMinutes));
                    interval = TimeSpan.FromHours(Math.Round(interval.TotalHours));
                    interval = TimeSpan.FromMinutes(Math.Round(interval.TotalMinutes));
                }

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }

            private static int[] GetBucketsInt(int[] buckets)
            {
                // Find the range and interval between buckets
                int range = buckets[buckets.Length - 1] - buckets[0];
                int interval = range / buckets.Length;

                // Round the buckets
                int scale = 10;
                while(interval > scale * 10)
                {
                    scale *= 10;
                }

                if(scale > 10)
                {
                    buckets[0] -= buckets[0] % scale;
                    buckets[buckets.Length - 1] -= buckets[buckets.Length - 1] % scale;
                    interval -= interval % scale;
                }

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }

            private static uint[] GetBucketsUint(uint[] buckets)
            {
                // Find the range and uinterval between buckets
                uint range = buckets[buckets.Length - 1] - buckets[0];
                uint interval = range / (uint)buckets.Length;

                // Round the buckets
                uint scale = 10;
                while (interval > scale * 10)
                {
                    scale *= 10;
                }

                if (scale > 10)
                {
                    buckets[0] -= buckets[0] % scale;
                    buckets[buckets.Length - 1] -= buckets[buckets.Length - 1] % scale;
                    interval -= interval % scale;
                }

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }

            private static long[] GetBucketsLong(long[] buckets)
            {
                // Find the range and longerval between buckets
                long range = buckets[buckets.Length - 1] - buckets[0];
                long interval = range / (long)buckets.Length;

                // Round the buckets
                long scale = 10;
                while (interval > scale * 10)
                {
                    scale *= 10;
                }

                if (scale > 10)
                {
                    buckets[0] -= buckets[0] % scale;
                    buckets[buckets.Length - 1] -= buckets[buckets.Length - 1] % scale;
                    interval -= interval % scale;
                }

                // Set the buckets
                for (int i = 1; i < buckets.Length - 1; ++i)
                {
                    buckets[i] = buckets[i - 1] + interval;
                }

                return buckets;
            }
        }
    }
}

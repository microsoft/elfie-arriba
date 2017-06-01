// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Model.Correctors;
using Arriba.Model.Expressions;
using Arriba.Structures;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  PercentilesQuery returns the requested percentiles for a query by sampling values
    ///  and getting the medians across partitions.
    /// </summary>
    public class PercentilesQuery : IQuery<DataBlockResult>
    {
        public string Column { get; set; }
        public double[] Percentiles { get; set; }

        public string TableName { get; set; }
        public IExpression Where { get; set; }
        public bool RequireMerge => false;

        public PercentilesQuery() : base()
        { }

        public PercentilesQuery(string column, string where, double[] percentiles)
        {
            this.Column = column;
            this.Where = QueryParser.Parse(where);
            this.Percentiles = percentiles;
        }

        public void OnBeforeQuery(ITable table)
        { }

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

            // Find the set of items matching the where clause
            ShortSet whereSet = new ShortSet(p.Count);
            this.Where.TryEvaluate(p, whereSet, result.Details);

            int matchCount = whereSet.Count();
            result.Total = matchCount;

            if (result.Details.Succeeded && matchCount > 0)
            {
                // Get sample values
                object[] samples = GetColumnSamples(p, p.Columns[this.Column], whereSet, matchCount);

                // Sort them
                Array.Sort(samples);

                // Record the values corresponding to those percentiles
                result.Values = new DataBlock(new string[] { "Percentiles", "Values" }, this.Percentiles.Length);

                for (int i = 0; i < this.Percentiles.Length; ++i)
                {
                    double percentile = this.Percentiles[i];
                    int sampleRow = (int)(percentile * samples.Length) - 1;
                    if (sampleRow < 0) sampleRow = 0;
                    if (sampleRow >= samples.Length) sampleRow = samples.Length - 1;

                    result.Values[i, 0] = percentile;
                    result.Values[i, 1] = samples[sampleRow];
                }
            }

            return result;
        }

        private static object[] GetColumnSamples(Partition p, IUntypedColumn column, ShortSet whereSet, int matchCount)
        {
            // Get up to 500 samples
            int countToGet = Math.Min(500, matchCount);

            object[] samples = new object[countToGet];

            Random r = new Random();
            int sampleCount = 0;
            int countLeft = matchCount;
            for (int i = 0; i < p.Count && sampleCount < countToGet; ++i)
            {
                ushort lid = (ushort)i;

                if (whereSet.Contains(lid))
                {
                    double excludeChance = r.NextDouble();
                    int countNeeded = countToGet - sampleCount;

                    // == if((countNeeded / countLeft) > excludeChance)
                    if (countNeeded > (excludeChance * countLeft))
                    {
                        samples[sampleCount] = column[lid];
                        sampleCount++;
                    }

                    countLeft--;
                }
            }

            return samples;
        }

        public DataBlockResult Merge(DataBlockResult[] partitionResults)
        {
            if (partitionResults == null) throw new ArgumentNullException("partitionResults");
            if (partitionResults.Length == 0) throw new ArgumentException("Length==0 not supported", "partitionResults");
            if (!partitionResults[0].Details.Succeeded) return partitionResults[0];

            DataBlockResult mergedResult = new DataBlockResult(this);

            // Merge totals and details
            for (int partitionIndex = 0; partitionIndex < partitionResults.Length; ++partitionIndex)
            {
                DataBlockResult result = partitionResults[partitionIndex];
                mergedResult.Details.Merge(result.Details);
                mergedResult.Total += result.Total;
            }

            if (mergedResult.Details.Succeeded && mergedResult.Total > 0)
            {
                mergedResult.Values = new DataBlock(new string[] { "Percentiles", "Values" }, this.Percentiles.Length);

                // Find the median for each percentile across partitions
                object[] valuesPerPartition = new object[partitionResults.Length];
                for (int i = 0; i < this.Percentiles.Length; ++i)
                {
                    for (int partitionIndex = 0; partitionIndex < partitionResults.Length; ++partitionIndex)
                    {
                        if (partitionResults[partitionIndex].Values != null)
                        {
                            valuesPerPartition[partitionIndex] = partitionResults[partitionIndex].Values[i, 1];
                        }
                    }

                    Array.Sort(valuesPerPartition);

                    mergedResult.Values[i, 0] = this.Percentiles[i];
                    mergedResult.Values[i, 1] = valuesPerPartition[valuesPerPartition.Length / 2];
                }
            }

            return mergedResult;
        }
    }
}

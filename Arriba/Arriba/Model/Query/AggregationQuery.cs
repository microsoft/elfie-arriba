// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Arriba.Extensions;
using Arriba.Model.Aggregations;
using Arriba.Model.Correctors;
using Arriba.Model.Expressions;
using Arriba.Structures;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  AggregrationQuery represents a Grid, Grouped, or simple Aggregation
    ///  sent to Arriba. 
    ///  
    ///  It handles queries like:
    ///    SELECT COUNT(*) FROM Bugs WHERE ...
    ///    SELECT SUM(WorkRemaining) FROM Bugs GROUP BY AssignedTo
    ///    SELECT SUM(HitCount) FROM WatsonBuckets GROUP BY MainModule, Month(CreatedDate)
    ///    SELECT SUM(InstallCount) FROM Apps DIMENSION Base10(RatingsCount), DIMENSION WHERE EntryPoint = "JavaScript", WHERE EntryPoint = "Managed", WHERE EntryPoint = "Native"
    /// </summary>
    public class AggregationQuery : IQuery<AggregationResult>
    {
        public IAggregator Aggregator { get; set; }

        /// <summary>
        ///  The set of columns, if any, passed to the Aggregation function.
        ///  [Ex: SUM(WorkRemaining) has one column, "WorkRemaining", passed.
        /// </summary>
        public string[] AggregationColumns { get; set; }

        /// <summary>
        ///  TableName is the name of the table to query.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        ///  Where is the IExpression restricting the overall results
        ///  across all groups.
        /// </summary>
        public IExpression Where { get; set; }

        /// <summary>
        ///  Dimensions is the set of zero or more dimensions across which
        ///  to compute the aggregate. Provide zero dimensions to get a single
        ///  aggregate value for every item matching the base Where clause.
        ///  Provide additional dimensions to compute multiple aggregations,
        ///  as GROUP BY or GRID style aggregations.
        /// </summary>
        public IList<AggregationDimension> Dimensions { get; set; }

        public AggregationQuery()
        {
            this.Where = new AllExpression();
            this.Dimensions = new List<AggregationDimension>();
        }

        public AggregationQuery(string aggregationFunction, IEnumerable<string> columns, string where)
            : this()
        {
            this.Aggregator = BuildAggregator(aggregationFunction);
            this.AggregationColumns = columns == null ? null : new List<string>(columns).ToArray();
            this.Where = QueryParser.Parse(where);
        }

        public AggregationQuery(AggregationQuery other)
        {
            this.Aggregator = other.Aggregator;
            this.AggregationColumns = other.AggregationColumns;
            this.TableName = other.TableName;
            this.Where = other.Where;
            this.Dimensions = other.Dimensions;
        }

        public static IAggregator BuildAggregator(string aggregationFunction)
        {
            aggregationFunction = aggregationFunction.ToLowerInvariant();

            switch (aggregationFunction)
            {
                case "count":
                    return new CountAggregator();
                case "sum":
                    return new SumAggregator();
                case "min":
                    return new MinAggregator();
                case "max":
                    return new MaxAggregator();
                default:
                    throw new NotImplementedException(StringExtensions.Format("Aggregation '{0}' is not implemented in Arriba.", aggregationFunction));
            }
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (this.AggregationColumns != null && this.AggregationColumns.Length > 0)
            {
                result.AppendLine(StringExtensions.Format("{0}({1})", this.Aggregator, String.Join(", ", this.AggregationColumns)));
            }
            else
            {
                result.AppendLine(this.Aggregator.ToString());
            }

            result.AppendLine(StringExtensions.Format("\tFROM {0}", this.TableName));
            result.AppendLine(StringExtensions.Format("\tWHERE {0}", this.Where));

            if (this.Dimensions != null && this.Dimensions.Count > 0)
            {
                foreach (AggregationDimension dimension in this.Dimensions)
                {
                    result.Append("DIMENSION");
                    if (!String.IsNullOrEmpty(dimension.Name)) result.Append(StringExtensions.Format(" \"{0}\"", dimension.Name));
                    result.AppendLine();

                    foreach (IExpression dWhere in dimension.GroupByWhere)
                    {
                        result.AppendLine(StringExtensions.Format("\t\tWHERE {0}", dWhere));
                    }
                }
            }

            return result.ToString();
        }

        public virtual void OnBeforeQuery(ITable table)
        {
            this.Where = this.Where ?? new AllExpression();

            // Allow dimensions to do pre-query work
            foreach (AggregationDimension dimension in this.Dimensions)
            {
                dimension.OnBeforeQuery(table, this.Where);
            }
        }

        public bool RequireMerge
        {
            get { return this.Aggregator.RequireMerge; }
        }

        public void Correct(ICorrector corrector)
        {
            if (corrector == null) throw new ArgumentNullException("corrector");

            this.Where = corrector.Correct(this.Where);

            foreach (AggregationDimension dimension in this.Dimensions)
            {
                for (int i = 0; i < dimension.GroupByWhere.Count; ++i)
                {
                    dimension.GroupByWhere[i] = corrector.Correct(dimension.GroupByWhere[i]);
                }
            }
        }

        public AggregationResult Compute(Partition p)
        {
            if (p == null) throw new ArgumentNullException("p");

            Stopwatch w = Stopwatch.StartNew();
            AggregationResult result = new AggregationResult(this);
            result.AggregationContext = this.Aggregator.CreateContext();

            // Get any columns passed to the aggregation function
            IUntypedColumn[] columns = null;
            if (this.AggregationColumns != null)
            {
                columns = new IUntypedColumn[this.AggregationColumns.Length];

                for (int i = 0; i < this.AggregationColumns.Length; ++i)
                {
                    string columnName = this.AggregationColumns[i];

                    if (!p.Columns.TryGetValue(columnName, out columns[i]))
                    {
                        result.Details.AddError(ExecutionDetails.ColumnDoesNotExist, columnName);
                        return result;
                    }
                }
            }

            // Find the number of dimensions and number of "cells" for which we'll aggregate
            List<string> resultBlockColumns = new List<string>();
            int rowCount = 1;

            for (int i = 0; i < this.Dimensions.Count; ++i)
            {
                AggregationDimension dimension = this.Dimensions[i];

                if (!String.IsNullOrEmpty(dimension.Name))
                {
                    resultBlockColumns.Add(dimension.Name);
                }
                else
                {
                    resultBlockColumns.Add(StringExtensions.Format("Dimension {0}", i + 1));
                }

                rowCount *= (dimension.GroupByWhere.Count + 1);
            }

            resultBlockColumns.Add("Aggregate");

            // Create the DataBlock to hold the final results
            result.Values = new DataBlock(resultBlockColumns, rowCount);

            // Find the set of items in the base query
            ShortSet baseWhereSet = new ShortSet(p.Count);
            this.Where.TryEvaluate(p, baseWhereSet, result.Details);
            result.Total = baseWhereSet.Count();

            // If this is only one dimension, use only one ShortSet and aggregate as we go
            if (this.Dimensions.Count == 1)
            {
                AggregationDimension dimension = this.Dimensions[0];
                ShortSet setForDimension = new ShortSet(p.Count);
                int nextBlockRow = 0;

                foreach (IExpression dimensionValue in dimension.GroupByWhere)
                {
                    // Get the set for this value intersected with the base set
                    setForDimension.Clear();
                    dimensionValue.TryEvaluate(p, setForDimension, result.Details);
                    setForDimension.And(baseWhereSet);

                    // Compute and store the aggregate value
                    if (!setForDimension.IsEmpty())
                    {
                        result.Values[nextBlockRow, 1] = this.Aggregator.Aggregate(result.AggregationContext, setForDimension, columns);
                    }

                    nextBlockRow++;
                }

                // Add the total
                result.Values[nextBlockRow, 1] = this.Aggregator.Aggregate(result.AggregationContext, baseWhereSet, columns);
            }
            else
            {
                // Compute the set of items actually matching each dimension-value
                List<List<Tuple<IExpression, ShortSet>>> allDimensionValueSets = new List<List<Tuple<IExpression, ShortSet>>>();
                foreach (AggregationDimension dimension in this.Dimensions)
                {
                    List<Tuple<IExpression, ShortSet>> dimensionSet = new List<Tuple<IExpression, ShortSet>>();

                    // Add one item for each value in this dimension
                    foreach (IExpression dimensionValue in dimension.GroupByWhere)
                    {
                        ShortSet setForDimensionValue = new ShortSet(p.Count);
                        dimensionValue.TryEvaluate(p, setForDimensionValue, result.Details);

                        dimensionSet.Add(new Tuple<IExpression, ShortSet>(dimensionValue, setForDimensionValue));
                    }

                    // Add one 'Total row' item
                    dimensionSet.Add(new Tuple<IExpression, ShortSet>(new AllExpression(), baseWhereSet));

                    allDimensionValueSets.Add(dimensionSet);
                }

                // Run the aggregator over the items
                AggregateAllDimensionsFlat(result.AggregationContext, result.Values, p.Count, baseWhereSet, allDimensionValueSets, columns, this.Aggregator);
            }

            // Add the dimension names to the result if this is the only partition; otherwise, merge will add it
            if (p.Mask.Equals(PartitionMask.All))
            {
                AddDimensionsToBlock(result.Values);
            }

            // Capture timing and return
            result.Runtime = w.Elapsed;
            return result;
        }

        private static void AggregateAllDimensionsFlat(object aggregationContext, DataBlock block, ushort itemCount, ShortSet baseWhereSet, List<List<Tuple<IExpression, ShortSet>>> allDimensionValueSets, IUntypedColumn[] columns, IAggregator aggregator)
        {
            int nextBlockRow = 0;

            int dimensionCount = allDimensionValueSets.Count;

            int currentDimension = 0;
            int[] nextIndexPerDimension = new int[dimensionCount];
            ShortSet[] setsPerDimension = new ShortSet[dimensionCount + 1];

            setsPerDimension[0] = baseWhereSet;
            for (int i = 1; i < setsPerDimension.Length; ++i)
            {
                setsPerDimension[i] = new ShortSet(itemCount);
            }

            object[] aggregationResults = new object[block.RowCount];

            while (currentDimension >= 0)
            {
                if (currentDimension == dimensionCount)
                {
                    // Leaf: Compute Aggregate, then ask for next combination
                    aggregationResults[nextBlockRow] = aggregator.Aggregate(aggregationContext, setsPerDimension[currentDimension], columns);
                    nextBlockRow++;
                    currentDimension--;
                }
                else if (nextIndexPerDimension[currentDimension] < allDimensionValueSets[currentDimension].Count)
                {
                    // Non-leaf, more values: Get next value and combine
                    Tuple<IExpression, ShortSet> dimensionValue = allDimensionValueSets[currentDimension][nextIndexPerDimension[currentDimension]];

                    ShortSet current = setsPerDimension[currentDimension + 1];
                    current.FromAnd(setsPerDimension[currentDimension], dimensionValue.Item2);

                    nextIndexPerDimension[currentDimension]++;
                    currentDimension++;

                    // Skip rest of range if set already empty [slower; IsEmpty too expensive]
                    if (current.IsEmpty())
                    {
                        int rowsToSkip = 1;
                        for (int i = currentDimension; i < dimensionCount; ++i)
                        {
                            rowsToSkip *= allDimensionValueSets[i].Count;
                        }

                        nextBlockRow += rowsToSkip;
                        currentDimension--;
                    }
                }
                else
                {
                    // Non-leaf, no more values: Pop up to previous dimension for next value there
                    nextIndexPerDimension[currentDimension] = 0;
                    currentDimension--;
                }
            }

            block.SetColumn(block.ColumnCount - 1, aggregationResults);
        }

        /// <summary>
        ///  Set the dimensions (where clauses) for each row in the result block.
        /// </summary>
        /// <param name="block">DataBlock to fill</param>
        private void AddDimensionsToBlock(DataBlock block)
        {
            int dimensionCount = this.Dimensions.Count;

            int nextBlockRow = 0;
            int currentDimension = 0;
            int[] nextIndexPerDimension = new int[dimensionCount];
            object[] dimensionsForRow = new object[dimensionCount];

            while (currentDimension >= 0)
            {
                if (currentDimension == dimensionCount)
                {
                    // Leaf: Copy each dimension value to result
                    for (int i = 0; i < dimensionCount; ++i)
                    {
                        block[nextBlockRow, i] = dimensionsForRow[i];
                    }

                    nextBlockRow++;
                    currentDimension--;
                }
                else if (nextIndexPerDimension[currentDimension] < this.Dimensions[currentDimension].GroupByWhere.Count)
                {
                    // Non-leaf, more values: Add this value and get next dimension
                    dimensionsForRow[currentDimension] = this.Dimensions[currentDimension].GroupByWhere[nextIndexPerDimension[currentDimension]];
                    nextIndexPerDimension[currentDimension]++;
                    currentDimension++;
                }
                else if (nextIndexPerDimension[currentDimension] == this.Dimensions[currentDimension].GroupByWhere.Count)
                {
                    // Non-leaf, last value: Add an 'All' item
                    dimensionsForRow[currentDimension] = String.Empty;
                    nextIndexPerDimension[currentDimension]++;
                    currentDimension++;
                }
                else
                {
                    // Non-leaf, no more values: Pop up to previous dimension for next value there
                    nextIndexPerDimension[currentDimension] = 0;
                    currentDimension--;
                }
            }
        }

        public AggregationResult Merge(AggregationResult[] partitionResults)
        {
            if (partitionResults == null) throw new ArgumentNullException("partitionResults");

            AggregationResult mergedResult = new AggregationResult(this);
            mergedResult.AggregationContext = partitionResults[0].AggregationContext;

            foreach (AggregationResult result in partitionResults)
            {
                mergedResult.Total += result.Total;
                mergedResult.Details.Merge(result.Details);
            }

            if (mergedResult.Details.Succeeded)
            {
                DataBlock mergedBlock = new DataBlock(partitionResults[0].Values.Columns, partitionResults[0].Values.RowCount);
                int partitionCount = partitionResults.Length;

                // Compute the dimension values only once (they're the same for every partition)
                AddDimensionsToBlock(mergedBlock);

                // Aggregate the values from each partition into the final column
                object[] valuesPerPartition = new object[partitionCount];
                int aggregateColumnIndex = mergedBlock.ColumnCount - 1;
                for (int row = 0; row < mergedBlock.RowCount; ++row)
                {
                    // Read values for each partition for this row
                    for (int partition = 0; partition < partitionCount; ++partition)
                    {
                        valuesPerPartition[partition] = partitionResults[partition].Values.GetValue(row, aggregateColumnIndex);
                    }

                    // Merge them and set on the merged result
                    mergedBlock.SetValue(row, aggregateColumnIndex, this.Aggregator.Merge(partitionResults[0].AggregationContext, valuesPerPartition));
                }

                mergedResult.Values = mergedBlock;
            }

            return mergedResult;
        }
    }
}

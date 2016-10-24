// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Arriba.Extensions;
using Arriba.Model.Column;
using Arriba.Model.Correctors;
using Arriba.Model.Expressions;
using Arriba.Structures;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  SelectQuery represents a query sent to Arriba. It is the equivalent of
    ///  a SELECT statement in SQL.
    ///  
    ///  SELECT [Columns]
    ///     FROM [TableName]
    ///     WHERE [Where]
    ///       OFFSET [Skip] ROWS
    ///       FETCH [Count] ROWS ONLY
    ///     ORDER BY [OrderByColumn] [OrderByDescending]
    /// </summary>
    public class SelectQuery : IQuery<SelectResult>
    {
        private const string SyntaxFormatString =
            @"SELECT {0}
    FROM {1}
    WHERE {2}
    ORDER BY {3}{4}
        OFFSET {5} ROWS
        FETCH {6} ROWS ONLY";

        /// <summary>
        ///  Columns lists the column names of the columns to return.
        /// </summary>
        public IList<string> Columns { get; set; }

        /// <summary>
        ///  TableName is the name of the table to query.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        ///  Where is the IExpression representing the set of clauses used to
        ///  restrict the results.
        /// </summary>
        public IExpression Where { get; set; }

        /// <summary>
        ///  OrderByColumn is the column by which to sort results.
        /// </summary>
        public string OrderByColumn { get; set; }

        /// <summary>
        ///  OrderByDescending is true to sort in descending order by the
        ///  OrderByColumn and false to sort ascending.
        /// </summary>
        public bool OrderByDescending { get; set; }

        /// <summary>
        ///  Count is the maximum number of rows to return at one time.
        /// </summary>
        public ushort Count { get; set; }

        /// <summary>
        ///  Skip is the number of matches to skip before returning rows.
        ///  This is used with count to page through the results when many
        ///  items match the query.
        /// </summary>
        public uint Skip { get; set; }

        /// <summary>
        ///  Highlighter to use to highlight returned values. Null if no
        ///  highlighting desired.
        /// </summary>
        public Highlighter Highlighter { get; set; }

        public SelectQuery()
        {
            this.Columns = new List<string>();
            this.Count = 10;
            this.Where = new AllExpression();
        }

        public SelectQuery(IEnumerable<string> columns, string where) : this()
        {
            this.Columns = new List<string>(columns);
            this.Where = QueryParser.Parse(where);
        }

        public SelectQuery(SelectQuery copyFromOther)
        {
            if (copyFromOther == null) throw new ArgumentNullException("copyFromOther");

            this.Columns = copyFromOther.Columns;
            this.TableName = copyFromOther.TableName;
            this.Where = copyFromOther.Where;
            this.OrderByColumn = copyFromOther.OrderByColumn;
            this.OrderByDescending = copyFromOther.OrderByDescending;
            this.Count = copyFromOther.Count;
            this.Skip = copyFromOther.Skip;
            this.Highlighter = copyFromOther.Highlighter;
        }

        public override string ToString()
        {
            return StringExtensions.Format(
                SyntaxFormatString,
                String.Join(", ", this.Columns),
                this.TableName,
                this.Where,
                this.OrderByColumn,
                this.OrderByDescending ? " DESC" : "",
                this.Skip,
                this.Count
            );
        }

        public bool RequireMerge
        {
            get { return false; }
        }

        public static IExpression ParseWhere(string whereClause)
        {
            return QueryParser.Parse(whereClause);
        }

        public void Prepare(Partition p)
        {
            if (p == null) throw new ArgumentNullException("p");

            // Expand '*' to all columns if seen
            List<string> columns = new List<string>();
            foreach (string column in this.Columns)
            {
                if (column == "*")
                {
                    columns.AddRange(p.ColumnNames);
                }
                else
                {
                    columns.Add(column);
                }
            }
            this.Columns = columns;

            // ORDER BY the ID column if nothing was provided
            if (String.IsNullOrEmpty(this.OrderByColumn))
            {
                this.OrderByColumn = p.IDColumn.Name;
                this.OrderByDescending = true;
            }
        }

        public void OnBeforeQuery(Table table)
        {
        }

        public void Correct(ICorrector corrector)
        {
            if (corrector == null) throw new ArgumentNullException("corrector");

            this.Where = corrector.Correct(this.Where);
        }

        public SelectResult Compute(Partition p)
        {
            if (p == null) throw new ArgumentNullException("p");

            SelectResult result = new SelectResult(this);

            // Verify that the ORDER BY column exists
            if (!p.Columns.ContainsKey(this.OrderByColumn))
            {
                result.Details.AddError(ExecutionDetails.ColumnDoesNotExist, this.OrderByColumn);
                return result;
            }

            // Find the set of items matching all terms
            ShortSet whereSet = new ShortSet(p.Count);
            this.Where.TryEvaluate(p, whereSet, result.Details);

            if (result.Details.Succeeded)
            {
                result.Total = whereSet.Count();

                // Find the set of IDs to return for the query (up to 'Count' after 'Skip' in ORDER BY order)
                ushort[] lidsToReturn = GetLIDsToReturn(p, this, result, whereSet);
                result.CountReturned = (ushort)lidsToReturn.Length;

                // Get all of the response columns and return them
                Array columns = new Array[this.Columns.Count];
                for (int i = 0; i < this.Columns.Count; ++i)
                {
                    string columnName = this.Columns[i];

                    IUntypedColumn column;
                    if (!p.Columns.TryGetValue(columnName, out column))
                    {
                        result.Details.AddError(ExecutionDetails.ColumnDoesNotExist, columnName);
                        return result;
                    }

                    Array values = column.GetValues(lidsToReturn);
                    if (this.Highlighter != null)
                    {
                        this.Highlighter.Highlight(values, column, this);
                    }

                    columns.SetValue(values, i);
                }

                result.Values = new DataBlock(p.GetDetails(this.Columns), result.CountReturned, columns);
            }

            return result;
        }

        #region Order By
        private ushort[] GetLIDsToReturn(Partition p, SelectQuery query, SelectResult result, ShortSet whereSet)
        {
            if (result.Total == 0) return new ushort[0];

            // Compute the most efficient way to scan.
            //  Sparse - get and sort the order by values for all matches.
            //  Dense - walk the order by column in order until we find enough matches.
            //  Walking in order measures about 20 times faster than Array.Sort() (cache locality; instruction count)
            int sparseCompareCount = (int)(result.Total * Math.Log(result.Total, 2));
            double densePercentageToScan = Math.Min(1.0d, (double)(query.Skip + query.Count) / (double)(result.Total));
            int denseCheckCount = (int)(p.Count * densePercentageToScan);

            if (sparseCompareCount * 20 < denseCheckCount)
            {
                return GetLIDsToReturnSparse(p, query, result, whereSet);
            }
            else
            {
                return GetLIDsToReturnDense(p, query, result, whereSet);
            }
        }

        private ushort[] GetLIDsToReturnSparse(Partition p, SelectQuery query, SelectResult result, ShortSet whereSet)
        {
            // Get the set of matching IDs
            ushort[] lids = whereSet.Values;

            // Lame - store the total to return here so we don't have to compute again
            result.Total = (uint)(lids.Length);

            // Compute the count to return - the count or the number left after skipping
            int countToReturn = Math.Min(query.Count, (int)(lids.Length - query.Skip));
            if (countToReturn < 0) return new ushort[0];

            // If no ORDER BY is provided, the default is the ID column descending
            if (String.IsNullOrEmpty(query.OrderByColumn))
            {
                query.OrderByColumn = p.IDColumn.Name;
                query.OrderByDescending = true;
            }

            // Get the values for all matches in the Order By column and IDs by the order by values
            Array orderByValues = p.Columns[query.OrderByColumn].GetValues(lids);
            Array.Sort(orderByValues, lids);

            // Walk in ascending or descending order and return the matches
            int count = 0;
            ushort[] lidsToReturn = new ushort[countToReturn];

            int index, end, step;
            if (query.OrderByDescending)
            {
                index = (int)((lids.Length - 1) - query.Skip);
                end = index - countToReturn;
                step = -1;
            }
            else
            {
                index = (int)query.Skip;
                end = index + countToReturn;
                step = 1;
            }

            for (; index != end; index += step)
            {
                lidsToReturn[count] = lids[index];
                count++;
            }

            return lidsToReturn;
        }

        private ushort[] GetLIDsToReturnDense(Partition p, SelectQuery query, SelectResult result, ShortSet whereSet)
        {
            int countToReturn = Math.Min(query.Count, (int)(result.Total - query.Skip));
            if (countToReturn < 0) return new ushort[0];

            ushort[] lidsToReturn = new ushort[countToReturn];

            // If no ORDER BY is provided, the default is the ID column descending
            if (String.IsNullOrEmpty(query.OrderByColumn))
            {
                query.OrderByColumn = p.IDColumn.Name;
                query.OrderByDescending = true;
            }

            // Enumerate in order by the OrderByColumn
            IColumn<object> orderByColumn = p.Columns[query.OrderByColumn];
            IList<ushort> sortedLIDs;
            int sortedLIDsCount;
            orderByColumn.TryGetSortedIndexes(out sortedLIDs, out sortedLIDsCount);

            // Enumerate matches in OrderBy order and return the requested columns for them
            ushort countSkipped = 0;
            ushort countAdded = 0;
            int sortedIndex = (query.OrderByDescending ? orderByColumn.Count - 1 : 0);
            int lastIndex = (query.OrderByDescending ? -1 : orderByColumn.Count);
            int step = (query.OrderByDescending ? -1 : 1);

            // Skip the first 'skip' matches
            if (query.Skip > 0)
            {
                for (; sortedIndex != lastIndex; sortedIndex += step)
                {
                    ushort lid = sortedLIDs[sortedIndex];
                    if (whereSet.Contains(lid))
                    {
                        if (++countSkipped == query.Skip) break;
                    }
                }
            }

            // Return the next 'count' matches
            for (; sortedIndex != lastIndex; sortedIndex += step)
            {
                ushort lid = sortedLIDs[sortedIndex];
                if (whereSet.Contains(lid))
                {
                    lidsToReturn[countAdded] = lid;
                    if (++countAdded == countToReturn) break;
                }
            }

            return lidsToReturn;
        }
        #endregion

        #region Merge
        /// <summary>
        ///  Identify the partition from which items should come to be sorted overall.
        /// </summary>
        /// <param name="partitionResults">DataBlock per partition with items to merge, sort column last</param>
        public SelectResult Merge(SelectResult[] partitionResults)
        {
            if (partitionResults == null) throw new ArgumentNullException("partitionResults");

            SelectResult mergedResult = new SelectResult(this);

            // Aggregate the total across partitions
            uint totalFound = 0;
            uint totalReturned = 0;
            for (int i = 0; i < partitionResults.Length; ++i)
            {
                totalFound += partitionResults[i].Total;
                totalReturned += partitionResults[i].CountReturned;

                mergedResult.Details.Merge(partitionResults[i].Details);
            }

            mergedResult.Total = totalFound;

            // If there are no values to return, stop
            if (totalReturned < this.Skip) return mergedResult;

            mergedResult.CountReturned = (ushort)Math.Min(this.Count, totalReturned - this.Skip);

            if (mergedResult.Details.Succeeded)
            {
                // Build a DataBlock for the merged result values
                List<ColumnDetails> columnsWithoutSort = new List<ColumnDetails>(partitionResults[0].Values.Columns);
                columnsWithoutSort.RemoveAt(columnsWithoutSort.Count - 1);

                DataBlock mergedBlock = new DataBlock(columnsWithoutSort, mergedResult.CountReturned);

                // Find the next value according to the sort order and add it until we have enough
                bool orderByDescending = this.OrderByDescending;
                int sortByColumn = (partitionResults[0].Values.ColumnCount - 1);
                int partitionCount = partitionResults.Length;

                int skipCount = (int)this.Skip;
                int itemIndex = 0;

                int[] nextIndexPerPartition = new int[partitionCount];

                while (skipCount > 0 || itemIndex < mergedResult.CountReturned)
                {
                    int bestPartition = -1;
                    IComparable bestValue = null;

                    // Find the column with the next value to merge
                    for (int partitionIndex = 0; partitionIndex < partitionCount; ++partitionIndex)
                    {
                        int potentialIndex = nextIndexPerPartition[partitionIndex];
                        if (potentialIndex == partitionResults[partitionIndex].Values.RowCount) continue;
                        IComparable potentialValue = (IComparable)partitionResults[partitionIndex].Values.GetValue(potentialIndex, sortByColumn);

                        int cmp = 0;
                        if (bestValue != null) cmp = potentialValue.CompareTo(bestValue);

                        if (bestPartition == -1 || (orderByDescending && cmp > 0) || (!orderByDescending && cmp < 0))
                        {
                            bestPartition = partitionIndex;
                            bestValue = potentialValue;
                        }
                    }

                    if (skipCount > 0)
                    {
                        // If there are more values to skip, skip this one
                        skipCount--;
                    }
                    else
                    {
                        // If this value should be included, write it next
                        for (int columnIndex = 0; columnIndex < mergedBlock.ColumnCount; ++columnIndex)
                        {
                            mergedBlock.SetValue(itemIndex, columnIndex, partitionResults[bestPartition].Values.GetValue(nextIndexPerPartition[bestPartition], columnIndex));
                        }

                        itemIndex++;
                    }

                    nextIndexPerPartition[bestPartition]++;
                }

                mergedResult.Values = mergedBlock;
            }

            return mergedResult;
        }
        #endregion
    }
}

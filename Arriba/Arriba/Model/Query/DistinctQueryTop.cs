// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Arriba.Structures;
using Arriba.Model.Expressions;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  DistinctQueryTop returns the most common unique values for a given column
    ///  in a given query. It is used to provide Inline Insight results for "[Column] = ".
    /// </summary>
    public class DistinctQueryTop : DistinctQuery
    {
        public string ValuePrefix { get; set; }

        public DistinctQueryTop() : base()
        { }

        public DistinctQueryTop(string column, string where, ushort count)
            : base(column, where, count)
        { }

        public DistinctQueryTop(string column, string valuePrefix, string where, ushort count)
            : base(column, where, count)
        {
            this.ValuePrefix = valuePrefix;
        }

        public override DistinctResult Compute(Partition p)
        {
            if (p == null) throw new ArgumentNullException("p");
            DistinctResult result = new DistinctResult(this);

            // Verify the column exists
            if (!p.ContainsColumn(this.Column))
            {
                result.Details.AddError(ExecutionDetails.ColumnDoesNotExist, this.Column);
                return result;
            }

            // Find the set of items matching the base where clause
            ShortSet whereSet = new ShortSet(p.Count);
            this.Where.TryEvaluate(p, whereSet, result.Details);

            // Capture the total of the base query
            result.Total = whereSet.Count();

            // Add a prefix filter for the prefix so far, if any and the column can prefix match
            if (!String.IsNullOrEmpty(this.ValuePrefix))
            {
                ExecutionDetails prefixDetails = new ExecutionDetails();
                ShortSet prefixSet = new ShortSet(p.Count);
                new TermExpression(this.Column, Operator.StartsWith, this.ValuePrefix).TryEvaluate(p, prefixSet, prefixDetails);
                if (prefixDetails.Succeeded) whereSet.And(prefixSet);
            }

            if (result.Details.Succeeded)
            {
                // Count the occurences of each value
                Dictionary<object, int> countByValue = new Dictionary<object, int>();
                IUntypedColumn column = p.Columns[this.Column];

                for (int i = 0; i < column.Count; ++i)
                {
                    ushort lid = (ushort)i;
                    if (whereSet.Contains(lid))
                    {
                        object value = column[lid];

                        int count;
                        countByValue.TryGetValue(value, out count);
                        countByValue[value] = count + 1;
                    }
                }

                // Convert the top this.Count rows by count into a DataBlock
                result.Values = ToDataBlock(countByValue, this.Column, (int)this.Count);

                result.AllValuesReturned = result.Values.RowCount == countByValue.Count;
                
            }

            return result;
        }

        private static DataBlock ToDataBlock(Dictionary<object, int> countByValue, string columnName, int desiredCount)
        {
            // Determine how many items to return
            int countToReturn = Math.Min(countByValue.Count, desiredCount);

            // Build result arrays
            int next = 0;
            object[] values = new object[countToReturn];
            int[] counts = new int[countToReturn];

            // Sort the dictionary by count descending and copy values
            foreach (var entry in countByValue.OrderByDescending((kvp => kvp.Value)))
            {
                values[next] = entry.Key;
                counts[next] = entry.Value;
                if (++next == countToReturn) break;
            }

            // Copy arrays to a DataBlock and return
            return new DataBlock(new string[] { columnName, "count" }, countToReturn, new object[] { values, counts });
        }

        public override DistinctResult Merge(DistinctResult[] partitionResults)
        {
            if (partitionResults == null) throw new ArgumentNullException("partitionResults");
            if (partitionResults.Length == 0) throw new ArgumentException("Length==0 not supported", "partitionResults");
            if (!partitionResults[0].Details.Succeeded) return partitionResults[0];

            DistinctResult mergedResult = new DistinctResult(this);
            mergedResult.ColumnType = partitionResults[0].ColumnType;
            mergedResult.AllValuesReturned = true;

            Dictionary<object, int> countByValue = new Dictionary<object, int>();

            for (int partitionIndex = 0; partitionIndex < partitionResults.Length; ++partitionIndex)
            {
                DistinctResult result = partitionResults[partitionIndex];
                DataBlock block = result.Values;

                // Add count per value together from each partition
                for (int rowIndex = 0; rowIndex < block.RowCount; ++rowIndex)
                {
                    object value = block[rowIndex, 0];
                    int countFromPartition = (int)block[rowIndex, 1];

                    int count;
                    countByValue.TryGetValue(value, out count);
                    countByValue[value] = count + countFromPartition;
                }

                // Merge other properties
                mergedResult.Details.Merge(result.Details);
                mergedResult.AllValuesReturned &= result.AllValuesReturned;
                mergedResult.Total += result.Total;
            }

            mergedResult.Values = ToDataBlock(countByValue, this.Column, (int)this.Count);

            return mergedResult;
        }
    }
}

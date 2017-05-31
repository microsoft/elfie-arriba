// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

using Arriba.Extensions;
using Arriba.Model.Correctors;
using Arriba.Model.Expressions;
using Arriba.Structures;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  DistinctQuery enables getting the set of unique values for a given
    ///  column within a specific filter up to a configured limit. It is
    ///  roughly equivalent to SELECT DISTINCT [column] in SQL, but only for
    ///  a single column.
    /// </summary>
    public class DistinctQuery : IQuery<DistinctResult>
    {
        /// <summary>
        ///  Column for which to return distinct values
        /// </summary>
        public string Column { get; set; }

        /// <summary>
        ///  The IExpression for the subset of items for which to find values
        /// </summary>
        public IExpression Where { get; set; }

        /// <summary>
        ///  Table to query
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        ///  The maximum number of distinct values to return, 0 to return all items
        /// </summary>
        public uint Count { get; set; }

        public DistinctQuery()
        {
            this.Count = 10;
            this.Where = new AllExpression();
        }

        public DistinctQuery(string column, string where, ushort count)
        {
            this.Column = column;
            this.Where = QueryParser.Parse(where);
            this.Count = count;
        }

        public void OnBeforeQuery(ITable table)
        {
            this.Where = this.Where ?? new AllExpression();
        }

        public bool RequireMerge
        {
            get { return false; }
        }

        public void Correct(ICorrector corrector)
        {
            if (corrector == null) throw new ArgumentNullException("corrector");

            this.Where = corrector.Correct(this.Where);
        }

        public virtual DistinctResult Compute(Partition p)
        {
            if (p == null) throw new ArgumentNullException("p");

            ushort countToReturnPerPartition = (this.Count == 0 || this.Count > ushort.MaxValue) ? ushort.MaxValue : (ushort)this.Count;

            DistinctResult result = new DistinctResult(this);

            // Verify the column exists
            if (!p.ContainsColumn(this.Column))
            {
                result.Details.AddError(ExecutionDetails.ColumnDoesNotExist, this.Column);
                return result;
            }

            // Find the set of items matching the where clause
            ShortSet whereSet = new ShortSet(p.Count);
            this.Where.TryEvaluate(p, whereSet, result.Details);

            if (result.Details.Succeeded)
            {
                IUntypedColumn column = p.Columns[this.Column];

                // Construct a helper object of the correct type to natively work with the column
                GetUniqueValuesWorker helper = NativeContainer.CreateTypedInstance<GetUniqueValuesWorker>(typeof(GetUniqueValuesWorker<>), column.ColumnType);

                bool allValuesReturned;
                Array uniqueValues = helper.GetUniqueValuesFromColumn(column.InnerColumn, whereSet, countToReturnPerPartition, out allValuesReturned);

                result.ColumnType = column.ColumnType;
                result.AllValuesReturned = allValuesReturned;

                // Build a DataBlock with the results and return it
                DataBlock resultValues = new DataBlock(new string[] { this.Column }, uniqueValues.GetLength(0));
                resultValues.SetColumn(0, uniqueValues);
                result.Values = resultValues;
            }

            return result;
        }

        public virtual DistinctResult Merge(DistinctResult[] partitionResults)
        {
            if (partitionResults == null) throw new ArgumentNullException("partitionResults");
            if (partitionResults.Length == 0) throw new ArgumentException("Length==0 not supported", "partitionResults");
            if (!partitionResults[0].Details.Succeeded) return partitionResults[0];

            DistinctResult mergedResult = new DistinctResult(this);
            mergedResult.ColumnType = partitionResults[0].ColumnType;
            mergedResult.AllValuesReturned = true;

            // Construct a helper object of the correct type to natively work with the column
            GetUniqueValuesWorker helper = NativeContainer.CreateTypedInstance<GetUniqueValuesWorker>(typeof(GetUniqueValuesWorker<>), partitionResults[0].ColumnType);

            IUniqueValueMerger merger = helper.GetMerger();

            for (int i = 0; i < partitionResults.Length; ++i)
            {
                DistinctResult result = partitionResults[i];

                // Merge Details
                mergedResult.Details.Merge(result.Details);

                // Merge whether values remain in any partition
                mergedResult.AllValuesReturned &= result.AllValuesReturned;

                // Add the values themselves to merge
                if (result.Values != null)
                {
                    merger.Add(result.Values.GetColumn(0));
                }
            }

            Array uniqueValues = merger.GetUniqueValues((int)this.Count);

            // Copy the merged values into a block
            DataBlock mergedBlock = new DataBlock(new string[] { this.Column }, uniqueValues.GetLength(0));
            mergedBlock.SetColumn(0, uniqueValues);
            mergedResult.Values = mergedBlock;

            // If the merge didn't return everything, we didn't return everything
            mergedResult.AllValuesReturned &= (uniqueValues.GetLength(0) == merger.Count);

            return mergedResult;
        }

        public override string ToString()
        {
            return StringExtensions.Format("SELECT TOP {0:n0} DISTINCT {1} WHERE {1}", this.Count, this.Column, this.Where);
        }

        /// <summary>
        /// Untyped interface to NativeContainerHelper<T>
        /// </summary>
        private abstract class GetUniqueValuesWorker
        {
            public abstract Array GetUniqueValuesFromColumn(IColumn column, ShortSet whereSet, int count, out bool allValuesReturned);
            public abstract IUniqueValueMerger GetMerger();
        }

        /// <summary>
        /// Helper class that contains methods to work on a strongly typed column.  This allows computation to occur
        /// with limited overhead of boxing/casting that is required when transitioning between untyped and typed domains
        /// </summary>
        private class GetUniqueValuesWorker<T> : GetUniqueValuesWorker where T : IEquatable<T>
        {
            public override Array GetUniqueValuesFromColumn(IColumn column, ShortSet whereSet, int count, out bool allValuesReturned)
            {
                if (count <= 0)
                {
                    allValuesReturned = false;
                    return new T[0];
                }

                IColumn<T> typedColumn = (IColumn<T>)column;

                // Boolean columns aren't sorted - just check true and false
                if (typedColumn is BooleanColumn)
                {
                    int countBefore = whereSet.Count();
                    if (countBefore == 0)
                    {
                        allValuesReturned = true;
                        return new bool[0];
                    }

                    // Filter to the set of values with the column 'true'
                    BooleanColumn bc = (BooleanColumn)typedColumn;
                    ShortSet trueSet = new ShortSet(bc.Count);
                    bc.TryWhere(Operator.Equals, true, trueSet, null);
                    whereSet.And(trueSet);

                    // Determine the count which were true and false matching the query
                    int countWhichAreTrue = whereSet.Count();
                    int countWhichAreFalse = countBefore - countWhichAreTrue;

                    allValuesReturned = true;

                    if (countWhichAreTrue > 0 && countWhichAreFalse > 0)
                    {
                        // If both existed and only one value was requested, return the value which more items had
                        if (count == 1)
                        {
                            allValuesReturned = false;
                            return new bool[] { (countWhichAreTrue > countWhichAreFalse) };
                        }

                        return new bool[] { true, false };
                    }
                    else if (countWhichAreTrue > 0)
                    {
                        return new bool[] { true };
                    }
                    else
                    {
                        return new bool[] { false };
                    }
                }

                // Get all LIDs in sorted order
                IList<ushort> sortedIndexes;
                int sortedIndexesCount;
                if (!column.TryGetSortedIndexes(out sortedIndexes, out sortedIndexesCount)) throw new ArribaException(String.Format("Unable to sort by non-sorted column {0}", column.Name));

                int uniqueValuesCount = 0;
                T prevValue = default(T);

                // Count the # of unique items
                for (int lastSortedIndex = 0; lastSortedIndex < sortedIndexesCount; ++lastSortedIndex)
                {
                    ushort itemLID = sortedIndexes[lastSortedIndex];

                    // Find the next LID which is still in our match set
                    if (whereSet.Contains(itemLID))
                    {
                        T currentValue = typedColumn[itemLID];

                        bool sameValue = prevValue.Equals(currentValue);

                        if (uniqueValuesCount != 0 && sameValue == true)
                        {
                            continue;
                        }

                        uniqueValuesCount++;
                        prevValue = currentValue;

                        // If we have enough matches, stop 
                        // (*after computing remaining items to tell if we got all of them*)
                        if (uniqueValuesCount == count)
                        {
                            break;
                        }
                    }
                }

                T[] uniqueValues = new T[uniqueValuesCount];
                ushort uniqueValuesIndex = 0;
                allValuesReturned = true;       // until proven false

                // Retrieve all of the unique items
                for (int lastSortedIndex = 0; lastSortedIndex < sortedIndexesCount; ++lastSortedIndex)
                {
                    ushort itemLID = sortedIndexes[lastSortedIndex];

                    // Find the next LID which is still in our match set
                    if (whereSet.Contains(itemLID))
                    {
                        T currentValue = typedColumn[itemLID];

                        bool sameValue = prevValue.Equals(currentValue);

                        if (uniqueValuesIndex != 0 && sameValue == true)
                        {
                            continue;
                        }

                        uniqueValues[uniqueValuesIndex++] = currentValue;
                        prevValue = currentValue;

                        // If we have enough matches, stop 
                        // (*after computing remaining items to tell if we got all of them*)
                        if (uniqueValuesIndex == count)
                        {
                            ushort lastItemLID = sortedIndexes[sortedIndexesCount - 1];
                            T lastValue = typedColumn[lastItemLID];
                            allValuesReturned = currentValue.Equals(lastValue);
                            break;
                        }
                    }
                }

                return uniqueValues;
            }

            public override IUniqueValueMerger GetMerger()
            {
                return new UniqueValueMerger<T>();
            }
        }
    }
}

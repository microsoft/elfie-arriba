// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Structures;

namespace Arriba.Model.Aggregations
{
    /// <summary>
    ///  IAggregator represents any aggregation function available in Arriba.
    ///  It requires the ability to compute a single aggregate value given
    ///  a set of matching items and any column(s) passed to the function in the
    ///  query, and the ability to merge partial results from multiple partitions.
    ///  
    ///  This design is somewhat like Map/Reduce where Aggregate is Map but
    ///  scoped to produce a single intermediate "key" and Merge is Reduce on
    ///  values with that key.
    ///  
    ///  NOTE: Performance is critical for aggregators, which are the core of
    ///  analytics. In order to maximize performance, aggregators must:
    ///  
    ///   - Call matches.Values once to get all matching item LIDs.
    ///   - Call column.GetValues once to get all values in a properly typed array.
    ///   - Identify column type once and cast the value array to the correct type to avoid per item casts.
    ///   - Have no method calls per item (unless they are inlined by the compiler).
    ///   
    ///  Test aggregators with columns of all supported times. Aggregators
    ///  following these guidelines should be able to aggregate 100M items per
    ///  second per core.
    /// </summary>
    public interface IAggregator
    {
        /// <summary>
        /// Flags indicating the aggregator requires the merge step
        /// </summary>
        bool RequireMerge { get; }

        /// <summary>
        /// Creates a unique context for an aggregation instance.  A context is a piece of data shared across all related calls to
        /// Aggregate or Merge.
        /// </summary>
        /// <returns>a unique context object for an aggregation session</returns>
        object CreateContext();

        /// <summary>
        ///  Compute the value of this Aggregate for the provided matches
        ///  given the provided columns. [Ex: SELECT SUM(ItemCost) will create
        ///  a SumAggregator which will be passed matches and the ItemCost
        ///  column].
        ///  
        ///  NOTE: See IAggregator class comment for implementation rules.
        /// </summary>
        /// <param name="context">aggregation context</param>
        /// <param name="matches">Set of matching rows to aggregate</param>
        /// <param name="columns">Columns, if any, the query passed to the aggregation function</param>
        /// <returns>Aggregate value for this partition</returns>
        object Aggregate(object context, ShortSet matches, IUntypedColumn[] columns);

        /// <summary>
        ///  Merge intermediate results from multiple partitions.
        ///  The values passed will be the same type returned by Aggregate,
        ///  so values can be cast to the known type actually returned by
        ///  Aggregate.
        ///  
        ///  NOTE: See IAggregator class comment for implementation rules.
        /// </summary>
        /// <param name="context">aggregation context</param>
        /// <param name="values">Values returned by Aggregate for multiple partitions</param>
        /// <returns>Combined value for all partitions together</returns>
        object Merge(object context, object[] values);
    }
}

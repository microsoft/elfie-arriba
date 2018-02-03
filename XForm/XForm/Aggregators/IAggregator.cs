// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using XForm.Data;
using XForm.Query;

namespace XForm.Aggregators
{
    /// <summary>
    ///  IAggregators can aggregate values from a set of rows.
    ///  The rows are mapped to ascending indices by GroupBy, which are passed to Add.
    ///  The aggregator then gets current values from any input columns and computes the aggregations.
    /// </summary>
    public interface IAggregator
    {
        /// <summary>
        ///  ColumnDetails for result column (the name and type aggregated; ex: RequestBytes.Sum)
        /// </summary>
        ColumnDetails ColumnDetails { get; }

        /// <summary>
        ///  Get the set of aggregated values in order by index. Called after all Add() calls
        /// </summary>
        XArray Values { get; }

        /// <summary>
        ///  Add a set of rows to the aggregation
        /// </summary>
        /// <param name="rowIndices">Index of the distinct value of each row (ascending, zero-based indices)</param>
        /// <param name="newDistinctCount">Total Distinct count including this set (used to resize the aggregation array)</param>
        void Add(XArray rowIndices, int newDistinctCount);
    }

    /// <summary>
    ///  IAggregatorBuilder is a builder interface to build IAggregators
    /// </summary>
    public interface IAggregatorBuilder : INamedBuilder, IUsage
    {
        IAggregator Build(IXTable source, XDatabaseContext context);
    }

    /// <summary>
    ///  IFoundIndicesTrackers are IAggregators which can identify which buckets had any rows
    ///  added to them. When aggregating on Enum columns, these can be used to figure out which
    ///  enum values actually had rows in the results.
    /// </summary>
    public interface IFoundIndicesTracker : IAggregator
    {
        ArraySelector FoundIndices { get; }
    }
}

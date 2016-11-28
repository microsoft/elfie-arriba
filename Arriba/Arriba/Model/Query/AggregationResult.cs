// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Arriba.Model.Query
{
    /// <summary>
    ///  AggregrationResult is the object returned by an aggregation query.
    ///  It contains a table with the aggregated values returned by each
    ///  dimension provided in the query.
    /// </summary>
    public class AggregationResult : DataBlockResult
    {
        internal object AggregationContext { get; set; }

        public AggregationResult(AggregationQuery query) : base(query)
        { }
    }
}

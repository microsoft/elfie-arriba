// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Structures;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  AggregrationResult is the object returned by an aggregation query.
    ///  It contains a table with the aggregated values returned by each
    ///  dimension provided in the query.
    /// </summary>
    public class AggregationResult : BaseResult
    {
        /// <summary>
        /// Total number of rows reflected in the results
        /// </summary>
        public uint Total { get; set; }
        public AggregationQuery Query { get; set; }
        public DataBlock Values { get; set; }

        internal object AggregationContext { get; set; }

        public AggregationResult(AggregationQuery query)
        {
            this.Query = query;
        }
    }
}

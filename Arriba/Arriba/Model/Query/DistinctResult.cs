// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Model.Expressions;
using Arriba.Structures;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  DistinctResult is the object returned from a DistinctQuery.
    ///  It contains the set of distinct values in a one-column DataBlock.
    /// </summary>
    public class DistinctResult : BaseResult
    {
        public DistinctQuery Query { get; set; }
        public DataBlock Values { get; set; }
        public System.Type ColumnType { get; set; }
        public bool AllValuesReturned { get; set; }

        public DistinctResult(DistinctQuery query) : base()
        {
            this.Query = query;
        }

        /// <summary>
        ///  Convert the set of values returned into a dimension for an aggregation.
        ///  NOTE: Aggregations with huge numbers of distinct values aren't recommended.
        /// </summary>
        /// <returns>AggregationDimension to use to 'Group By' the column</returns>
        public AggregationDimension ToAggregationDimension()
        {
            AggregationDimension d = new AggregationDimension();
            d.Name = this.Query.Column;

            for (int rowIndex = 0; rowIndex < this.Values.RowCount; ++rowIndex)
            {
                d.GroupByWhere.Add(new TermExpression(this.Query.Column, Operator.Equals, this.Values[rowIndex, 0]));
            }

            return d;
        }
    }
}

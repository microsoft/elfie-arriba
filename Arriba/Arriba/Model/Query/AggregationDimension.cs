// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Extensions;
using Arriba.Model.Expressions;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  AggregationDimensions are included in an AggregationQuery, and separate aggregates
    ///  are computed for each combination of dimensions. A dimension may be a column (so
    ///  that each distinct value for that column will be aggregated) or a set of Where
    ///  clauses (so that the matches for each clause will be computed across each other
    ///  dimension).
    /// </summary>
    public class AggregationDimension
    {
        /// <summary>
        ///  Label to return for the column containing this dimension
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///  GroupByWhere is the set of Where clauses for which to aggregate on each
        ///  distinct value, in Grid style aggregations.
        /// </summary>
        public IList<IExpression> GroupByWhere { get; set; }

        public AggregationDimension()
        {
            this.GroupByWhere = new List<IExpression>();
        }

        public AggregationDimension(string name) : this()
        {
            this.Name = name;
        }

        public AggregationDimension(string name, params string[] conditions)
            : this(name, (IEnumerable<string>)conditions)
        {
        }

        public AggregationDimension(string name, IEnumerable<string> conditions)
            : this()
        {
            this.Name = name;

            this.AddConditions(conditions);
        }

        public virtual void OnBeforeQuery(ITable table, IExpression baseWhere)
        {
        }

        protected void AddConditions(IEnumerable<string> conditions)
        {
            foreach (string condition in conditions)
            {
                this.AddCondition(condition);
            }
        }

        protected void AddCondition(string condition)
        {
            this.GroupByWhere.Add(SelectQuery.ParseWhere(condition));
        }

        public override string ToString()
        {
            return StringExtensions.Format("Query [{0}]", String.Join(",", this.GroupByWhere));
        }
    }
}

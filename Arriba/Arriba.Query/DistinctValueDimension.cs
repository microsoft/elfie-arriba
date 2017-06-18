// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Extensions;
using Arriba.Model.Expressions;

namespace Arriba.Model.Query
{
    public class DistinctValueDimension : AggregationDimension
    {
        public string Column { get; set; }
        public ushort? MaximumValues { get; set; }
        private const int DefaultMaximumValues = 50;

        public DistinctValueDimension() : base()
        { }

        public DistinctValueDimension(string column) : base(StringExtensions.Format("Distinct Values [{0}]", column))
        {
            this.Column = column;
        }

        public override void OnBeforeQuery(ITable table, IExpression where)
        {
            var result = table.Query(new DistinctQueryTop(this.Column, "", MaximumValues ?? DefaultMaximumValues) { Where = where });

            if (result.Details.Succeeded)
            {
                if (result.Values != null)
                {
                    // If there are more than 20 values, try getting distributions and use them if available
                    if (result.Values.RowCount > 20)
                    {
                        var dr = table.Query(new DistributionQuery(this.Column, "", true) { Where = where });
                        if (dr.Details.Succeeded && dr.Values != null)
                        {
                            this.AddCondition(StringExtensions.Format("{0} <= {1}", QueryParser.WrapColumnName(this.Column), QueryParser.WrapValue(dr.Values[0, 0])));

                            for (var i = 1; i < dr.Values.RowCount - 1; i++)
                            {
                                this.AddCondition(StringExtensions.Format("{0} <= {1} AND {0} > {2}", QueryParser.WrapColumnName(this.Column), QueryParser.WrapValue(dr.Values[i, 0]), QueryParser.WrapValue(dr.Values[i - 1, 0])));
                            }

                            this.AddCondition(StringExtensions.Format("{0} > {1}", QueryParser.WrapColumnName(this.Column), QueryParser.WrapValue(dr.Values[dr.Values.RowCount - 2, 0])));

                            return;
                        }
                    }

                    // Otherwise, use the Distinct values
                    for (var i = 0; i < result.Values.RowCount; i++)
                    {
                        this.AddCondition(StringExtensions.Format("{0} = {1}", QueryParser.WrapColumnName(this.Column), QueryParser.WrapValue(result.Values[i, 0])));
                    }
                }
            }
            else
            {
                // If the column doesn't exist or the query fails, add one dimension value so the overall AggregationQuery will return a good error
                this.AddCondition(StringExtensions.Format("[{0}]=\"\"", this.Column));
            }
        }
    }
}

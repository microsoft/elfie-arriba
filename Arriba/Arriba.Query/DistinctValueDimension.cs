// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            var distinctQuery = new DistinctQueryTop(this.Column, "", MaximumValues ?? DefaultMaximumValues) { Where = where };

            var result = table.Query(distinctQuery);

            if (result.Details.Succeeded)
            {
                for (var i = 0; i < result.Values.RowCount; i++)
                {
                    this.AddCondition(StringExtensions.Format("[{0}]=\"{1}\"", this.Column, result.Values[i, 0]));
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

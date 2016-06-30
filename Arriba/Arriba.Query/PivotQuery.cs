// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Arriba.Extensions;

namespace Arriba.Model.Query
{
    public class PivotQuery : AggregationQuery
    {
        public override void OnBeforeQuery(Table table)
        {
            base.OnBeforeQuery(table);

            // Ask each pivot dimension to compute it's group by where clauses for
            // the current table. 
            foreach (var pivot in this.PivotDimensions)
            {
                pivot.CalculateClauses(table, this.Where);
            }
        }

        private IEnumerable<PivotDimension> PivotDimensions
        {
            get
            {
                return this.Dimensions.OfType<PivotDimension>();
            }
        }

        private string GetPivotDimensionNames()
        {
            return String.Join("," + Environment.NewLine, this.Dimensions.Select(d => String.Concat("[", d.ToString(), "]")));
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("PIVOT");
            result.AppendLine(StringExtensions.Format("\t{0}({1})", this.Aggregator, String.Join(", ", this.AggregationColumns)));
            result.AppendLine("OVER");
            result.AppendLine(this.GetPivotDimensionNames());
            result.AppendLine();
            result.AppendLine("PRODUCES");
            result.AppendLine("(");
            result.AppendLine(base.ToString());
            result.AppendLine(")");

            return result.ToString();
        }
    }
}

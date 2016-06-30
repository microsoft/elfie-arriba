// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Extensions;
using Arriba.Model.Expressions;

namespace Arriba.Model.Query
{
    public class DistinctValuePivotDimension : PivotDimension
    {
        private const int DefaultMaximumValues = 50;

        public DistinctValuePivotDimension() : base()
        { }

        public DistinctValuePivotDimension(string column) : base(column, StringExtensions.Format("Distinct Values [{0}]", column))
        { }

        public ushort? MaximumValues
        {
            get;
            set;
        }

        internal override void CalculateClauses(Table table, IExpression where)
        {
            var distinctQuery = new DistinctQuery
            {
                Column = this.Column,
                Where = where,
                Count = MaximumValues ?? DefaultMaximumValues
            };

            var result = table.Query(distinctQuery);

            for (var i = 0; i < result.Values.RowCount; i++)
            {
                this.AddCondition(StringExtensions.Format("[{0}]=\"{1}\"", this.Column, result.Values[i, 0]));
            }
        }
    }
}

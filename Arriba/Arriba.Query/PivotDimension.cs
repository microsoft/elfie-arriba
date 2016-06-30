// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Arriba.Model.Expressions;
using Arriba.Structures;

namespace Arriba.Model.Query
{
    public abstract class PivotDimension : AggregationDimension
    {
        public PivotDimension() : base()
        { }

        public PivotDimension(string column) : base()
        {
            this.Column = column;
        }

        public PivotDimension(string column, string name) : base(name)
        { }

        public string Column { get; set; }

        internal abstract void CalculateClauses(Table table, IExpression where);


        protected bool TryGetScalarValue<T>(Table table, IExpression where, ScalarValueType type, out T value)
        {
            value = default(T);
            var cols = new List<string>() { this.Column, this.Column }; // BUG BUG 

            var scalarQuery = table.Query(new SelectQuery()
            {
                Columns = cols,
                OrderByColumn = this.Column,
                OrderByDescending = (type == ScalarValueType.MaximumValue),
                Count = 1,
                Where = where
            });

            if (scalarQuery.CountReturned == 1 && scalarQuery.Values.ColumnCount > 0)
            {
                return Value.Create(scalarQuery.Values[0, 0]).TryConvert<T>(out value);
            }

            return false;
        }

        protected enum ScalarValueType
        {
            MinimumValue,
            MaximumValue
        }
    }
}

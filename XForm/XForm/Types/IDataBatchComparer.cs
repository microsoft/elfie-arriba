// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Query;
using XForm.Transforms;

namespace XForm.Types
{
    public interface IDataBatchComparer
    {
        void SetValue(object value);

        void WhereEquals(DataBatch source, RowRemapper result);
        void WhereNotEquals(DataBatch source, RowRemapper result);
        void WhereLessThan(DataBatch source, RowRemapper result);
        void WhereLessThanOrEquals(DataBatch source, RowRemapper result);
        void WhereGreaterThan(DataBatch source, RowRemapper result);
        void WhereGreaterThanOrEquals(DataBatch source, RowRemapper result);
    }

    public static class DataBatchComparerExtensions
    {
        public static Action<DataBatch, RowRemapper> TryBuild(this IDataBatchComparer comparer, CompareOperator cOp, object value)
        {
            // Set (and cast) the value to compare against
            comparer.SetValue(value);

            // Return the function for the desired comparison operation
            switch (cOp)
            {
                case CompareOperator.Equals:
                    return comparer.WhereEquals;
                case CompareOperator.NotEquals:
                    return comparer.WhereNotEquals;
                case CompareOperator.GreaterThan:
                    return comparer.WhereGreaterThan;
                case CompareOperator.GreaterThanOrEqual:
                    return comparer.WhereGreaterThanOrEquals;
                case CompareOperator.LessThan:
                    return comparer.WhereLessThan;
                case CompareOperator.LessThanOrEqual:
                    return comparer.WhereLessThanOrEquals;
                default:
                    throw new NotImplementedException(cOp.ToString());
            }
        }
    }
}

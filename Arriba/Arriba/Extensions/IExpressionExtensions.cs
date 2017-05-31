// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Expressions;

namespace Arriba.Extensions
{
    public static class IExpressionExtensions
    {
        /// <summary>
        ///  Return a list of all TermExpressions in the given Expression matching the provided
        ///  columnName, if any.
        /// </summary>
        /// <param name="expression">Expression to traverse</param>
        /// <param name="columnName">ColumnName to match, Null/Empty to include all</param>
        /// <returns>List of TermExpressions which match ColumnName</returns>
        public static IList<TermExpression> GetAllTerms(this IExpression expression, string columnName)
        {
            List<TermExpression> result = new List<TermExpression>();
            GetAllTerms(expression, columnName, result);
            return result;
        }

        private static void GetAllTerms(IExpression expression, string columnName, List<TermExpression> result)
        {
            if (expression is AllExceptColumnsTermExpression)
            {
                AllExceptColumnsTermExpression te = (AllExceptColumnsTermExpression)expression;
                if (!te.RestrictedColumns.Contains(columnName)) result.Add(te);
            }
            else if (expression is TermExpression)
            {
                TermExpression te = (TermExpression)expression;

                if (String.IsNullOrEmpty(columnName) || te.ColumnName.Equals("*") || String.Equals(columnName, te.ColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(te);
                }
            }

            foreach (IExpression child in expression.Children())
            {
                GetAllTerms(child, columnName, result);
            }
        }
    }
}

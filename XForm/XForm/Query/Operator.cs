// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace XForm.Query
{
    public enum CompareOperator : byte
    {
        Equals = 0,
        NotEquals = 1,
        LessThan = 2,
        LessThanOrEqual = 3,
        GreaterThan = 4,
        GreaterThanOrEqual = 5
    }

    public enum BooleanOperator : byte
    {
        Set = 0,
        And = 1,
        Or = 2
    }

    public static class OperatorExtensions
    {
        public static CompareOperator ParseCompareOperator(this string op)
        {
            switch (op)
            {
                case "<":
                    return CompareOperator.LessThan;
                case "<=":
                    return CompareOperator.LessThanOrEqual;
                case ">":
                    return CompareOperator.GreaterThan;
                case ">=":
                    return CompareOperator.GreaterThanOrEqual;
                case "=":
                case "==":
                    return CompareOperator.Equals;
                case "!=":
                case "<>":
                    return CompareOperator.NotEquals;
                default:
                    throw new NotImplementedException($"XForm doesn't know CompareOperator \"{op}\".");
            }
        }
    }
}

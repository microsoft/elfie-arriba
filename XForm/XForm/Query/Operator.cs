// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

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
        public static IEnumerable<string> ValidCompareOperators = new string[] { "<", "<=", ">", ">=", "=", "==", "!=", "<>" };

        public static bool TryParseCompareOperator(this string op, out CompareOperator result)
        {
            switch (op)
            {
                case "<":
                    result = CompareOperator.LessThan;
                    break;
                case "<=":
                    result = CompareOperator.LessThanOrEqual;
                    break;
                case ">":
                    result = CompareOperator.GreaterThan;
                    break;
                case ">=":
                    result = CompareOperator.GreaterThanOrEqual;
                    break;
                case "=":
                case "==":
                    result = CompareOperator.Equals;
                    break;
                case "!=":
                case "<>":
                    result = CompareOperator.NotEquals;
                    break;
                default:
                    result = CompareOperator.Equals;
                    return false;
            }

            return true;
        }

        public static bool TryInvertCompareOperator(this CompareOperator op, out CompareOperator inverse)
        {
            switch(op)
            {
                case CompareOperator.Equals:
                    inverse = CompareOperator.NotEquals;
                    return true;
                case CompareOperator.NotEquals:
                    inverse = CompareOperator.Equals;
                    return true;
                case CompareOperator.LessThan:
                    inverse = CompareOperator.GreaterThanOrEqual;
                    return true;
                case CompareOperator.LessThanOrEqual:
                    inverse = CompareOperator.GreaterThan;
                    return true;
                case CompareOperator.GreaterThan:
                    inverse = CompareOperator.LessThanOrEqual;
                    return true;
                case CompareOperator.GreaterThanOrEqual:
                    inverse = CompareOperator.LessThan;
                    return true;
                default:
                    // No operators yet which can't be inverted, but 'contains', and 'startsWith' can't be.
                    inverse = op;
                    return false;
            }
        }
    }
}

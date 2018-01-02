// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace XForm.Query
{
    // WARNING: Values must stay in sync with XFormNative.Operator

    public enum CompareOperator : byte
    {
        Equal = 0,
        NotEqual = 1,
        LessThan = 2,
        LessThanOrEqual = 3,
        GreaterThan = 4,
        GreaterThanOrEqual = 5
    }

    public enum BooleanOperator : byte
    {
        And = 0,
        Or = 1
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
                    result = CompareOperator.Equal;
                    break;
                case "!=":
                case "<>":
                    result = CompareOperator.NotEqual;
                    break;
                default:
                    result = CompareOperator.Equal;
                    return false;
            }

            return true;
        }

        public static bool TryInvertCompareOperator(this CompareOperator op, out CompareOperator inverse)
        {
            switch (op)
            {
                case CompareOperator.Equal:
                    inverse = CompareOperator.NotEqual;
                    return true;
                case CompareOperator.NotEqual:
                    inverse = CompareOperator.Equal;
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

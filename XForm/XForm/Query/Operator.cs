// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        GreaterThanOrEqual = 5,
        Contains = 6,
        ContainsExact = 7,
        StartsWith = 8,
        EndsWith = 9
    }

    public enum BooleanOperator : byte
    {
        And = 0,
        Or = 1
    }

    public static class OperatorExtensions
    {
        public static IEnumerable<string> ValidCompareOperators = new string[] { "<", "<=", ">", ">=", "=", "==", "!=", "<>", ":", "::", "|>", ">|" };
        public static IEnumerable<string> ValidBooleanOperators = new string[] { "AND", "&&", "&", "OR", "||", "|" };

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
                case ":":
                    result = CompareOperator.Contains;
                    break;
                case "::":
                    result = CompareOperator.ContainsExact;
                    break;
                case "|>":
                    result = CompareOperator.StartsWith;
                    break;
                case ">|":
                    result = CompareOperator.EndsWith;
                    break;
                default:
                    result = CompareOperator.Equal;
                    return false;
            }

            return true;
        }

        public static string ToQueryForm(this CompareOperator op)
        {
            switch (op)
            {
                case CompareOperator.Equal:
                    return "=";
                case CompareOperator.NotEqual:
                    return "!=";
                case CompareOperator.LessThan:
                    return "<";
                case CompareOperator.LessThanOrEqual:
                    return "<=";
                case CompareOperator.GreaterThan:
                    return ">";
                case CompareOperator.GreaterThanOrEqual:
                    return ">=";
                case CompareOperator.Contains:
                    return ":";
                case CompareOperator.ContainsExact:
                    return "::";
                case CompareOperator.StartsWith:
                    return "|>";
                case CompareOperator.EndsWith:
                    return ">|";
                default:
                    throw new NotImplementedException(op.ToString());
            }
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
                    // Can't invert other operators (Contains, ContainsExact, StartsWith)
                    inverse = op;
                    return false;
            }
        }

        public static bool TryParseBooleanOperator(this string op, out BooleanOperator result)
        {
            switch (op.ToUpperInvariant())
            {
                case "AND":
                case "&&":
                case "&":
                    result = BooleanOperator.And;
                    break;
                case "OR":
                case "||":
                case "|":
                    result = BooleanOperator.Or;
                    break;
                default:
                    result = BooleanOperator.And;
                    return false;
            }

            return true;
        }

        public static string ToQueryForm(this BooleanOperator op)
        {
            switch (op)
            {
                case BooleanOperator.And:
                    return "AND";
                case BooleanOperator.Or:
                    return "OR";
                default:
                    throw new NotImplementedException(op.ToString());
            }
        }

        public static bool TryParseNot(this string op)
        {
            switch (op.ToUpperInvariant())
            {
                case "NOT":
                case "!":
                    return true;
                default:
                    return false;
            }
        }
    }
}

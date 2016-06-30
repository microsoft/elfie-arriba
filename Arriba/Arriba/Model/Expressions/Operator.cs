// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba.Model.Expressions
{
    /// <summary>
    ///  Operators for expression clauses.
    /// </summary>
    public enum Operator : byte
    {
        Equals = 0,
        NotEquals,
        StartsWith,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        In,
        Matches,
        MatchesExact
    }

    public static class OperatorExtensions
    {
        public static string ToSyntaxString(this Operator op)
        {
            switch (op)
            {
                case Operator.Equals:
                    return " = ";
                case Operator.GreaterThan:
                    return " > ";
                case Operator.GreaterThanOrEqual:
                    return " >= ";
                case Operator.In:
                    return " IN ";
                case Operator.LessThan:
                    return " < ";
                case Operator.LessThanOrEqual:
                    return " <= ";
                case Operator.Matches:
                    return ":";
                case Operator.MatchesExact:
                    return "::";
                case Operator.NotEquals:
                    return " <> ";
                case Operator.StartsWith:
                    return " STARTSWITH ";
                default:
                    return op.ToString();
            }
        }

        /// <summary>
        ///  Return operators safe to correct with 'OrExpression' conversions. These are
        ///  equals and match operators. For range operators or not equals, OrExpression
        ///  corrections are not correct.
        ///  
        ///  Ex: 
        ///   'a = scottlo'  -> '(a = scottlo || a = "Scott Louvau")   [Good]
        ///   'a != scottlo' -> '(a != scottlo || a != "Scott Louvau") [Bad - would match everything.]
        ///   'a > scottlo'  -> '(a > scottlo || a > "Scott Louvau")   [Bad - messes up range.]
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public static bool IsOrCorrectableOperator(this Operator op)
        {
            return op == Operator.Equals || op == Operator.StartsWith || op == Operator.Matches || op == Operator.MatchesExact;
        }

        public static bool TryEvaluate<T>(this T itemValue, Operator op, T value, out bool result) where T : IComparable<T>
        {
            int cmp = itemValue.CompareTo(value);

            switch (op)
            {
                case Operator.Equals:
                    result = (cmp == 0);
                    break;
                case Operator.NotEquals:
                    result = (cmp != 0);
                    break;
                case Operator.StartsWith:
                    result = value.ToString().StartsWith(itemValue.ToString(), StringComparison.Ordinal);
                    break;
                case Operator.LessThan:
                    result = (cmp < 0);
                    break;
                case Operator.LessThanOrEqual:
                    result = (cmp <= 0);
                    break;
                case Operator.GreaterThan:
                    result = (cmp > 0);
                    break;
                case Operator.GreaterThanOrEqual:
                    result = (cmp >= 0);
                    break;
                default:
                    // Other operators are not supported
                    result = false;
                    return false;
            }

            return true;
        }
    }
}

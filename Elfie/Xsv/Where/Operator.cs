// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Xsv.Where
{
    /// <summary>
    ///  Operator describes a comparison operator using a flags enum,
    ///  with Equal, LessThan, and GreaterThan as distinct parts. This
    ///  means LessThanOrEqual can be LessThan | Equal and NotEquals can
    ///  be LessThan | GreaterThan.
    /// </summary>
    [Flags]
    public enum Operator : byte
    {
        Equals = 0x1,
        LessThan = 0x2,
        GreaterThan = 0x4,
        NotEquals = LessThan | GreaterThan,
        LessThanOrEqual = LessThan | Equals,
        GreaterThanOrEqual = GreaterThan | Equals,
        StartsWith = 0x8,
        Contains = 0xF
    }

    public static class OperatorExtensions
    {
        public static Operator Parse(string value)
        {
            if (String.IsNullOrEmpty(value)) throw new UsageException("Operator was null");

            switch (value)
            {
                case "=":
                case "==":
                    return Operator.Equals;
                case "!=":
                case "<>":
                    return Operator.NotEquals;
                case "<":
                    return Operator.LessThan;
                case "<=":
                    return Operator.LessThanOrEqual;
                case ">":
                    return Operator.GreaterThan;
                case ">=":
                    return Operator.GreaterThanOrEqual;
                case "|>":
                    return Operator.StartsWith;
                case ":":
                    return Operator.Contains;
                default:
                    throw new UsageException($"Operator '{value}' is not a known operator.");
            }
        }

        /// <summary>
        ///  Validate whether a left.CompareTo(right) return value matches the given operator.
        /// </summary>
        /// <param name="op">Operator to match</param>
        /// <param name="compareToResult">left.CompareTo(right) result</param>
        /// <returns>True if matching, False otherwise</returns>
        public static bool Matches(this Operator op, int compareToResult)
        {
            // If compareTo was negative, we match if the operator included the 'LessThan' flag
            // [LessThan, LessThanOrEqual, NotEqual]
            if (compareToResult < 0) return (op & Operator.LessThan) != 0;

            // If compareTo was positive, we match if the operator included the 'GreaterThan' flag
            // [GreaterThan, GreaterThanOrEqual, NotEqual]
            if (compareToResult > 0) return (op & Operator.GreaterThan) != 0;

            // Otherwise, match if the operator included the 'Equal' flag
            // [Equal, LessThanOrEqual, GreaterThanOrEqual]
            return (op & Operator.Equals) != 0;
        }
    }
}

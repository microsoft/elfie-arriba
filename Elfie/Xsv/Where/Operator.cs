using System;
using XsvConcat;

namespace Xsv.Where
{
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

            switch(value)
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

        public static bool Matches(this Operator op, int compareToResult)
        {
            if (compareToResult < 0) return (op & Operator.LessThan) != 0;
            if (compareToResult > 0) return (op & Operator.GreaterThan) != 0;
            return (op & Operator.Equals) != 0;
        }
    }
}

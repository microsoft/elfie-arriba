namespace V5.Query
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
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
}

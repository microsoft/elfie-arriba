// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using XForm.Data;

namespace XForm.Query.Expression
{
    public interface IExpression
    {
        void Evaluate(BitVector result);
    }
}

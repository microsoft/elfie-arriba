// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using XForm.Data;

namespace XForm.Query.Expression
{
    internal class NotExpression : IExpression
    {
        private IExpression _inner;

        public NotExpression(IExpression inner)
        {
            _inner = inner;
        }

        public void Evaluate(BitVector vector)
        {
            _inner.Evaluate(vector);

            // Issue: This isn't correct; we need the vector count, not capacity. Currently fixed in Where.Next() with ClearAbove().
            vector.Not(vector.Capacity);
        }

        public override string ToString()
        {
            return $"NOT({_inner})";
        }
    }
}

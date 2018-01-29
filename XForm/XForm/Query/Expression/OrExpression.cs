// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

using XForm.Data;

namespace XForm.Query.Expression
{
    internal class OrExpression : IExpression
    {
        private IExpression[] _terms;
        private BitVector _termVector;

        public OrExpression(IExpression[] terms)
        {
            _terms = terms;
        }

        public void Evaluate(BitVector vector)
        {
            Allocator.AllocateToSize(ref _termVector, vector.Capacity);

            foreach (IExpression term in _terms)
            {
                _termVector.None();
                term.Evaluate(_termVector);
                vector.Or(_termVector);
            }
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            foreach (IExpression term in _terms)
            {
                if (result.Length > 0) result.Append(" OR ");
                result.Append(term);
            }

            return result.ToString();
        }
    }
}

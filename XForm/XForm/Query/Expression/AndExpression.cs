// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

using XForm.Data;

namespace XForm.Query.Expression
{
    internal class AndExpression : IExpression
    {
        private IExpression[] _terms;
        private BitVector _termVector;

        public AndExpression(IExpression[] terms)
        {
            _terms = terms;
        }

        public void Evaluate(BitVector vector)
        {
            Allocator.AllocateToSize(ref _termVector, vector.Capacity);
            vector.All(vector.Capacity);

            foreach (IExpression term in _terms)
            {
                _termVector.None();
                term.Evaluate(_termVector);
                vector.And(_termVector);
            }
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            foreach (IExpression term in _terms)
            {
                if (result.Length > 0) result.Append(" AND ");

                if (term is OrExpression)
                {
                    // Explicit parenthesis are only required on an OR expression inside and AND expression.
                    // AND takes precedence over OR, so A OR B AND C OR D => (A OR (B AND C) OR D)
                    result.Append("(");
                    result.Append(term);
                    result.Append(")");
                }
                else
                {
                    result.Append(term);
                }
            }

            return result.ToString();
        }
    }
}

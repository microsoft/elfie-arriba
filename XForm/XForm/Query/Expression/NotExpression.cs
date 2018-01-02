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
            vector.Not(vector.Capacity);
        }

        public override string ToString()
        {
            return $"NOT({_inner})";
        }
    }
}

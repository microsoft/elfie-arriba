using XForm.Data;

namespace XForm.Query.Expression
{
    public interface IExpression
    {
        void Evaluate(BitVector result);
    }
}

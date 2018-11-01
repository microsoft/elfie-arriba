using XForm.Data;

namespace XForm.Types
{
    public interface IXArrayComputer
    {
        XArray Add(XArray left, XArray right);
        XArray Subtract(XArray left, XArray right);
        XArray Multiply(XArray left, XArray right);
        XArray Divide(XArray left, XArray right);
    }
}

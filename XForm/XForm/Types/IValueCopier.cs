namespace XForm.Types
{
    /// <summary>
    ///  IValueCopier makes copies of values which can't just be copied by assignment.
    ///  [String8]
    /// </summary>
    public interface IValueCopier
    {
        void Reset();
    }

    public interface IValueCopier<T> : IValueCopier
    {
        T Copy(T value);
    }

}

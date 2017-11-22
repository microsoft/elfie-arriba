using System;

namespace XForm.Extensions
{
    public static class IComparableExtensions
    {
        public static T BiggestOf<T>(this T left, T right) where T : IComparable<T>
        {
            return left.CompareTo(right) > 0 ? left : right;
        }
    }
}

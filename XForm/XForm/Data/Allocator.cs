namespace XForm.Data
{
    public static class Allocator
    {
        public static void AllocateToSize<T>(ref T[] array, int minimumSize)
        {
            if (array == null || array.Length < minimumSize) array = new T[minimumSize];
        }
    }
}

namespace XForm.Generator
{
    public class SupportedTypes
    {
        public static string[] PrimitiveTypes = new string[] { "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double" };
        public static string[] AdditionalTypes = new string[] { "DateTime", "TimeSpan", "String8" };

        // Guid doesn't support direct compare operators ('<', '<=', etc) and so must go through IComparable<T>.
    }
}

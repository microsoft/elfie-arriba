// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XForm.Generator
{
    public class SupportedTypes
    {
        public static string[] PrimitiveTypes = new string[] { "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double" };
        public static string[] AdditionalTypes = new string[] { "DateTime", "TimeSpan", "String8" };

        public static string[] UnsignedNumbersInOrder = new string[] { "byte", "ushort", "uint", "ulong" };
        public static string[] SignedNumbersInOrder = new string[] { "sbyte", "short", "int", "long" };
        public static string[] FloatingPointInOrder = new string[] { "float", "double" };

        // Guid doesn't support direct compare operators ('<', '<=', etc) and so must go through IComparable<T>.

        public static string ToClassName(string typeName)
        {
            return char.ToUpperInvariant(typeName[0]) + typeName.Substring(1);
        }

        public static bool IsUnsigned(string className)
        {
            return className.StartsWith("u") || className.Equals("byte");
        }
    }
}

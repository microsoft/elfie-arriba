using System;
using V5.Serialization;

namespace V5.Data
{
    public static class ColumnFactory
    {
        public static string GetTypeIdentifier(Type type)
        {
            if (type == typeof(bool)) return "b1";
            if (type == typeof(byte)) return "b8";
            if (type == typeof(int)) return "i32";
            if (type == typeof(long)) return "i64";
            throw new ArgumentOutOfRangeException($"ColumnFactory does not support type \"{type.Name}\".");
        }

        public static Type GetType(string identifier)
        {
            switch(identifier.ToLowerInvariant())
            {
                case "b1":
                    return typeof(bool);
                case "b8":
                    return typeof(byte);
                case "i32":
                    return typeof(int);
                case "i64":
                    return typeof(long);
                default:
                    throw new ArgumentOutOfRangeException($"ColumnFactory does not support type identifier \"{identifier}\".");
            }
        }

        public static IColumn Build(string columnName, string typeIdentifier, string parentIdentifier, CachedLoader loader)
        {
            switch (typeIdentifier.ToLowerInvariant())
            {
                case "b8":
                    return new PrimitiveColumn<byte>(columnName, typeIdentifier, parentIdentifier, loader);
                case "i32":
                    return new PrimitiveColumn<int>(columnName, typeIdentifier, parentIdentifier, loader);
                case "i64":
                    return new PrimitiveColumn<long>(columnName, typeIdentifier, parentIdentifier, loader);
                default:
                    throw new ArgumentOutOfRangeException($"ColumnFactory does not support type identifier \"{typeIdentifier}\".");
            }
        }
    }
}

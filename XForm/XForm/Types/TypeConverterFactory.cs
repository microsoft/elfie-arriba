// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;

namespace XForm.Types
{
    public static class TypeConverterFactory
    {
        public static Func<DataBatch, DataBatch> GetConverter(Type sourceType, Type targetType, object defaultValue, bool strict)
        {
            // Convert the defaultValue to the right type
            defaultValue = ConvertSingle(defaultValue, targetType);

            Func<DataBatch, DataBatch> converter = null;

            // See if the target type provides conversion
            ITypeProvider targetTypeProvider = TypeProviderFactory.TryGet(targetType);
            if (targetTypeProvider != null)
            {
                converter = targetTypeProvider.TryGetConverter(sourceType, targetType, defaultValue, strict);
                if (converter != null) return converter;
            }

            // See if the source type provides conversion
            ITypeProvider sourceTypeProvider = TypeProviderFactory.TryGet(sourceType);
            if (sourceTypeProvider != null)
            {
                converter = sourceTypeProvider.TryGetConverter(sourceType, targetType, defaultValue, strict);
                if (converter != null) return converter;
            }

            throw new NotImplementedException($"No converter available from {sourceType.Name} to {targetType.Name}.");
        }

        public static object ConvertSingle(object value, Type targetType)
        {
            if (value == null) return null;
            if (value.GetType().Equals(targetType)) return value;
            if (value is string) return ConvertSingle((string)value, targetType);
            if (value is String8) return ConvertSingle((String8)value, targetType);

            throw new NotImplementedException($"XForm doesn't know how to convert \"{value}\" (type {value.GetType().Name}) to {targetType.Name}.");
        }

        public static object ConvertSingle(string value, Type targetType)
        {
            if (value == null) return null;
            if (targetType == typeof(string)) return value;
            return ConvertSingle(String8.Convert(value, new byte[String8.GetLength(value)]), targetType);
        }

        public static object ConvertSingle(String8 value, Type targetType)
        {
            if (value == null) return null;
            if (targetType == typeof(String8)) return value;
            Func<DataBatch, DataBatch> converter = GetConverter(typeof(String8), targetType, null, true);

            DataBatch result = converter(DataBatch.Single(new String8[] { value }, 1));

            // Verify the result was not null unless the input was "" or 'null'
            if (result.IsNull != null && result.IsNull[0] == true)
            {
                if (value.IsEmpty() || value.CompareTo("null", true) == 0) return null;
                throw new ArgumentException($"Could not convert \"{value}\" to {targetType.Name}.");
            }

            return result.Array.GetValue(0);
        }
    }
}

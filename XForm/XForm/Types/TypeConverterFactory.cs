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

            throw new ArgumentException($"No converter available from {sourceType.Name} to {targetType.Name}.");
        }

        public static object ConvertSingle(object value, Type targetType)
        {
            // Nulls are always converted to null
            if (value == null) return null;

            // If the type is already right, just return it
            Type sourceType = value.GetType();
            if (sourceType.Equals(targetType)) return value;

            // Until we have a StringTypeProvider, convert string to String8 for conversions
            if (sourceType == typeof(string))
            {
                sourceType = typeof(String8);
                value = String8.Convert((string)value, new byte[String8.GetLength((string)value)]);

                if (targetType == typeof(String8)) return value;
            }

            // Get the converter for the desired type combination
            Func<DataBatch, DataBatch> converter = GetConverter(sourceType, targetType, null, true);

            Array array = Allocator.AllocateArray(sourceType, 1);
            array.SetValue(value, 0);

            DataBatch result = converter(DataBatch.Single(array, 1));

            // Verify the result was not null unless the input was "" or 'null'
            if (result.IsNull != null && result.IsNull[0] == true)
            {
                string stringValue = value.ToString();
                if (stringValue != "" || String.Compare(stringValue, "null", true) == 0) return null;
                throw new ArgumentException($"Could not convert \"{value}\" to {targetType.Name}.");
            }

            return result.Array.GetValue(0);

            throw new ArgumentException($"No converter available for value \"{value}\" (type {value.GetType().Name}) to {targetType.Name}.");
        }
    }
}

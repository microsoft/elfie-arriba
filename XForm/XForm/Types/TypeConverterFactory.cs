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
            Func<DataBatch, DataBatch> converter = TryGetConverter(sourceType, targetType, defaultValue, strict);
            if(converter == null) throw new ArgumentException($"No converter available from {sourceType.Name} to {targetType.Name}.");
            return converter;
        }

        public static Func<DataBatch, DataBatch> TryGetConverter(Type sourceType, Type targetType, object defaultValue, bool strict)
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

            // Try again with implicit string to String8 conversion
            if(sourceType == typeof(string))
            {
                converter = TryGetConverter(typeof(String8), targetType, defaultValue, strict);

                // If found, encode the string to String8 conversion and then the String8 to target conversion
                if (converter != null)
                {
                    Func<DataBatch, DataBatch> innerConverter = GetConverter(typeof(string), typeof(String8), defaultValue, strict);
                    return (batch) => converter(innerConverter(batch));
                }
            }

            return null;
        }

        public static object ConvertSingle(object value, Type targetType)
        {
            object result;
            if (!TryConvertSingle(value, targetType, out result)) throw new ArgumentException($"Could not convert \"{value}\" to {targetType.Name}.");
            return result;
        }

        public static bool TryConvertSingle(object value, Type targetType, out object result)
        {
            // Nulls are always converted to null
            if (value == null)
            {
                result = null;
                return true;
            }

            // If the type is already right, just return it
            Type sourceType = value.GetType();
            if (sourceType.Equals(targetType))
            {
                result = value;
                return true;
            }

            // Get the converter for the desired type combination
            Func<DataBatch, DataBatch> converter = GetConverter(sourceType, targetType, null, true);

            Array array = Allocator.AllocateArray(sourceType, 1);
            array.SetValue(value, 0);

            DataBatch resultBatch = converter(DataBatch.Single(array, 1));

            // Verify the result was not null unless the input was "" or 'null'
            if (resultBatch.IsNull != null && resultBatch.IsNull[0] == true)
            {
                result = null;

                string stringValue = value.ToString();
                if (stringValue != "" || String.Compare(stringValue, "null", true) == 0) return true;
                return false;
            }

            result = resultBatch.Array.GetValue(0);
            return true;

            throw new ArgumentException($"No converter available for value \"{value}\" (type {value.GetType().Name}) to {targetType.Name}.");
        }
    }
}

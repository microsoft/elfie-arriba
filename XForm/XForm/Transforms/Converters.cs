using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using XForm.Data;

namespace XForm.Transforms
{
    public static class TypeConverterFactory
    {
        public static Func<DataBatch, DataBatch> Build(Type sourceType, Type targetType, object defaultValue, bool strict)
        {
            // Convert the defaultValue to the right type
            defaultValue = ConvertSingle(defaultValue, targetType);

            // Build a converter for the set of types
            if (targetType == typeof(String8))
            {
                ToString8Converter converter = new ToString8Converter();
                if (sourceType == typeof(int)) return converter.IntegerToString8;
                if (sourceType == typeof(DateTime)) return converter.DateTimeToString8;
                if (sourceType == typeof(bool)) return converter.BooleanToString8;
            }
            else if (sourceType == typeof(String8))
            {
                if (targetType == typeof(int))
                {
                    String8ToIntegerConverter converter = new String8ToIntegerConverter(defaultValue);
                    if (strict) return converter.ConvertOrThrow;
                    return converter.ConvertOrDefault;
                }
                else if (targetType == typeof(DateTime))
                {
                    String8ToDateTimeConverter converter = new String8ToDateTimeConverter(defaultValue);
                    if (strict) return converter.ConvertOrThrow;
                    return converter.ConvertOrDefault;
                }
                else if (targetType == typeof(bool))
                {
                    String8ToBooleanConverter converter = new String8ToBooleanConverter(defaultValue);
                    if (strict) return converter.ConvertOrThrow;
                    return converter.ConvertOrDefault;
                }
            }

            // TODO: Extensibility like Elfie Readers and Writers

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
            Func<DataBatch, DataBatch> converter = Build(typeof(String8), targetType, null, true);
            return converter(DataBatch.All(new String8[] { value })).Array.GetValue(0);
        }
    }

    internal class ToString8Converter
    {
        private String8[] _stringArray;
        private byte[] _buffer;

        private void ClearAndSize(int length, int bytesPerItem)
        {
            if (_stringArray == null || _stringArray.Length < length) _stringArray = new String8[length];
            if (bytesPerItem > 0 && (_buffer == null || _buffer.Length < length * bytesPerItem)) _buffer = new byte[length * bytesPerItem];
        }

        public DataBatch IntegerToString8(DataBatch batch)
        {
            ClearAndSize(batch.Count, 12);
            int bufferBytesUsed = 0;

            int[] sourceArray = (int[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _stringArray[i] = String8.FromInteger(sourceArray[batch.Index(i)], _buffer, bufferBytesUsed);
                bufferBytesUsed += _stringArray[i].Length;
            }

            return DataBatch.All(_stringArray, batch.Count);
        }

        public DataBatch DateTimeToString8(DataBatch batch)
        {
            ClearAndSize(batch.Count, 20);
            int bufferBytesUsed = 0;

            DateTime[] sourceArray = (DateTime[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _stringArray[i] = String8.FromDateTime(sourceArray[batch.Index(i)], _buffer, bufferBytesUsed);
                bufferBytesUsed += _stringArray[i].Length;
            }

            return DataBatch.All(_stringArray, batch.Count);
        }

        public DataBatch BooleanToString8(DataBatch batch)
        {
            ClearAndSize(batch.Count, 0);

            bool[] sourceArray = (bool[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _stringArray[i] = String8.FromBoolean(sourceArray[batch.Index(i)]);
            }

            return DataBatch.All(_stringArray, batch.Count);
        }
    }

    internal class String8ToIntegerConverter
    {
        private int[] _array;
        private int _default;

        public String8ToIntegerConverter(object defaultValue)
        {
            _default = (int)(defaultValue ?? default(int));
        }

        public DataBatch ConvertOrDefault(DataBatch batch)
        {
            if (_array == null || _array.Length < batch.Count) _array = new int[batch.Count];

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToInteger(out _array[i])) _array[i] = _default;
            }

            return DataBatch.All(_array, batch.Count);
        }

        public DataBatch ConvertOrThrow(DataBatch batch)
        {
            if (_array == null || _array.Length < batch.Count) _array = new int[batch.Count];

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToInteger(out _array[i])) throw new InvalidCastException($"Unable to cast String8 value \"{sourceArray[batch.Index(i)]}\" to an integer.");
            }

            return DataBatch.All(_array, batch.Count);
        }
    }

    internal class String8ToDateTimeConverter
    {
        private DateTime[] _array;
        private DateTime _default;

        public String8ToDateTimeConverter(object defaultValue)
        {
            _default = (DateTime)(defaultValue ?? default(DateTime));
        }

        public DataBatch ConvertOrDefault(DataBatch batch)
        {
            if (_array == null || _array.Length < batch.Count) _array = new DateTime[batch.Count];

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToDateTime(out _array[i])) _array[i] = _default;
            }

            return DataBatch.All(_array, batch.Count);
        }

        public DataBatch ConvertOrThrow(DataBatch batch)
        {
            if (_array == null || _array.Length < batch.Count) _array = new DateTime[batch.Count];

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToDateTime(out _array[i])) throw new InvalidCastException($"Unable to cast String8 value \"{sourceArray[batch.Index(i)]}\" to a DateTime.");
            }

            return DataBatch.All(_array, batch.Count);
        }
    }

    internal class String8ToBooleanConverter
    {
        private bool[] _array;
        private bool _default;

        public String8ToBooleanConverter(object defaultValue)
        {
            _default = (bool)(defaultValue ?? default(bool));
        }

        public DataBatch ConvertOrDefault(DataBatch batch)
        {
            if (_array == null || _array.Length < batch.Count) _array = new bool[batch.Count];

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToBoolean(out _array[i])) _array[i] = _default;
            }

            return DataBatch.All(_array, batch.Count);
        }

        public DataBatch ConvertOrThrow(DataBatch batch)
        {
            if (_array == null || _array.Length < batch.Count) _array = new bool[batch.Count];

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToBoolean(out _array[i])) throw new InvalidCastException($"Unable to cast String8 value \"{sourceArray[batch.Index(i)]}\" to a boolean.");
            }

            return DataBatch.All(_array, batch.Count);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Types.Comparers;

namespace XForm.Types
{
    public class TimeSpanTypeProvider : ITypeProvider
    {
        public string Name => "TimeSpan";
        public Type Type => typeof(TimeSpan);

        public IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath, bool requireCached)
        {
            return new ConvertingReader(TypeProviderFactory.Get(typeof(long)).BinaryReader(streamProvider, columnPath, requireCached), TypeConverterFactory.GetConverter(typeof(long), typeof(TimeSpan)));
        }

        public IColumnWriter BinaryWriter(IStreamProvider streamProvider, string columnPath)
        {
            return new ConvertingWriter(TypeProviderFactory.Get(typeof(long)).BinaryWriter(streamProvider, columnPath), TypeConverterFactory.GetConverter(typeof(TimeSpan), typeof(long)));
        }

        public IXArrayComparer TryGetComparer()
        {
            // TimeSpanComparer is generated
            return new TimeSpanComparer();
        }

        public NegatedTryConvert TryGetNegatedTryConvert(Type sourceType, Type targetType, object defaultValue)
        {
            if (sourceType == typeof(TimeSpan) && targetType == typeof(long))
            {
                return new TimeSpanConverter(null).TimeSpanToLong;
            }
            else if (sourceType == typeof(long) && targetType == typeof(TimeSpan))
            {
                return new TimeSpanConverter(null).LongToTimeSpan;
            }
            else if (sourceType == typeof(string) && targetType == typeof(TimeSpan))
            {
                return new TimeSpanConverter(defaultValue).StringToTimeSpan;
            }

            return null;
        }

        public IValueCopier TryGetCopier()
        {
            // No copier needed for this type
            return null;
        }
    }

    /// <summary>
    ///  TimeSpanConverter converts TimeSpans to longs and back by getting the UTC Ticks long of the TimeSpan.
    /// </summary>
    public class TimeSpanConverter
    {
        private TimeSpan _defaultValue;

        private TimeSpan[] _timeSpanArray;
        private long[] _longArray;
        private bool[] _couldNotConvertArray;

        public TimeSpanConverter(object defaultValue)
        {
            _defaultValue = (TimeSpan)(TypeConverterFactory.ConvertSingle(defaultValue, typeof(TimeSpan)) ?? default(TimeSpan));
        }

        public bool[] TimeSpanToLong(XArray xarray, out Array result)
        {
            Allocator.AllocateToSize(ref _longArray, xarray.Count);

            TimeSpan[] sourceArray = (TimeSpan[])xarray.Array;
            for (int i = 0; i < xarray.Count; ++i)
            {
                _longArray[i] = sourceArray[xarray.Index(i)].Ticks;
            }

            result = _longArray;
            return null;
        }

        public bool[] LongToTimeSpan(XArray xarray, out Array result)
        {
            Allocator.AllocateToSize(ref _timeSpanArray, xarray.Count);

            long[] sourceArray = (long[])xarray.Array;
            for (int i = 0; i < xarray.Count; ++i)
            {
                _timeSpanArray[i] = TimeSpan.FromTicks(sourceArray[xarray.Index(i)]);
            }

            result = _timeSpanArray;
            return null;
        }

        public bool[] StringToTimeSpan(XArray xarray, out Array result)
        {
            Allocator.AllocateToSize(ref _timeSpanArray, xarray.Count);
            Allocator.AllocateToSize(ref _couldNotConvertArray, xarray.Count);

            bool anyCouldNotConvert = false;
            string[] sourceArray = (string[])xarray.Array;
            for (int i = 0; i < xarray.Count; ++i)
            {
                string value = sourceArray[xarray.Index(i)];

                bool couldNotConvert = !TryParseTimeSpanFriendly(value, out _timeSpanArray[i]);
                if (couldNotConvert) couldNotConvert = String.IsNullOrEmpty(value) || !TimeSpan.TryParse(value, out _timeSpanArray[i]);
                if (couldNotConvert) _timeSpanArray[i] = _defaultValue;

                _couldNotConvertArray[i] = couldNotConvert;
                anyCouldNotConvert |= couldNotConvert;
            }

            result = _timeSpanArray;
            return (anyCouldNotConvert ? _couldNotConvertArray : null);
        }

        /// <summary>
        ///  Parse a "friendly" TimeSpan value, like 7d, 24h, 5m, 30s.
        /// </summary>
        /// <param name="value">String value to parse as a "friendly" format TimeSpan</param>
        /// <param name="result">TimeSpan equivalent of value</param>
        /// <returns>TimeSpan equivalent to value</returns>
        public static bool TryParseTimeSpanFriendly(string value, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (String.IsNullOrEmpty(value)) return false;

            // Find the portion of the value which is the number part
            int numberPrefixLength = 0;
            for (; numberPrefixLength < value.Length; ++numberPrefixLength)
            {
                if (Char.IsLetter(value[numberPrefixLength])) break;
            }

            double numberPartValue = double.Parse(value.Substring(0, numberPrefixLength));
            string suffix = value.Substring(numberPrefixLength);

            switch (suffix.ToLowerInvariant())
            {
                case "s":
                case "sec":
                    result = TimeSpan.FromSeconds(numberPartValue);
                    break;
                case "m":
                case "min":
                    result = TimeSpan.FromMinutes(numberPartValue);
                    break;
                case "h":
                case "hour":
                    result = TimeSpan.FromHours(numberPartValue);
                    break;
                case "d":
                case "day":
                    result = TimeSpan.FromDays(numberPartValue);
                    break;
                default:
                    //throw new ArgumentException($"Unable to parse \"{value}\" as a friendly TimeSpan. Unit \"{suffix}\" was unknown. Expecting 's', 'm', 'h', 'd'.");
                    return false;
            }

            return true;
        }
    }
}

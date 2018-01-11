// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Query;
using XForm.Types.Comparers;

namespace XForm.Types
{
    public class TimeSpanTypeProvider : ITypeProvider
    {
        public string Name => "TimeSpan";
        public Type Type => typeof(TimeSpan);

        public IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath)
        {
            return new ConvertingReader(TypeProviderFactory.Get(typeof(long)).BinaryReader(streamProvider, columnPath), TryGetConverter(typeof(long), typeof(TimeSpan), null, true));
        }

        public IColumnWriter BinaryWriter(IStreamProvider streamProvider, string columnPath)
        {
            return new ConvertingWriter(TypeProviderFactory.Get(typeof(long)).BinaryWriter(streamProvider, columnPath), TryGetConverter(typeof(TimeSpan), typeof(long), null, true));
        }

        public IDataBatchComparer TryGetComparer()
        {
            // TimeSpanComparer is generated
            return new TimeSpanComparer();
        }

        public Func<DataBatch, DataBatch> TryGetConverter(Type sourceType, Type targetType, object defaultValue, bool strict)
        {
            if (sourceType == typeof(TimeSpan) && targetType == typeof(long))
            {
                return new TimeSpanConverter().TimeSpanToLong;
            }
            else if (sourceType == typeof(long) && targetType == typeof(TimeSpan))
            {
                return new TimeSpanConverter().LongToTimeSpan;
            }
            else if (sourceType == typeof(string) && targetType == typeof(TimeSpan))
            {
                return new TimeSpanConverter().StringToTimeSpan;
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
        private TimeSpan[] _timeSpanArray;
        private long[] _longArray;
        private bool[] _isNullArray;

        public DataBatch TimeSpanToLong(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _longArray, batch.Count);
            Allocator.AllocateToSize(ref _isNullArray, batch.Count);

            bool areAnyNull = false;
            TimeSpan[] sourceArray = (TimeSpan[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int index = batch.Index(i);
                _longArray[i] = sourceArray[index].Ticks;
                _isNullArray[i] = (batch.IsNull != null && batch.IsNull[index]);
                areAnyNull |= _isNullArray[i];
            }

            return DataBatch.All(_longArray, batch.Count, (areAnyNull ? _isNullArray : null));
        }

        public DataBatch LongToTimeSpan(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _timeSpanArray, batch.Count);
            Allocator.AllocateToSize(ref _isNullArray, batch.Count);

            bool areAnyNull = false;
            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int index = batch.Index(i);
                _timeSpanArray[i] = TimeSpan.FromTicks(sourceArray[index]);
                _isNullArray[i] = (batch.IsNull != null && batch.IsNull[index]);
                areAnyNull |= _isNullArray[i];
            }

            return DataBatch.All(_timeSpanArray, batch.Count, (areAnyNull ? _isNullArray : null));
        }

        public DataBatch StringToTimeSpan(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _timeSpanArray, batch.Count);
            Allocator.AllocateToSize(ref _isNullArray, batch.Count);

            bool areAnyNull = false;
            string[] sourceArray = (string[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int index = batch.Index(i);
                bool isNull = (batch.IsNull != null && batch.IsNull[index]);
                if (isNull)
                {
                    _timeSpanArray[i] = TimeSpan.Zero;
                }
                else
                {
                    isNull = !TryParseTimeSpanFriendly(sourceArray[index], out _timeSpanArray[i]);
                    if (isNull) isNull = !TimeSpan.TryParse(sourceArray[index], out _timeSpanArray[i]);
                }

                _isNullArray[i] = (batch.IsNull != null && batch.IsNull[index]);
                areAnyNull |= _isNullArray[i];
            }

            return DataBatch.All(_timeSpanArray, batch.Count, (areAnyNull ? _isNullArray : null));
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

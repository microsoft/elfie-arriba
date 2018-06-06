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

        public IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath, CachingOption requireCached)
        {
            // Cache the converted TimeSpan, not the inner long
            return ColumnCache.Instance.GetOrBuild(columnPath, requireCached, () =>
            {
                return ConvertingReader.Build(
                    TypeProviderFactory.Get(typeof(long)).BinaryReader(streamProvider, columnPath, CachingOption.Never),
                    TypeConverterFactory.GetConverter(typeof(long), typeof(TimeSpan)));
            });
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
    }
}

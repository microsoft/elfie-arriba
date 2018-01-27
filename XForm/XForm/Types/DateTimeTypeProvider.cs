// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Types.Comparers;

namespace XForm.Types
{
    public class DateTimeTypeProvider : ITypeProvider
    {
        public string Name => "DateTime";

        public Type Type => typeof(DateTime);

        public IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath, CachingOption requireCached)
        {
            // Cache the converted DateTime, not the inner long
            return ColumnCache.Instance.GetOrBuild(columnPath, requireCached, () =>
            {
                return ConvertingReader.Build(
                    TypeProviderFactory.Get(typeof(long)).BinaryReader(streamProvider, columnPath, CachingOption.Never),
                    TypeConverterFactory.GetConverter(typeof(long), typeof(DateTime)));
            });
        }

        public IColumnWriter BinaryWriter(IStreamProvider streamProvider, string columnPath)
        {
            return new ConvertingWriter(TypeProviderFactory.Get(typeof(long)).BinaryWriter(streamProvider, columnPath), TypeConverterFactory.GetConverter(typeof(DateTime), typeof(long)));
        }

        public IXArrayComparer TryGetComparer()
        {
            // DateTimeComparer is generated
            return new DateTimeComparer();
        }

        public NegatedTryConvert TryGetNegatedTryConvert(Type sourceType, Type targetType, object defaultValue)
        {
            if (sourceType == typeof(DateTime) && targetType == typeof(long))
            {
                return new DateTimeConverter().DateTimeToLong;
            }
            else if (sourceType == typeof(long) && targetType == typeof(DateTime))
            {
                return new DateTimeConverter().LongToDateTime;
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
    ///  DateTimeConverter converts DateTimes to longs and back by getting the UTC Ticks long of the DateTime.
    /// </summary>
    public class DateTimeConverter
    {
        private DateTime[] _dateTimeArray;
        private long[] _longArray;

        public bool[] DateTimeToLong(XArray xarray, out Array result)
        {
            Allocator.AllocateToSize(ref _longArray, xarray.Count);

            DateTime[] sourceArray = (DateTime[])xarray.Array;
            for (int i = 0; i < xarray.Count; ++i)
            {
                _longArray[i] = sourceArray[xarray.Index(i)].ToUniversalTime().Ticks;
            }

            result = _longArray;
            return null;
        }

        public bool[] LongToDateTime(XArray xarray, out Array result)
        {
            Allocator.AllocateToSize(ref _dateTimeArray, xarray.Count);

            long[] sourceArray = (long[])xarray.Array;
            for (int i = 0; i < xarray.Count; ++i)
            {
                _dateTimeArray[i] = new DateTime(sourceArray[xarray.Index(i)], DateTimeKind.Utc);
            }

            result = _dateTimeArray;
            return null;
        }
    }
}

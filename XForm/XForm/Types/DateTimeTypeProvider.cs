// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Query;
using XForm.Transforms;
using XForm.Types.Comparers;

namespace XForm.Types
{
    public class DateTimeTypeProvider : ITypeProvider
    {
        public string Name => "DateTime";

        public Type Type => typeof(DateTime);

        public IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath)
        {
            return new ConvertingReader(TypeProviderFactory.Get(typeof(long)).BinaryReader(streamProvider, columnPath), TryGetConverter(typeof(long), typeof(DateTime), null, true));
        }

        public IColumnWriter BinaryWriter(IStreamProvider streamProvider, string columnPath)
        {
            return new ConvertingWriter(TypeProviderFactory.Get(typeof(long)).BinaryWriter(streamProvider, columnPath), TryGetConverter(typeof(DateTime), typeof(long), null, true));
        }

        public Action<DataBatch, DataBatch, RowRemapper> TryGetComparer(CompareOperator op)
        {
            return new ComparableComparer<DateTime>().TryBuild(op);
        }

        public Func<DataBatch, DataBatch> TryGetConverter(Type sourceType, Type targetType, object defaultValue, bool strict)
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
    }

    /// <summary>
    ///  DateTimeConverter converts DateTimes to longs and back by getting the UTC Ticks long of the DateTime.
    /// </summary>
    public class DateTimeConverter
    {
        private DateTime[] _dateTimeArray;
        private long[] _longArray;

        public DataBatch DateTimeToLong(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _longArray, batch.Count);

            DateTime[] sourceArray = (DateTime[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _longArray[i] = sourceArray[batch.Index(i)].ToUniversalTime().Ticks;
            }

            return DataBatch.All(_longArray, batch.Count);
        }

        public DataBatch LongToDateTime(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _dateTimeArray, batch.Count);

            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _dateTimeArray[i] = new DateTime(sourceArray[batch.Index(i)], DateTimeKind.Utc);
            }

            return DataBatch.All(_dateTimeArray, batch.Count);
        }
    }
}

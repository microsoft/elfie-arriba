// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.IO;
using XForm.Query;
using XForm.Transforms;

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

        public Action<DataBatch, RowRemapper> TryGetComparer(CompareOperator op, object value)
        {
            return new DateTimeComparer().TryBuild(op, value);
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

    internal class DateTimeComparer : IDataBatchComparer
    {
        public Type Type => typeof(DateTime);
        public DateTime Value;

        public void SetValue(object value)
        {
            Value = (DateTime)value;
        }

        public void WhereEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            DateTime[] sourceArray = (DateTime[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value == sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereNotEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            DateTime[] sourceArray = (DateTime[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value != sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereLessThan(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            DateTime[] sourceArray = (DateTime[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value > sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereLessThanOrEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            DateTime[] sourceArray = (DateTime[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value >= sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereGreaterThan(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            DateTime[] sourceArray = (DateTime[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value < sourceArray[realIndex]) result.Add(i);
            }
        }

        public void WhereGreaterThanOrEquals(DataBatch source, RowRemapper result)
        {
            result.ClearAndSize(source.Count);
            DateTime[] sourceArray = (DateTime[])source.Array;
            for (int i = 0; i < source.Count; ++i)
            {
                int realIndex = source.Index(i);
                if (Value <= sourceArray[realIndex]) result.Add(i);
            }
        }
    }
}

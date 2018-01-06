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

        public ComparerExtensions.Comparer TryGetComparer(CompareOperator op)
        {
            // DateTimeComparer is generated
            return new DateTimeComparer().TryBuild(op);
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
        private bool[] _isNullArray;

        public DataBatch DateTimeToLong(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _longArray, batch.Count);
            Allocator.AllocateToSize(ref _isNullArray, batch.Count);

            bool areAnyNull = false;
            DateTime[] sourceArray = (DateTime[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int index = batch.Index(i);
                _longArray[i] = sourceArray[index].ToUniversalTime().Ticks;
                _isNullArray[i] = (batch.IsNull != null && batch.IsNull[index]);
                areAnyNull |= _isNullArray[i];
            }

            return DataBatch.All(_longArray, batch.Count, (areAnyNull ? _isNullArray : null));
        }

        public DataBatch LongToDateTime(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _dateTimeArray, batch.Count);
            Allocator.AllocateToSize(ref _isNullArray, batch.Count);

            bool areAnyNull = false;
            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int index = batch.Index(i);
                _dateTimeArray[i] = new DateTime(index, DateTimeKind.Utc);
                _isNullArray[i] = (batch.IsNull != null && batch.IsNull[index]);
                areAnyNull |= _isNullArray[i];
            }

            return DataBatch.All(_longArray, batch.Count, (areAnyNull ? _isNullArray : null));
        }
    }
}

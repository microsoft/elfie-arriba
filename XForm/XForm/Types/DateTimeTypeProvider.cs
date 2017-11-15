using System;
using XForm.Data;

namespace XForm.Types
{
    public class DateTimeTypeProvider : ITypeProvider
    {
        public string Name => "DateTime";

        public Type Type => typeof(DateTime);

        public IColumnReader BinaryReader(string columnPath)
        {
            return new ConvertingReader(TypeProviderFactory.Get(typeof(long)).BinaryReader(columnPath), TryGetConverter(typeof(long), typeof(DateTime), null));
        }

        public IColumnWriter BinaryWriter(string columnPath)
        {
            return new ConvertingWriter(TypeProviderFactory.Get(typeof(long)).BinaryWriter(columnPath), TryGetConverter(typeof(DateTime), typeof(long), null));
        }

        public Func<DataBatch, DataBatch> TryGetConverter(Type sourceType, Type targetType, object defaultValue)
        {
            if(sourceType == typeof(DateTime) && targetType == typeof(long))
            {
                return new DateTimeConverter().DateTimeToLong;
            }
            else if(sourceType == typeof(long) && targetType == typeof(DateTime))
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
            for(int i = 0; i < batch.Count; ++i)
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

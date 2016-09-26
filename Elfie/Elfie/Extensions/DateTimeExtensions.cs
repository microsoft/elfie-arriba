using System;

namespace Microsoft.CodeAnalysis.Elfie.Extensions
{
    public static class DateTimeExtensions
    {
        public static long ToLong(this DateTime value)
        {
            return value.ToUniversalTime().Ticks;
        }

        public static DateTime FromLong(long dateTimeValue)
        {
            return new DateTime(dateTimeValue, DateTimeKind.Utc);
        }
    }
}

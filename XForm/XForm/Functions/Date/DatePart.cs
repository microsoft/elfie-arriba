// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Functions.Date
{
    public enum DatePart
    {
        Year = 0,
        Month = 1,
        DayOfMonth = 2,
        Hour = 3,
        Minute = 4,
        Second = 5,
        Millisecond = 6
    }

    internal class DatePartBuilder : IFunctionBuilder
    {
        public string Name => "DatePart";
        public string Usage => "DatePart({DateTime}, {Part})";
        public Type ReturnType => typeof(ushort);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            IXColumn baseDateTime = context.Parser.NextColumn(source, context, typeof(DateTime));
            DatePart part = context.Parser.NextEnum<DatePart>();
            Func<DateTime, ushort> extractMethod = DatePartMethod(part);

            return SimpleTransformFunction<DateTime, ushort>.Build(
                Name,
                source,
                baseDateTime,
                (dateTime) => extractMethod(dateTime));
        }

        private static Func<DateTime, ushort> DatePartMethod(DatePart part)
        {
            switch (part)
            {
                case DatePart.Year:
                    return Year;
                case DatePart.Month:
                    return Month;
                case DatePart.DayOfMonth:
                    return DayOfMonth;
                case DatePart.Hour:
                    return Hour;
                case DatePart.Minute:
                    return Minute;
                case DatePart.Second:
                    return Second;
                case DatePart.Millisecond:
                    return Millisecond;
                default:
                    throw new NotImplementedException(part.ToString());
            }
        }

        private static ushort Year(DateTime when)
        {
            return (ushort)when.Year;
        }

        private static ushort Month(DateTime when)
        {
            return (ushort)when.Month;
        }

        private static ushort DayOfMonth(DateTime when)
        {
            return (ushort)when.Day;
        }

        private static ushort Hour(DateTime when)
        {
            return (ushort)when.Hour;
        }

        private static ushort Minute(DateTime when)
        {
            return (ushort)when.Minute;
        }

        private static ushort Second(DateTime when)
        {
            return (ushort)when.Second;
        }

        private static ushort Millisecond(DateTime when)
        {
            return (ushort)when.Millisecond;
        }
    }
}

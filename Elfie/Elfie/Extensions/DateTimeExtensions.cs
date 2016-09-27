// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

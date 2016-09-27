// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.CodeAnalysis.Elfie.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        ///  Convert a DateTime to the UTC Ticks long equivalent.
        ///  This is used to serialize DateTimes as primitives.
        ///  NOTE: DateTimes round-tripped will come back as UTC, not Local.
        /// </summary>
        /// <param name="value">DateTime to serialize</param>
        /// <returns>DateTime UTC Ticks as long</returns>
        public static long ToLong(this DateTime value)
        {
            return value.ToUniversalTime().Ticks;
        }
    }
}

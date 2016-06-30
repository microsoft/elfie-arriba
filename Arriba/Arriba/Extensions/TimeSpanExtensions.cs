// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba.Extensions
{
    /// <summary>
    ///  Extension methods for System.Timespan
    /// </summary>
    public static class TimeSpanExtensions
    {
        /// <summary>
        ///  Method to convert a Timespan into a short, easy to understand value 
        ///  for humans to read (1.4s, 66s, 21m).
        /// </summary>
        /// <param name="s">TimeSpan to convert (must be a positive span)</param>
        /// <returns>String value representing span</returns>
        public static string ToFriendlyString(this TimeSpan s)
        {
            if (s.Ticks < 0) throw new ArgumentOutOfRangeException("s", "ToFriendlyString only reports positive TimeSpans");

            DateTime beforeNow = DateTime.Now.Subtract(s);
            int totalYears = DateTime.Today.Year - beforeNow.Year;
            int totalMonths = 12 * totalYears + DateTime.Today.Month - beforeNow.Month;

            if (s.TotalMilliseconds < 10)
                return StringExtensions.Format("{0:n3}ms", s.TotalMilliseconds);
            else if (s.TotalSeconds < 1)
                return StringExtensions.Format("{0:n0}ms", s.TotalMilliseconds);
            else if (s.TotalSeconds < 10)
                return StringExtensions.Format("{0:n1}s", s.TotalSeconds);
            else if (s.TotalMinutes < 2)
                return StringExtensions.Format("{0:n0}s", s.TotalSeconds);
            else if (s.TotalMinutes < 10)
                return StringExtensions.Format("{0:n1} minutes", s.TotalMinutes);
            else if (s.TotalHours < 2)
                return StringExtensions.Format("{0:n0} minutes", s.TotalMinutes);
            else if (s.TotalDays < 2)
                return StringExtensions.Format("{0:n0} hours", s.TotalHours);
            else if (totalMonths < 2)
                return StringExtensions.Format("{0:n0} days", s.TotalDays);
            else if (totalMonths < 24)
                return StringExtensions.Format("{0:n0} months", totalMonths);
            else
                return StringExtensions.Format("{0:n0} years", totalYears);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.CodeAnalysis.Elfie.Extensions
{
    public static class LongExtensions
    {
        public const long Kilobyte = 1024;
        public const long Megabyte = 1024 * 1024;
        public const long Gigabyte = 1024 * 1024 * 1024;

        public const long Thousand = 1000;
        public const long Million = 1000 * 1000;
        public const long Billion = 1000 * 1000 * 1000;

        public static string[] SizeScales = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB" };
        public static string[] CountScales = { "", "K", "M", "B", "T" };

        /// <summary>
        ///  Converts a file size in bytes into an easily human readable scaled value (ex: 1.44 MB)
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <returns>String value representing the size</returns>
        public static string SizeString(this long bytes)
        {
            return SizeString(bytes, 1024, SizeScales);
        }

        /// <summary>
        ///  Converts a count into an easily human readable scaled value (ex: 1.22 B)
        /// </summary>
        /// <param name="count">Size in bytes</param>
        /// <returns>String value representing the size</returns>
        public static string CountString(this long count)
        {
            return SizeString(count, 1000, CountScales);
        }

        /// <summary>
        ///  Converts a file size in bytes into an easily human readable scaled value (ex: 1.44 MB)
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <returns>String value representing the size</returns>
        private static string SizeString(this long bytes, long divisorPerUnit, string[] unitOptions)
        {
            int scale = 0;
            double scaledSize = bytes;

            // Determine the overall scale of the value
            while (scaledSize > divisorPerUnit && scale < (unitOptions.Length - 1))
            {
                scale++;
                scaledSize /= divisorPerUnit;
            }

            // Determine the unit name to show
            string units = unitOptions[scale];

            // Determine how many decimal digits we should show 
            // (four significant figures total; never show decimals for bytes)
            string formatString = "{0:n0} {1}";
            if (scale > 0)
            {
                int scaleWholeDigits = (int)Math.Log10(scaledSize) + 1;
                int decimalPrecision = 4 - scaleWholeDigits;
                formatString = "{0:n" + decimalPrecision.ToString(CultureInfo.InvariantCulture) + "} {1}";
            }

            return String.Format(CultureInfo.InvariantCulture, formatString, scaledSize, units);
        }

        /// <summary>
        ///  Convert a long [UTC ticks] to the corresponding UTC DateTime.
        /// </summary>
        /// <param name="dateTimeValue">UTC Ticks Long to convert</param>
        /// <returns>UTC DateTime equivalent</returns>
        public static DateTime ToDateTime(this long dateTimeValue)
        {
            return new DateTime(dateTimeValue, DateTimeKind.Utc);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Arriba.Extensions
{
    public static class LongExtensions
    {
        public static string[] BaseTenValues = { "0", "1", "10", "100", "1k", "10k", "100k", "1m", "10m", "100m", "1b", "10b", "100b", "1t", "10t", ">100t" };
        public static string[] FileScaleValues = { "0b", "1b", "10b", "100b", "1kb", "10kb", "100kb", "1mb", "10mb", "100mb", "1gb", "10gb", "100gb", "1tb", "10tb", ">100tb" };

        /// <summary>
        ///  Converts a file size in bytes into an easily human readable scaled value (ex: 1.44 MB)
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <returns>String value representing the size</returns>
        public static string SizeString(this long bytes)
        {
            int scale = 0;
            double scaledSize = bytes;

            // Determine the overall units of the size (bytes, KB, MB, GB, TB)
            while (scaledSize > 1024)
            {
                scale++;
                scaledSize /= 1024;
            }

            // Determine the unit name to show
            //  long.MaxValue = 2^64 = 2^4 * 2^60 (8 exabytes)
            string[] unitOptions = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB" };
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

            return StringExtensions.Format(formatString, scaledSize, units);
        }

        /// <summary>
        ///  Converts a number into an easily human readable (and indexable) value indicating base ten
        ///  scale - "1k" for values from 1000-9999, "10k" for values up to 99,999, etc.
        /// </summary>
        /// <param name="value">Value for which to return scale</param>
        /// <returns>Scale of Value as a string</returns>
        public static string BaseTenScale(this long value)
        {
            if (value <= 0) return BaseTenValues[0];

            int scale = 1;
            long scaledValue = value;

            while (scaledValue >= 10 && scale < BaseTenValues.Length - 1)
            {
                scale++;
                scaledValue /= 10;
            }

            return BaseTenValues[scale];
        }

        /// <summary>
        ///  Converts a number of bytes into an easily human readable (and indexable) value indicating base ten
        ///  scale - "1kb" for values up to 1024-10,239, "1mb" for values up 1,048,576-10,485,759 etc.
        /// </summary>
        /// <param name="bytes">Value for which to return scale</param>
        /// <returns>Scale of Value as a string</returns>
        public static string FileSizeScale(this long bytes)
        {
            if (bytes <= 0) return FileScaleValues[0];

            int scale = 1;
            long scaledValue = bytes;

            while (scaledValue >= 1024 && scale < FileScaleValues.Length - 3)
            {
                scale = scale + 3;
                scaledValue /= 1024;
            }

            if (scaledValue >= 100) scale++;
            if (scaledValue >= 10) scale++;

            return FileScaleValues[scale];
        }
    }
}

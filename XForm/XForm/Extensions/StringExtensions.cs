// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace XForm.Extensions
{
    public static class StringExtensions
    {
        public static string BeforeFirst(this string text, char c)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            int index = text.IndexOf(c);
            if (index == -1) return string.Empty;

            return text.Substring(0, index);
        }

        public static string RelativePath(this string fullPath, string prefixToRemove, string suffixToRemove = null)
        {
            if (String.IsNullOrEmpty(fullPath)) return String.Empty;

            int index = 0;
            int length = fullPath.Length;

            if (!String.IsNullOrEmpty(prefixToRemove))
            {
                if (!fullPath.StartsWith(prefixToRemove, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"Path {fullPath} wasn't under {prefixToRemove}.");
                index = prefixToRemove.Length;
                length -= prefixToRemove.Length;
            }

            if (!String.IsNullOrEmpty(suffixToRemove))
            {
                if (!fullPath.EndsWith(suffixToRemove, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"Path {fullPath} didn't have suffix {suffixToRemove}.");
                length -= suffixToRemove.Length;
            }

            return fullPath.Substring(index, length);
        }

        /// <summary>
        ///  Parse a "friendly" TimeSpan value, like 7d, 24h, 5m, 30s.
        /// </summary>
        /// <param name="value">String value to parse as a "friendly" format TimeSpan</param>
        /// <returns>TimeSpan equivalent to value</returns>
        public static TimeSpan ParseTimeSpanFriendly(this string value)
        {
            TimeSpan result;
            if (!TryParseTimeSpanFriendly(value, out result)) throw new ArgumentException("value");
            return result;
        }

        /// <summary>
        ///  Parse a "friendly" TimeSpan value, like 7d, 24h, 5m, 30s.
        /// </summary>
        /// <param name="value">String value to parse as a "friendly" format TimeSpan</param>
        /// <param name="result">TimeSpan equivalent of value</param>
        /// <returns>TimeSpan equivalent to value</returns>
        public static bool TryParseTimeSpanFriendly(this string value, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (String.IsNullOrEmpty(value)) return false;

            // Find the portion of the value which is the number part
            int numberPrefixLength = 0;
            for (; numberPrefixLength < value.Length; ++numberPrefixLength)
            {
                if (Char.IsLetter(value[numberPrefixLength])) break;
            }

            double numberPartValue = double.Parse(value.Substring(0, numberPrefixLength));
            string suffix = value.Substring(numberPrefixLength);

            switch (suffix.ToLowerInvariant())
            {
                case "s":
                case "sec":
                    result = TimeSpan.FromSeconds(numberPartValue);
                    break;
                case "m":
                case "min":
                    result = TimeSpan.FromMinutes(numberPartValue);
                    break;
                case "h":
                case "hour":
                    result = TimeSpan.FromHours(numberPartValue);
                    break;
                case "d":
                case "day":
                    result = TimeSpan.FromDays(numberPartValue);
                    break;
                default:
                    //throw new ArgumentException($"Unable to parse \"{value}\" as a friendly TimeSpan. Unit \"{suffix}\" was unknown. Expecting 's', 'm', 'h', 'd'.");
                    return false;
            }

            return true;
        }
    }
}

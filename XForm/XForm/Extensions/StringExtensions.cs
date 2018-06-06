// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace XForm.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveTrailing(this string text, char c)
        {
            if (!string.IsNullOrEmpty(text) && text[text.Length - 1] == c) return text.Substring(0, text.Length - 1);
            return text;
        }

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
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    }
}

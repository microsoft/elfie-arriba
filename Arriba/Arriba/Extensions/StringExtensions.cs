// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Arriba.Extensions
{
    public static class StringExtensions
    {
        public static readonly object[] EmptyArray = new object[0];
        public const string NullArgumentReplacement = "<null>";

        /// <summary>
        ///  Convert all newlines in value to '\r\n' form.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string CanonicalizeNewlines(this string value)
        {
            if (String.IsNullOrEmpty(value)) return String.Empty;
            StringBuilder result = new StringBuilder();

            char last = '\0';
            for (int i = 0; i < value.Length; ++i)
            {
                char c = value[i];

                if (c == '\n')
                {
                    // \n without \r? Prepend '\r'
                    if (last != '\r')
                    {
                        result.Append('\r');
                    }
                }
                else if (last == '\r')
                {
                    // '\r' without '\n'? Append '\n'
                    result.Append('\n');
                }

                result.Append(c);

                last = c;
            }

            // If the last character was a lone '\r', add the '\n'
            if (last == '\r') result.Append('\n');

            return result.ToString();
        }

        /// <summary>
        /// Wrapper for StringExtensions.Format to pass culture as well as any additional behavior handling required
        /// </summary>
        /// <param name="formatSpecifier"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string Format(string formatSpecifier, params object[] args)
        {
            if (args == null)
            {
                // If no args were passed, pass an empty array so that String.Format doesn't throw.
                args = EmptyArray;
            }
            else
            {
                // Replace any null arguments with "<null>" so output is clearer.
                for (int i = 0; i < args.Length; ++i)
                {
                    if (args[i] == null) args[i] = NullArgumentReplacement;
                }
            }

            return String.Format(System.Globalization.CultureInfo.InvariantCulture, formatSpecifier, args);
        }
    }
}

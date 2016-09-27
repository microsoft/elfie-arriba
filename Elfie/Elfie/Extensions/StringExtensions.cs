// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.Elfie.Extensions
{
    /// <summary>
    ///  Extension Methods for System.String
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///  String.Join variant which takes an IEnumerator instead of an IEnumerable.
        /// </summary>
        /// <typeparam name="T">Type of item in enumerator</typeparam>
        /// <param name="separator">String to write between items in output</param>
        /// <param name="enumerator">IEnumerator of items</param>
        /// <returns>item.ToString for each item, separated by separators</returns>
        public static string Join<T>(string separator, IEnumerator<T> enumerator)
        {
            StringBuilder builder = new StringBuilder();
            while (enumerator.MoveNext())
            {
                if (builder.Length > 0) builder.Append(separator);

                T current = enumerator.Current;
                if (current == null)
                {
                    builder.Append("<null>");
                }
                else
                {
                    builder.Append(current.ToString());
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Return an enumerable list of target framework names, from a
        /// String8 instance that contains an XML-encoded set
        /// </summary>
        /// <param name="encodedFrameworkNames">String8 to converter to enumerable framework names</param>
        /// <returns>One or more target framework names as strings</returns>
        public static IEnumerable<string> ToFrameworkNames(this string encodedFrameworkNames)
        {
            List<string> frameworkNames = new List<string>();

            if (!string.IsNullOrEmpty(encodedFrameworkNames))
            {
                foreach (XNode node in XElement.Parse(encodedFrameworkNames, LoadOptions.None)
                    .DescendantNodes()
                    .Where(x => x is XText))
                {
                    frameworkNames.Add(((XText)node).Value);
                }
            }
            return frameworkNames;
        }

        public static string EncodeFrameworkNamesToXml(string[] tfms)
        {
            var x = new XElement("tfms", tfms.Select(t => new XElement("tfm", t)));
            var s = x.ToString(SaveOptions.DisableFormatting);
            return s;
        }

        /// <summary>
        ///  Return the SHA-256 hash of the UTF8 form of a string.
        /// </summary>
        /// <param name="text">String to hash</param>
        /// <returns>SHA-256 hash of text</returns>
        public static byte[] ToSHA256(this string text)
        {
            SHA256Managed sha = new SHA256Managed();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        ///  Return the hex string of the SHA-256 hash of the UTF8 form of a string.
        /// </summary>
        /// <param name="text">String to hash</param>
        /// <returns>Hex string of SHA-256 hash of text</returns>
        public static string ToSHA256String(this string text)
        {
            byte[] hash = text.ToSHA256();

            StringBuilder hashText = new StringBuilder();

            foreach (byte b in hash)
            {
                hashText.Append(b.ToString("x2"));
            }

            return hashText.ToString();
        }
    }
}

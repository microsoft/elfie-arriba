// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    public static class Read
    {
        /// <summary>
        ///  Easy method to read a file from a method which reads from a TextWriter.
        /// </summary>
        /// <param name="readMethod">Method which reads from a TextWriter</param>
        /// <param name="filePath">File Path to read from</param>
        public static void FromFile(Action<TextReader> readMethod, string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                readMethod(reader);
            }
        }

        /// <summary>
        ///  Easy method to read a string from a method which reads from a TextWriter.
        /// </summary>
        /// <param name="readMethod">Method which reads from to a TextWriter</param>
        /// <param name="content">Content to read</param>
        public static void FromString(Action<TextReader> readMethod, string content)
        {
            using (StringReader reader = new StringReader(content))
            {
                readMethod(reader);
            }
        }
    }
}

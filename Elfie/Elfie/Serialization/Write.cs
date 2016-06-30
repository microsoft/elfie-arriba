// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  Write contains methods to quickly write text content to strings or files.
    /// </summary>
    public static class Write
    {
        /// <summary>
        ///  Easy method to build a string from a method which writes to a TextWriter.
        /// </summary>
        /// <param name="writeMethod">Method which writes to a TextWriter</param>
        /// <returns>string written by method</returns>
        public static string ToString(Action<TextWriter> writeMethod)
        {
            StringBuilder result = new StringBuilder();

            using (StringWriter writer = new StringWriter(result))
            {
                writeMethod(writer);
            }

            return result.ToString();
        }

        /// <summary>
        ///  Easy method to build a string from a method which writes to a TextWriter.
        /// </summary>
        /// <param name="writeMethod">Method which writes to a TextWriter</param>
        /// <returns>string written by method</returns>
        public static string ToString<T>(Func<TextWriter, T> writeMethod)
        {
            StringBuilder result = new StringBuilder();

            using (StringWriter writer = new StringWriter(result))
            {
                writeMethod(writer);
            }

            return result.ToString();
        }

        /// <summary>
        ///  Easy method to write a file from a method which writes to a TextWriter.
        ///  
        /// </summary>
        /// <param name="writeMethod">Method which writes to a TextWriter</param>
        /// <param name="filePath">File Path to write to</param>
        public static void ToFile(Action<TextWriter> writeMethod, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                writeMethod(writer);
            }
        }
    }
}

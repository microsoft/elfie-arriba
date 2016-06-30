// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Arriba.Serialization
{
    /// <summary>
    ///  TextSerializer provides simple serialization of plain text values. They
    ///  are written as UTF-8 bytes using .NET encoding, which means there is a
    ///  length prefix, allowing multiple values within a file.
    /// </summary>
    public class TextSerializer
    {
        /// <summary>
        ///  Read a DateTime value from the given file or return the default.
        /// </summary>
        /// <param name="path">DiskCache relative path to read</param>
        /// <param name="defaultValue">Default if file not found or not DateTime</param>
        /// <returns>Value read, if possible, or default value</returns>
        public static DateTime ReadDateTime(string path, DateTime defaultValue)
        {
            DateTime result;
            StringSerializer s = new StringSerializer();
            if (!s.TryRead(path) || !DateTime.TryParse(s.Value, out result))
            {
                return defaultValue;
            }

            return result;
        }

        /// <summary>
        ///  Read a string value from the given file or return the default.
        /// </summary>
        /// <param name="path">DiskCache relative path to read</param>
        /// <param name="defaultValue">Default if file not found</param>
        /// <returns>Value read, if possible, or default value</returns>
        public static string ReadString(string path, string defaultValue)
        {
            StringSerializer s = new StringSerializer();
            if (!s.TryRead(path)) return defaultValue;
            return s.Value;
        }

        /// <summary>
        ///  Write a DateTime value to the given file.
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="path">DiskCache relative path at which to write</param>
        public static void Write(DateTime value, string path)
        {
            StringSerializer s = new StringSerializer(value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            s.Write(path);
        }

        /// <summary>
        ///  Write a string value to the given file.
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="path">DiskCache relative path at which to write</param>
        public static void Write(string value, string path)
        {
            StringSerializer s = new StringSerializer(value);
            s.Write(path);
        }

        private class StringSerializer : IBinarySerializable
        {
            public string Value { get; set; }

            public StringSerializer()
            { }

            public StringSerializer(string value)
            {
                this.Value = value;
            }

            public void ReadBinary(ISerializationContext context)
            {
                if (context == null) throw new ArgumentNullException("context");

                this.Value = context.Reader.ReadString();
            }

            public void WriteBinary(ISerializationContext context)
            {
                if (context == null) throw new ArgumentNullException("context");

                context.Writer.Write(this.Value);
            }
        }
    }
}

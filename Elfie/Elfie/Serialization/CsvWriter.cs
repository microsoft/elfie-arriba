// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  CsvWriter is a high performance writer for the CSV (comma-separated value)
    ///  format. Values are always quoted by this writer for best performance and
    ///  consistent validation that downstream readers handle quoted values correctly.
    ///  
    ///  Quotes within quoted values are doubled to escape them.
    ///  
    ///  https://www.ietf.org/rfc/rfc4180.txt
    ///  
    ///  Usage:
    ///  using (CsvWriter w = new CsvWriter(writeToPath, new string[] { "Name", "IPs" }))
    ///  {
    ///     while(/* ... data source .. */)
    ///     {
    ///         w.Write(name);
    ///         w.Write(ips);
    ///         w.NextRow();
    ///     }
    /// }
    /// </summary>
    public class CsvWriter : BaseTabularWriter
    {
        /// <summary>
        ///  Construct a new CsvWriter to write to the given file path.
        ///  The file is overwritten if it exists.
        /// </summary>
        /// <param name="filePath">Path to file to write.</param>
        /// <param name="writeHeaderRow">True to write a header row, False otherwise.</param>
        public CsvWriter(string filePath, bool writeHeaderRow = true) :
            base(filePath, writeHeaderRow)
        { }

        /// <summary>
        ///  Construct a new CsvWriter to write to the given stream.
        ///  The column name list is used to validate that rows have the right
        ///  number of columns written even if a header row isn't written.
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        /// <param name="writeHeaderRow">True to write a header row, False otherwise</param>
        public CsvWriter(Stream stream, bool writeHeaderRow = true) :
            base(stream, writeHeaderRow)
        { }

        protected override void WriteCellDelimiter(Stream stream)
        {
            stream.WriteByte(UTF8.Comma);
        }

        protected override void WriteRowSeparator(Stream stream)
        {
            stream.WriteByte(UTF8.CR);
            stream.WriteByte(UTF8.Newline);
        }

        protected override void WriteCellValue(Stream stream, String8 value)
        {
            WriteValueStart(stream);
            WriteValuePart(stream, value);
            WriteValueEnd(stream);
        }

        protected override void WriteValueStart(Stream stream)
        {
            // Partial values must be escaped (we don't know if they'll need to be)
            stream.WriteByte(UTF8.Quote);
        }

        protected override void WriteValuePart(Stream stream, String8 value)
        {
            // Look for quotes in string
            int nextWriteStartIndex = 0;
            int end = value.Index + value.Length;
            for (int i = value.Index; i < end; ++i)
            {
                byte c = value.Array[i];
                if (c == UTF8.Quote)
                {
                    // Write everything including quote
                    int inStringIndex = i - value.Index;
                    value.Substring(nextWriteStartIndex, inStringIndex - nextWriteStartIndex).WriteTo(stream);
                    nextWriteStartIndex = inStringIndex;

                    // Write a second quote
                    stream.WriteByte(UTF8.Quote);
                }
            }

            // Write content after the last quote seen
            value.Substring(nextWriteStartIndex).WriteTo(stream);
        }

        protected override void WriteValuePart(Stream stream, byte c)
        {
            if (c == UTF8.Quote) stream.WriteByte(UTF8.Quote);
            stream.WriteByte(c);
        }

        protected override void WriteValueEnd(Stream stream)
        {
            // Partial values must be escaped (we don't know if they'll need to be)
            stream.WriteByte(UTF8.Quote);
        }
    }
}

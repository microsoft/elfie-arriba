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
        /// <param name="columnNames">Column Names to write out.</param>
        /// <param name="writeHeaderRow">True to write a header row, False otherwise.</param>
        /// /// <param name="cellDelimiter">Delimiter between cells, default is tab.</param>
        public CsvWriter(string filePath, IEnumerable<string> columnNames, bool writeHeaderRow = true) :
            base(filePath, columnNames, writeHeaderRow)
        { }

        /// <summary>
        ///  Construct a new CsvWriter to write to the given stream.
        ///  The column name list is used to validate that rows have the right
        ///  number of columns written even if a header row isn't written.
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        /// <param name="columnNames">Column names to write.</param>
        /// <param name="writeHeaderRow">True to write a header row, False otherwise</param>
        /// <param name="cellDelimiter">Delimiter between cells, default is tab.</param>
        public CsvWriter(Stream stream, IEnumerable<string> columnNames, bool writeHeaderRow = true) :
            base(stream, columnNames, writeHeaderRow)
        { }

        protected override void WriteCellDelimiter(Stream stream)
        {
            stream.WriteByte(UTF8.Comma);
        }

        protected override void WriteRowSeparator(Stream stream)
        {
            stream.WriteByte(UTF8.CR);
            stream.WriteByte(UTF8.LF);
        }

        protected override void WriteCellValue(Stream stream, String8 value)
        {
            // Write leading quote
            stream.WriteByte(UTF8.Quote);

            // Look for quotes in string
            int nextWriteStartIndex = 0;
            int end = value._index + value._length;
            for (int i = value._index; i < end; ++i)
            {
                byte c = value._buffer[i];
                if (c == UTF8.Quote)
                {
                    // Write everything including quote
                    int inStringIndex = i - value._index;
                    value.Substring(nextWriteStartIndex, inStringIndex - nextWriteStartIndex).WriteTo(stream);
                    nextWriteStartIndex = inStringIndex;

                    // Write a second quote
                    stream.WriteByte(UTF8.Quote);
                }
            }

            // Write content after the last quote seen
            value.Substring(nextWriteStartIndex).WriteTo(stream);

            // Write trailing quote
            stream.WriteByte(UTF8.Quote);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  TsvReader is a high performance writer for the TSV (tab-separated value)
    ///  format. The writer removes '\t' and '\n' in values written to avoid read
    ///  errors, as no standard for escaping seems to be defined.
    ///  
    ///  See BaseTabularWriter for details and usage.
    /// </summary>
    public class TsvWriter : BaseTabularWriter
    {
        /// <summary>
        ///  Construct a new TsvWriter to write to the given file path.
        ///  The file is overwritten if it exists.
        /// </summary>
        /// <param name="filePath">Path to file to write.</param>
        /// <param name="columnNames">Column Names to write out.</param>
        /// <param name="writeHeaderRow">True to write a header row, False otherwise.</param>
        /// /// <param name="cellDelimiter">Delimiter between cells, default is tab.</param>
        public TsvWriter(string filePath, IEnumerable<string> columnNames, bool writeHeaderRow = true) :
            base(filePath, columnNames, writeHeaderRow)
        { }

        /// <summary>
        ///  Construct a new TsvWriter to write to the given stream.
        ///  The column name list is used to validate that rows have the right
        ///  number of columns written even if a header row isn't written.
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        /// <param name="columnNames">Column names to write.</param>
        /// <param name="writeHeaderRow">True to write a header row, False otherwise</param>
        /// <param name="cellDelimiter">Delimiter between cells, default is tab.</param>
        public TsvWriter(Stream stream, IEnumerable<string> columnNames, bool writeHeaderRow = true) :
            base(stream, columnNames, writeHeaderRow)
        { }

        protected override void WriteCellDelimiter(Stream stream)
        {
            stream.WriteByte(UTF8.Tab);
        }

        protected override void WriteRowSeparator(Stream stream)
        {
            stream.WriteByte(UTF8.CR);
            stream.WriteByte(UTF8.Newline);
        }

        protected override void WriteCellValue(Stream stream, String8 value)
        {
            // Escaping: If value contains cell or row delimiter, just omit them
            // No standard for TSV escaping.
            int nextWriteStartIndex = 0;
            int end = value._index + value._length;
            for (int i = value._index; i < end; ++i)
            {
                byte c = value._buffer[i];
                if (c == UTF8.Tab || c == UTF8.Newline)
                {
                    int inStringIndex = i - value._index;
                    value.Substring(nextWriteStartIndex, inStringIndex - nextWriteStartIndex).WriteTo(stream);
                    nextWriteStartIndex = inStringIndex + 1;
                }
            }

            value.Substring(nextWriteStartIndex).WriteTo(stream);
        }
    }
}

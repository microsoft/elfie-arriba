// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Arriba.Extensions;
using Arriba.Structures;

namespace Arriba.Serialization.Csv
{
    /// <summary>
    ///  CsvWriter provides efficient serialization of values to CSV files for
    ///  interoperability with other data systems.
    ///  
    ///  Follows http://tools.ietf.org/html/rfc4180
    /// </summary>
    public class CsvWriter : IDisposable
    {
        private const string HeadersDidNotMatch = "CsvWriter could not append to CSV. Heading found did not match columns to write.\r\nExpect:{0}Actual:{1}";
        private const string IncorrectTerminatorFound = "CsvWriter could not append to CSV. Existing content did not end with CRLF terminator. Found '{0}'.";

        private MemoryStream CurrentRow { get; set; }
        private ISerializationContext Context { get; set; }
        private int ColumnCount { get; set; }
        private int ColumnsInCurrentRow { get; set; }

        public CsvWriter(string outputFilePath, IEnumerable<string> columns) :
            this(new SerializationContext(new FileStream(outputFilePath, FileMode.OpenOrCreate)), columns)
        { }

        /// <summary>
        ///  Create a CsvWriter to append to the provided context. A heading row
        ///  is written if this is an empty file.
        /// </summary>
        /// <param name="context">ISerializationContext to write to</param>
        /// <param name="columns">Set of column headings for this CSV</param>
        public CsvWriter(ISerializationContext context, IEnumerable<string> columns)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (columns == null) throw new ArgumentNullException("columns");

            this.CurrentRow = new MemoryStream();
            this.Context = context;
            this.ColumnCount = columns.Count();

            // If this stream is being appended to, create a MemoryStream to write
            // the expected heading row for comparison
            if (context.Stream.CanSeek && context.Stream.Length > 0)
            {
                this.Context = new SerializationContext(new MemoryStream());
            }

            // Write the UTF-8 BOM
            this.Context.Writer.Write((byte)0xEF);
            this.Context.Writer.Write((byte)0xBB);
            this.Context.Writer.Write((byte)0xBF);

            // Write the heading row
            foreach (string column in columns)
            {
                AppendValue(column);
            }

            AppendRowSeparator();

            // If this isn't a new CSV, verify it and seek to the end
            if (context.Stream.CanSeek && context.Stream.Length > 0)
            {
                // Verify the heading matches what we would write
                int headingLength = (int)this.Context.Stream.Length;

                this.Context.Stream.Seek(0, SeekOrigin.Begin);
                ByteBlock expectedHeading = new ByteBlock(this.Context.Reader.ReadBytes(headingLength));

                context.Stream.Seek(0, SeekOrigin.Begin);
                ByteBlock actualHeading = new ByteBlock(context.Reader.ReadBytes(headingLength));

                if (!expectedHeading.Equals(actualHeading))
                {
                    throw new IOException(StringExtensions.Format(HeadersDidNotMatch, expectedHeading, actualHeading));
                }

                // Verify the last bytes are terminators
                context.Stream.Seek(-2, SeekOrigin.End);
                string terminator = Encoding.UTF8.GetString(context.Reader.ReadBytes(2));
                if (terminator != "\r\n") throw new IOException(StringExtensions.Format(IncorrectTerminatorFound, terminator));

                // Swap the desired context back for subsequent use
                this.Context = context;
            }
        }

        /// <summary>
        ///  Append a row of values from an array. The array must have as many values
        ///  as the number of columns. The column and row separator are written.
        ///  Values are converted to strings with ToString and then to UTF8; byte arrays
        ///  and ByteBlocks are assumed to already be UTF8 strings.
        /// </summary>
        /// <param name="row">Array of values to append</param>
        public void AppendRow(Array row)
        {
            if (row == null) throw new ArgumentNullException("row");

            foreach (object value in row)
            {
                AppendValue(value);
            }

            AppendRowSeparator();
        }

        /// <summary>
        ///  Append a value to the current row. A column separator is written if needed.
        ///  Callers must call AppendRowSeparator after appending the row of values.
        ///  Values are converted to strings with ToString and then to UTF8; byte arrays
        ///  and ByteBlocks are assumed to already be UTF8 strings.
        /// </summary>
        /// <param name="value">Value to append to the current row</param>
        public void AppendValue(object value)
        {
            if (value == null)
            {
                // Null - just write the next separator with no value
                AppendColumnSeparator();
            }
            else if (value is DateTime)
            {
                // DateTime - Convert to UTC, string, then bytes
                AppendValue(Encoding.UTF8.GetBytes(((DateTime)value).ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)));
            }
            else if (value is byte[])
            {
                // Array - Pass through as-is
                byte[] valueArray = (byte[])value;
                AppendValue(valueArray, 0, valueArray.Length);
            }
            else if (value is ByteBlock)
            {
                // ByteBlock - avoid double encoding
                ByteBlock valueBlock = (ByteBlock)value;
                AppendValue(valueBlock.Array, valueBlock.Index, valueBlock.Length);
            }
            else
            {
                // Other types - use ToString conversion and then UTF8 form
                AppendValue(Encoding.UTF8.GetBytes(value.ToString()));
            }
        }

        /// <summary>
        ///  Append a value to the current row from the given byte[]. The array
        ///  is assumed to already be UTF8 bytes. A column separator is written
        ///  if needed.
        /// </summary>
        /// <param name="valueBytes">UTF8 byte[] to write from</param>
        public void AppendValue(byte[] valueBytes)
        {
            AppendValue(valueBytes, 0, valueBytes.Length);
        }

        /// <summary>
        ///  Append a value to the current row from the given byte[]. The array
        ///  is assumed to already be UTF8 bytes. A column separator is written
        ///  if needed.
        /// </summary>
        /// <param name="valueBytes">UTF8 byte[] to write from</param>
        /// <param name="index">Index from which to write</param>
        /// <param name="length">Length of bytes to write</param>
        public void AppendValue(byte[] valueBytes, int index, int length)
        {
            // Append the comma between values, if needed
            AppendColumnSeparator();

            // Empty block, only write the seperator. 
            if (length == 0)
            {
                return;
            }

            if (valueBytes == null) throw new ArgumentNullException("valueBytes");

            int end = index + length;

            // If value contains a double quote, comma, or newline character, it must be escaped
            bool requiresEscaping = false;
            for (int i = index; i < end; ++i)
            {
                byte b = valueBytes[i];
                if (b == UTF8.CR || b == UTF8.LF || b == UTF8.Comma || b == UTF8.DoubleQuote)
                {
                    requiresEscaping = true;
                    break;
                }
            }

            if (!requiresEscaping)
            {
                // Unescaped - write value bytes directly
                this.CurrentRow.Write(valueBytes, index, length);
            }
            else
            {
                // Escaped - double quote value, and escape double quotes with two double quotes
                this.CurrentRow.WriteByte(UTF8.DoubleQuote);

                for (int i = index; i < end; ++i)
                {
                    byte b = valueBytes[i];
                    if (b == UTF8.DoubleQuote) this.CurrentRow.WriteByte(UTF8.DoubleQuote);
                    this.CurrentRow.WriteByte(b);
                }

                this.CurrentRow.WriteByte(UTF8.DoubleQuote);
            }
        }

        /// <summary>
        ///  Append a row separator. If writing values with AppendValue, call
        ///  after writing a row of values.
        /// </summary>
        public void AppendRowSeparator()
        {
            if (this.ColumnsInCurrentRow != this.ColumnCount) throw new InvalidOperationException(StringExtensions.Format("The CSV has {0:n0} columns, but {1:n0} were written in a row.", this.ColumnCount, this.ColumnsInCurrentRow));
            this.CurrentRow.WriteByte(UTF8.CR);
            this.CurrentRow.WriteByte(UTF8.LF);

            this.CurrentRow.Seek(0, SeekOrigin.Begin);
            this.CurrentRow.CopyTo(this.Context.Stream);
            this.CurrentRow.SetLength(0);

            this.ColumnsInCurrentRow = 0;
        }

        /// <summary>
        ///  Append a column separator. Called automatically by AppendValue and AppendRow.
        /// </summary>
        private void AppendColumnSeparator()
        {
            if (this.ColumnsInCurrentRow > 0)
            {
                this.CurrentRow.WriteByte(UTF8.Comma);
            }

            this.ColumnsInCurrentRow++;
        }

        /// <summary>
        ///  Flush the stream at the current position.
        /// </summary>
        public void Flush()
        {
            this.Context.Stream.Flush();
        }

        /// <summary>
        ///  Return the number of bytes written out so far.
        /// </summary>
        public long BytesWritten
        {
            get
            {
                return this.Context.Stream.Length;
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///  Dispose the ISerializationContext being written to.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (this.CurrentRow != null)
            {
                this.CurrentRow.Dispose();
                this.CurrentRow = null;
            }

            if (this.Context != null)
            {
                this.Context.Dispose();
                this.Context = null;
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  TsvReader is a high performance writer for the TSV (tab-separated value)
    ///  format. The reader doesn't support escaped \t or \n in values (no standard
    ///  for escaping seems to be defined).
    ///  
    ///  Usage:
    ///  using (TsvWriter w = new TsvWriter(writeToPath, new string[] { "Name", "IPs" }))
    ///  {
    ///     while(/* ... data source .. */)
    ///     {
    ///         w.Write(name);
    ///         w.Write(ips);
    ///         w.NextRow();
    ///     }
    /// }
    /// </summary>
    public class TsvWriter : IDisposable
    {
        private Stream _stream;
        private byte _cellDelimiter;

        private int _columnCount;
        private int _currentRowColumnCount;
        private int _rowCount;

        private byte[] _typeConversionBuffer;

        /// <summary>
        ///  Construct a new TsvWriter to write to the given file path.
        ///  The file is overwritten if it exists.
        /// </summary>
        /// <param name="tsvFilePath">Path to file to write.</param>
        /// <param name="columnNames">Column Names to write out.</param>
        /// <param name="writeHeaderRow">True to write a header row, False otherwise.</param>
        /// /// <param name="cellDelimiter">Delimiter between cells, default is tab.</param>
        public TsvWriter(string tsvFilePath, IEnumerable<string> columnNames, bool writeHeaderRow = true, char cellDelimiter = '\t') :
            this(new FileStream(tsvFilePath, FileMode.Create, FileAccess.Write, FileShare.None), columnNames, writeHeaderRow, cellDelimiter)
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
        public TsvWriter(Stream stream, IEnumerable<string> columnNames, bool writeHeaderRow = true, char cellDelimiter = '\t')
        {
            _stream = stream;
            _cellDelimiter = (byte)cellDelimiter;
            _columnCount = columnNames.Count();
            _currentRowColumnCount = 0;
            _rowCount = 1;

            _typeConversionBuffer = new byte[30];

            // Write header row
            if (writeHeaderRow)
            {
                String8Block buffer = new String8Block();
                foreach (string columnName in columnNames)
                {
                    Write(buffer.GetCopy(columnName));
                }

                NextRow();
            }

            if (_columnCount == 0) throw new TsvWriterException("No columns were passed to contructor. TSV must have at least one column.");
        }

        /// <summary>
        ///  Write a value to the current row.
        ///  Delimiter characters are skipped if found in the value [no TSV escaping standard]
        /// </summary>
        /// <param name="value">String8 to write to the current row.</param>
        public void Write(String8 value)
        {
            WriteCellSeparator();

            // Escaping: If value contains cell or row delimiter, just omit them
            // No standard for TSV escaping.
            int nextWriteStartIndex = 0;
            int end = value._index + value._length;
            for (int i = value._index; i < end; ++i)
            {
                byte c = value._buffer[i];
                if (c == _cellDelimiter || c == (byte)'\n')
                {
                    int inStringIndex = i - value._index;
                    value.Substring(nextWriteStartIndex, inStringIndex - nextWriteStartIndex).WriteTo(_stream);
                    nextWriteStartIndex = inStringIndex + 1;
                }
            }

            value.Substring(nextWriteStartIndex).WriteTo(_stream);
        }

        /// <summary>
        ///  Write an integer to the current row.
        ///  The value is converted without allocations.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void Write(int value)
        {
            WriteCellSeparator();
            String8.FromInteger(value, _typeConversionBuffer).WriteTo(_stream);
        }

        /// <summary>
        ///  Write a row separator and start the next row.
        ///  NextRow must be called after the row values are written.
        ///  NextRow validates that the correct number of values were written.
        /// </summary>
        public void NextRow()
        {
            if (_currentRowColumnCount != _columnCount) throw new TsvWriterException(String.Format("Wrote wrong number of columns for row {0:n0}. Wrote {1:n0}, expected {2:n0} columns.", _rowCount, _currentRowColumnCount, _columnCount));
            WriteRowSeparator();
        }

        private void WriteCellSeparator()
        {
            if (_currentRowColumnCount >= _columnCount) throw new TsvWriterException("Too many columns written to TsvWriter. Call NextRow after each row.");
            if (_currentRowColumnCount > 0) _stream.WriteByte(_cellDelimiter);
            _currentRowColumnCount++;
        }

        private void WriteRowSeparator()
        {
            _stream.WriteByte((byte)'\r');
            _stream.WriteByte((byte)'\n');
            _currentRowColumnCount = 0;
            _rowCount++;
        }

        /// <summary>
        ///  Return the line number of the current row being written, 1-based.
        ///  The header is line one, if it was written.
        /// </summary>
        public int LineNumber
        {
            get { return _rowCount; }
        }

        /// <summary>
        ///  Return how many bytes were written out so far.
        ///  If the stream wasn't at the beginning, includes previous writing.
        /// </summary>
        public long BytesWritten
        {
            get { return _stream.Position; }
        }

        /// <summary>
        ///  Dispose the TsvWriter.
        /// </summary>
        public void Dispose()
        {
            if (_stream != null)
            {
                if (_currentRowColumnCount > 0) WriteRowSeparator();
                _stream.Dispose();
                _stream = null;
            }
        }
    }

    [Serializable]
    public class TsvWriterException : Exception
    {
        public TsvWriterException() { }
        public TsvWriterException(string message) : base(message) { }
        public TsvWriterException(string message, Exception inner) : base(message, inner) { }
        protected TsvWriterException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

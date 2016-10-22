// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  BaseTabularWriter is a high performance writer base class for tabular formats
    ///  (columns in rows). Descendants specify how to separate cells and rows and how
    ///  to write string values (with proper escaping).
    ///  
    ///  Usage:
    ///  using (BaseTabularWriter w = new XWriter(writeToPath, new string[] { "Name", "IPs" }))
    ///  {
    ///     while(/* ... data source .. */)
    ///     {
    ///         w.Write(name);
    ///         w.Write(ips);
    ///         w.NextRow();
    ///     }
    /// }
    /// </summary>
    public abstract class BaseTabularWriter : IDisposable
    {
        private Stream _stream;
        private int _columnCount;
        private int _rowCount;
        private int _currentRowColumnCount;
        private byte[] _typeConversionBuffer;

        /// <summary>
        ///  Construct a new writer to write to the given file path.
        ///  The file is overwritten if it exists.
        /// </summary>
        /// <param name="filePath">Path to file to write.</param>
        /// <param name="columnNames">Column Names to write out.</param>
        /// <param name="writeHeaderRow">True to write a header row, False otherwise.</param>
        /// /// <param name="cellDelimiter">Delimiter between cells, default is tab.</param>
        public BaseTabularWriter(string filePath, IEnumerable<string> columnNames, bool writeHeaderRow = true) :
            this(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None), columnNames, writeHeaderRow)
        { }

        /// <summary>
        ///  Construct a new writer to write to the given stream.
        ///  The column name list is used to validate that rows have the right
        ///  number of columns written even if a header row isn't written.
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        /// <param name="columnNames">Column names to write.</param>
        /// <param name="writeHeaderRow">True to write a header row, False otherwise</param>
        /// <param name="cellDelimiter">Delimiter between cells, default is tab.</param>
        public BaseTabularWriter(Stream stream, IEnumerable<string> columnNames, bool writeHeaderRow = true)
        {
            _stream = stream;
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

            if (_columnCount == 0) throw new InvalidOperationException("No columns were passed to contructor. TSV must have at least one column.");
        }

        protected abstract void WriteCellValue(Stream stream, String8 value);
        protected abstract void WriteCellDelimiter(Stream stream);
        protected abstract void WriteRowSeparator(Stream stream);

        /// <summary>
        ///  Write a value to the current row.
        /// </summary>
        /// <param name="value">String8 to write to the current row.</param>
        public void Write(String8 value)
        {
            if (_currentRowColumnCount >= _columnCount) throw new InvalidOperationException(String.Format("Writing too many columns for row {0:n0}. Wrote {1:n0}, expected {2:n0} columns.", _rowCount, _currentRowColumnCount, _columnCount));
            if (_currentRowColumnCount > 0) WriteCellDelimiter(_stream);
            WriteCellValue(_stream, value);
            _currentRowColumnCount++;
        }

        /// <summary>
        ///  Write an integer to the current row.
        ///  The value is converted without allocations.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void Write(int value)
        {
            Write(String8.FromInteger(value, _typeConversionBuffer));
        }

        /// <summary>
        ///  Write a row separator and start the next row.
        ///  NextRow must be called after the row values are written.
        ///  NextRow validates that the correct number of values were written.
        /// </summary>
        public void NextRow()
        {
            if (_currentRowColumnCount != _columnCount) throw new InvalidOperationException(String.Format("Wrote wrong number of columns for row {0:n0}. Wrote {1:n0}, expected {2:n0} columns.", _rowCount, _currentRowColumnCount, _columnCount));
            WriteRowSeparator(_stream);

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
                if (_currentRowColumnCount > 0) WriteRowSeparator(_stream);
                _stream.Dispose();
                _stream = null;
            }
        }
    }
}

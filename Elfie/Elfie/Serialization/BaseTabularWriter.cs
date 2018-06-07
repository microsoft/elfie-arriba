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
    ///  using (BaseTabularWriter w = new XWriter(writeToPath))
    ///  {
    ///     w.SetColumns(new string[] { "Name", "IPs" });
    ///     
    ///     while(/* ... data source .. */)
    ///     {
    ///         w.Write(name);
    /// 
    ///         w.WriteValueStart();
    ///         for(int i = 0; i &lt; source.IPs.Count; ++i)
    ///         {
    ///             if(i > 0) w.WriteValuePart((byte)';');
    ///             w.WriteValuePart(source.IPs[i]);
    ///         }
    ///         w.WriteValueEnd();
    /// 
    ///         w.NextRow();
    ///     }
    /// }
    /// </summary>
    public abstract class BaseTabularWriter : ITabularWriter
    {
        private Stream _stream;
        private bool _writeHeaderRow;

        private int _columnCount;
        private int _rowCountWritten;
        private int _currentRowColumnCount;
        private bool _inPartialColumn;
        private byte[] _typeConversionBuffer;

        /// <summary>
        ///  Construct a new writer to write to the given file path.
        ///  The file is overwritten if it exists.
        /// </summary>
        /// <param name="filePath">Path to file to write.</param>
        /// <param name="writeHeaderRow">True to write a header row, False otherwise.</param>
        public BaseTabularWriter(string filePath, bool writeHeaderRow = true) :
            this(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None), writeHeaderRow)
        { }

        /// <summary>
        ///  Construct a new writer to write to the given stream.
        ///  The column name list is used to validate that rows have the right
        ///  number of columns written even if a header row isn't written.
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        /// <param name="writeHeaderRow">True to write a header row, False otherwise</param>
        public BaseTabularWriter(Stream stream, bool writeHeaderRow = true)
        {
            _stream = stream;
            _writeHeaderRow = writeHeaderRow;

            _currentRowColumnCount = 0;
            _rowCountWritten = 0;

            _typeConversionBuffer = new byte[30];
        }

        // Abstract methods. Descendants must implement writing whole cell values,
        // cell and row delimiters, and partial values (for concatenated results).

        protected abstract void WriteCellValue(Stream stream, String8 value);
        protected abstract void WriteCellDelimiter(Stream stream);
        protected abstract void WriteRowSeparator(Stream stream);
        protected abstract void WriteValueStart(Stream stream);
        protected abstract void WriteValuePart(Stream stream, String8 part);
        protected abstract void WriteValuePart(Stream stream, byte c);
        protected abstract void WriteValueEnd(Stream stream);

        /// <summary>
        ///  Identify the columns to be written.
        ///  Must be called before anything else.
        /// </summary>
        /// <param name="columnNames">Set of column names each row will write.</param>
        public void SetColumns(IEnumerable<string> columnNames)
        {
            if (_columnCount != 0) throw new InvalidOperationException("SetColumns may only be called once on a JsonTabularWriter.");
            _columnCount = columnNames.Count();

            // Write header row
            if (_writeHeaderRow)
            {
                String8Block buffer = new String8Block();
                foreach (string columnName in columnNames)
                {
                    Write(buffer.GetCopy(columnName));
                }

                NextRow();

                // Header row shouldn't count toward row count written.
                _rowCountWritten = 0;
            }

            if (_columnCount == 0) throw new InvalidOperationException("No columns were passed to contructor. TSV must have at least one column.");
        }

        /// <summary>
        ///  Write a value to the current row.
        /// </summary>
        /// <param name="value">String8 to write to the current row.</param>
        public void Write(String8 value)
        {
            if (_currentRowColumnCount >= _columnCount) throw new InvalidOperationException(String.Format("Writing too many columns for row {0:n0}. Wrote {1:n0}, expected {2:n0} columns.", _rowCountWritten, _currentRowColumnCount, _columnCount));
            if (_inPartialColumn) throw new InvalidOperationException("Write was called while in a multi-part column. Call WriteValueStart, WriteValuePart, and WriteValueEnd only for partial columns.");
            if (_currentRowColumnCount > 0) WriteCellDelimiter(_stream);
            WriteCellValue(_stream, value);
            _currentRowColumnCount++;
        }

        /// <summary>
        ///  Write a long to the current row.
        ///  The value is converted without allocations.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void Write(long value)
        {
            Write(String8.FromNumber(value, _typeConversionBuffer));
        }

        /// <summary>
        ///  Write a double to the current row.
        ///  The value is converted without allocations.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void Write(double value)
        {
            Write(String8.FromNumber(value, _typeConversionBuffer));
        }

        /// <summary>
        ///  Write a boolean to the current row.
        ///  The value is converted without allocations.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void Write(bool value)
        {
            Write(String8.FromBoolean(value));
        }

        /// <summary>
        ///  Write a single UTF8 byte to the current row.
        ///  The value is converted without allocations.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void Write(byte value)
        {
            WriteValueStart();
            WriteValuePart(value);
            WriteValueEnd();
        }

        /// <summary>
        ///  Write a single DateTime [UTC] to the current row.
        ///  The value is converted without allocations.
        /// </summary>
        /// <param name="value">Value to write</param>
        public void Write(DateTime value)
        {
            Write(String8.FromDateTime(value, _typeConversionBuffer));
        }

        /// <summary>
        ///  Write the beginning of a cell value which will be written in parts.
        ///  Used for concatenated values.
        /// </summary>
        public void WriteValueStart()
        {
            if (_currentRowColumnCount >= _columnCount) throw new InvalidOperationException(String.Format("Writing too many columns for row {0:n0}. Wrote {1:n0}, expected {2:n0} columns.", _rowCountWritten, _currentRowColumnCount, _columnCount));
            if (_currentRowColumnCount > 0) WriteCellDelimiter(_stream);
            _inPartialColumn = true;
            WriteValueStart(_stream);
        }

        /// <summary>
        ///  Write a value as part of the current cell value.
        /// </summary>
        /// <param name="part">String8 to write to the current cell.</param>
        public void WriteValuePart(String8 part)
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueStart must be called before WriteValuePart.");
            WriteValuePart(_stream, part);
        }

        /// <summary>
        ///  Write a single UTF8 byte as part of the current cell value.
        /// </summary>
        /// <param name="c">Character to write to the current cell.</param>
        public void WriteValuePart(byte c)
        {
            WriteValuePart(_stream, c);
        }

        /// <summary>
        ///  Write the end of a cell value which was written in parts.
        ///  Used for concatenated values.
        /// </summary>
        public void WriteValueEnd()
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueEnd called but WriteValueStart was never called.");

            _inPartialColumn = false;
            WriteValueEnd(_stream);
            _currentRowColumnCount++;
        }

        /// <summary>
        ///  Write a UTC DateTime as part of a single cell value.
        ///  Callers must call WriteValueStart and WriteValueEnd around WriteValuePart calls.
        /// </summary>
        /// <param name="part">Value to write</param>
        public void WriteValuePart(DateTime part)
        {
            WriteValuePart(String8.FromDateTime(part, _typeConversionBuffer));
        }

        /// <summary>
        ///  Write an integer as part of a single cell value.
        ///  Callers must call WriteValueStart and WriteValueEnd around WriteValuePart calls.
        /// </summary>
        /// <param name="part">Value to write</param>
        public void WriteValuePart(int part)
        {
            WriteValuePart(String8.FromInteger(part, _typeConversionBuffer));
        }

        /// <summary>
        ///  Write a boolean as part of a single cell value.
        ///  Callers must call WriteValueStart and WriteValueEnd around WriteValuePart calls.
        /// </summary>
        /// <param name="part">Value to write</param>
        public void WriteValuePart(bool part)
        {
            WriteValuePart(String8.FromBoolean(part));
        }

        /// <summary>
        ///  Write a row separator and start the next row.
        ///  NextRow must be called after the row values are written.
        ///  NextRow validates that the correct number of values were written.
        /// </summary>
        public void NextRow()
        {
            if (_currentRowColumnCount != _columnCount) throw new InvalidOperationException(String.Format("Wrote wrong number of columns for row {0:n0}. Wrote {1:n0}, expected {2:n0} columns.", _rowCountWritten, _currentRowColumnCount, _columnCount));
            WriteRowSeparator(_stream);

            _currentRowColumnCount = 0;
            _rowCountWritten++;
        }

        /// <summary>
        ///  Return the number of rows written so far.
        ///  In rows without newlines, one less than the line number of the current row.
        /// </summary>
        public int RowCountWritten
        {
            get { return _rowCountWritten; }
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

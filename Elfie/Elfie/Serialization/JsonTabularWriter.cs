// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  JsonTabularWriter writes tabular data to a compact JSON form.
    ///  This form writes the column names only once, adds minimal structural overhead,
    ///  and can still be easily read in JavaScript and translated to a normal "array of objects" form.
    /// 
    ///  {
    ///     "colIndex": { "ColumnName1": 0, "ColumnName2": 1, ... },
    ///     "rows": [
    ///        [ "Row1Col1", "Row1Col2" ],
    ///        [ "Row2Col1", "Row2Col2" ],
    ///        ...
    ///      ]
    ///  }
    ///  
    ///  Values can be read from JavaScript like:
    ///  var value = data.rows[rowIndex][data.colIndex["ColumnName"]];
    /// </summary>
    public class JsonTabularWriter : ITabularWriter
    {
        private static String8 s_beforeColumnNames = String8.Convert("{\n\"colIndex\": { ", new byte[16]);
        private static String8 s_afterColumnNames = String8.Convert(" },\n\"rows\": [\n", new byte[14]);
        private static String8 s_escapedCharPrefix = String8.Convert("\\u00", new byte[4]);
        private static String8 s_valueDelimiter = String8.Convert(", ", new byte[2]);

        private Stream _stream;
        private byte[] _typeConversionBuffer;

        private int _columnCount;

        private int _rowCountWritten;
        private int _currentRowColumnCount;
        private bool _inPartialColumn;

        /// <summary>
        ///  Construct a new writer to write to the given file path.
        ///  The file is overwritten if it exists.
        /// </summary>
        /// <param name="filePath">Path to file to write.</param>
        public JsonTabularWriter(string filePath) :
            this(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        { }

        /// <summary>
        ///  Construct a new writer to write to the given stream.
        ///  The column name list is used to validate that rows have the right
        ///  number of columns written even if a header row isn't written.
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        public JsonTabularWriter(Stream stream)
        {
            _stream = stream;
            _typeConversionBuffer = new byte[30];

            _rowCountWritten = 0;
            _currentRowColumnCount = 0;
            _inPartialColumn = false;
        }

        #region Escaping
        private byte ToHexDigit(int b)
        {
            return (b < 10 ? (byte)(UTF8.Zero + b) : (byte)(UTF8.A + b - 10));
        }

        /// <summary>
        ///  Write a single byte properly escaped for a quoted string.
        /// </summary>
        /// <param name="c">UTF8 byte to write</param>
        private void WriteEscaped(byte c)
        {
            if (c < 32)
            {
                s_escapedCharPrefix.WriteTo(_stream);
                _stream.WriteByte(ToHexDigit(c / 16));
                _stream.WriteByte(ToHexDigit(c & 0xF));
            }
            else if (c == UTF8.Quote || c == UTF8.Backslash)
            {
                _stream.WriteByte(UTF8.Backslash);
                _stream.WriteByte(c);
            }
            else
            {
                _stream.WriteByte(c);
            }
        }

        /// <summary>
        ///  Write UTF8 content escaped properly to be in double-quotes, but don't
        ///  write the surrounding quotes.
        /// </summary>
        /// <param name="value">UTF8 value to write</param>
        private void WriteEscaped(String8 value)
        {
            int nextWriteStartIndex = 0;

            int end = value.Index + value.Length;
            for (int i = value.Index; i < end; ++i)
            {
                byte c = value.Array[i];
                bool isControl = c < 32;

                if (isControl || c == UTF8.Backslash || c == UTF8.Quote)
                {
                    int inStringIndex = i - value.Index;

                    // Write everything before this escaped portion
                    value.Substring(nextWriteStartIndex, inStringIndex - nextWriteStartIndex).WriteTo(_stream);

                    // Write the escaped character
                    if (isControl)
                    {
                        s_escapedCharPrefix.WriteTo(_stream);
                        _stream.WriteByte(ToHexDigit(c / 16));
                        _stream.WriteByte(ToHexDigit(c & 0xF));
                    }
                    else
                    {
                        _stream.WriteByte(UTF8.Backslash);
                        _stream.WriteByte(c);
                    }

                    // Track the next block which doesn't need escaping
                    nextWriteStartIndex = inStringIndex + 1;
                }
            }

            // Write the trailing unescaped block
            value.Substring(nextWriteStartIndex).WriteTo(_stream);
        }
        #endregion

        public void SetColumns(IEnumerable<string> columnNames)
        {
            if (_columnCount != 0) throw new InvalidOperationException("SetColumns may only be called once on a JsonTabularWriter.");

            //  {
            //     "colIndex": { 
            s_beforeColumnNames.WriteTo(_stream);

            int columnIndex = 0;

            foreach (string columnName in columnNames)
            {
                int length = String8.GetLength(columnName);
                if (_typeConversionBuffer == null || _typeConversionBuffer.Length < length) _typeConversionBuffer = new byte[length];

                // , 
                if (columnIndex > 0) s_valueDelimiter.WriteTo(_stream);

                // "ColumnName"
                _stream.WriteByte(UTF8.Quote);
                WriteEscaped(String8.Convert(columnName, _typeConversionBuffer));
                _stream.WriteByte(UTF8.Quote);

                // : 0
                _stream.WriteByte(UTF8.Colon);
                _stream.WriteByte(UTF8.Space);
                String8.FromInteger(columnIndex, _typeConversionBuffer).WriteTo(_stream);
                columnIndex++;
            }

            // },
            // "rows": { 
            s_afterColumnNames.WriteTo(_stream);

            _columnCount = columnIndex;
        }

        public void NextRow()
        {
            if (_currentRowColumnCount != _columnCount) throw new InvalidOperationException(String.Format("Wrote wrong number of columns for row {0:n0}. Wrote {1:n0}, expected {2:n0} columns.", _rowCountWritten, _currentRowColumnCount, _columnCount));

            // Write ] to end this row array
            _stream.WriteByte(UTF8.Space);
            _stream.WriteByte(UTF8.RightBracket);

            // Comma and newline aren't written until next row begins

            _currentRowColumnCount = 0;
            _rowCountWritten++;
        }

        #region Write overloads
        private void WriteColumnSeparator()
        {
            if (_currentRowColumnCount >= _columnCount) throw new InvalidOperationException(String.Format("Writing too many columns for row {0:n0}. Wrote {1:n0}, expected {2:n0} columns.", _rowCountWritten, _currentRowColumnCount, _columnCount));
            if (_inPartialColumn) throw new InvalidOperationException("Write was called while in a multi-part column. Call WriteValueStart, WriteValuePart, and WriteValueEnd only for partial columns.");

            if (_currentRowColumnCount == 0)
            {
                // Write comma and newline after previous row
                if (_rowCountWritten > 0)
                {
                    _stream.WriteByte(UTF8.Comma);
                    _stream.WriteByte(UTF8.Newline);
                }

                // Right [ to start this row array
                _stream.WriteByte(UTF8.LeftBracket);
                _stream.WriteByte(UTF8.Space);
            }
            else
            {
                // ", "
                s_valueDelimiter.WriteTo(_stream);
            }

            _currentRowColumnCount++;
        }

        public void Write(bool value)
        {
            // Booleans are written without quotes and never need escaping. "ColumnName": true
            WriteColumnSeparator();
            String8.FromBoolean(value).WriteTo(_stream);
        }

        public void Write(long value)
        {
            // Numbers are written without quotes and never need escaping. "ColumnName": -1234
            WriteColumnSeparator();
            String8.FromNumber(value, _typeConversionBuffer).WriteTo(_stream);
        }

        public void Write(double value)
        {
            // Numbers are written without quotes and never need escaping. "ColumnName": -1234
            WriteColumnSeparator();
            String8.FromNumber(value, _typeConversionBuffer).WriteTo(_stream);
        }

        public void Write(String8 value)
        {
            // Strings are quoted and escaped. "ColumnName": "Value \"Quoted\"."
            WriteColumnSeparator();
            _stream.WriteByte(UTF8.Quote);
            WriteEscaped(value);
            _stream.WriteByte(UTF8.Quote);
        }

        public void Write(byte value)
        {
            // Bytes are quoted and escaped. "ColumnName": "\\"
            WriteColumnSeparator();
            _stream.WriteByte(UTF8.Quote);
            WriteEscaped(value);
            _stream.WriteByte(UTF8.Quote);
        }

        public void Write(DateTime value)
        {
            // DateTimes are quoted but never need escaping. "ColumnName": "2017-05-03T12:43:21Z"
            WriteColumnSeparator();
            _stream.WriteByte(UTF8.Quote);
            String8.FromDateTime(value, _typeConversionBuffer).WriteTo(_stream);
            _stream.WriteByte(UTF8.Quote);
        }

        public void WriteValueStart()
        {
            WriteColumnSeparator();
            _inPartialColumn = true;

            // Write the value leading quote
            _stream.WriteByte(UTF8.Quote);
        }

        public void WriteValueEnd()
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueEnd called but WriteValueStart was never called.");

            // Write the trailing quote
            _stream.WriteByte(UTF8.Quote);

            _inPartialColumn = false;
        }

        public void WriteValuePart(int part)
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueStart must be called before WriteValuePart.");
            String8.FromInteger(part, _typeConversionBuffer).WriteTo(_stream);
        }

        public void WriteValuePart(bool part)
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueStart must be called before WriteValuePart.");
            String8.FromBoolean(part).WriteTo(_stream);
        }

        public void WriteValuePart(DateTime value)
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueStart must be called before WriteValuePart.");
            String8.FromDateTime(value, _typeConversionBuffer).WriteTo(_stream);
        }

        public void WriteValuePart(byte c)
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueStart must be called before WriteValuePart.");
            WriteEscaped(c);
        }

        public void WriteValuePart(String8 value)
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueStart must be called before WriteValuePart.");
            WriteEscaped(value);
        }
        #endregion

        public long BytesWritten
        {
            get { return _stream.Position; }
        }

        public int RowCountWritten
        {
            get { return _rowCountWritten; }
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                // <closing 'rows'>, opened in SetColumn
                //    ]
                // }
                if (_columnCount > 0)
                {
                    _stream.WriteByte(UTF8.Newline);
                    _stream.WriteByte(UTF8.RightBracket);
                    _stream.WriteByte(UTF8.Newline);
                    _stream.WriteByte(UTF8.RightBrace);
                }

                _stream.Dispose();
                _stream = null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    public class JsonTabularWriter : ITabularWriter
    {
        private static String8 True8 = String8.Convert("true", new byte[4]);
        private static String8 False8 = String8.Convert("false", new byte[5]);

        private Stream _stream;

        private int _columnCount;
        private int _rowCountWritten;
        private int _currentRowColumnCount;
        private bool _inPartialColumn;
        private byte[] _typeConversionBuffer;

        private String8[] _columnNames;

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

            _currentRowColumnCount = 0;
            _rowCountWritten = 0;

            _typeConversionBuffer = new byte[30];
        }

        public void NextRow()
        {
            if (_currentRowColumnCount != _columnCount) throw new InvalidOperationException(String.Format("Wrote wrong number of columns for row {0:n0}. Wrote {1:n0}, expected {2:n0} columns.", _rowCountWritten, _currentRowColumnCount, _columnCount));

            _stream.WriteByte((byte)'}');

            _currentRowColumnCount = 0;
            _rowCountWritten++;
        }

        public void SetColumns(IEnumerable<string> columnNames)
        {
            List<String8> set = new List<String8>();

            foreach(string columnName in columnNames)
            {
                set.Add(String8.Convert(columnName, new byte[String8.GetLength(columnName)]));
            }

            this._columnNames = set.ToArray();
            _columnCount = this._columnNames.Length;
        }

        private void WriteColumnName()
        {
            // Start new object on new line
            if(_currentRowColumnCount == 0)
            {
                if(_rowCountWritten > 0)
                {
                    _stream.WriteByte(UTF8.Comma);
                    _stream.WriteByte(UTF8.Newline);
                }
                else
                {
                    _stream.WriteByte((byte)'[');
                    _stream.WriteByte(UTF8.Newline);
                }

                _stream.WriteByte((byte)'{');
            }

            // Write comma after previous value
            if (_currentRowColumnCount > 0) _stream.WriteByte(UTF8.Comma);

            // Write the column name
            _stream.WriteByte(UTF8.Quote);
            WriteValuePart(_columnNames[_currentRowColumnCount]);
            _stream.WriteByte(UTF8.Quote);

            // :
            _stream.WriteByte(UTF8.Colon);
        }

        private void WriteCellDelimiter(Stream stream)
        {
            stream.WriteByte((byte)',');
        }

        public void Write(bool value)
        {
            WriteUnwrapped(value ? True8 : False8);
        }

        public void Write(byte value)
        {
            WriteValueStart();
            WriteValuePart(value);
            WriteValueEnd();
        }

        public void Write(int value)
        {
            WriteUnwrapped(String8.FromInteger(value, _typeConversionBuffer));
        }

        public void Write(DateTime value)
        {
            Write(String8.FromDateTime(value, _typeConversionBuffer));
        }

        public void Write(String8 value)
        {
            if (_currentRowColumnCount >= _columnCount) throw new InvalidOperationException(String.Format("Writing too many columns for row {0:n0}. Wrote {1:n0}, expected {2:n0} columns.", _rowCountWritten, _currentRowColumnCount, _columnCount));
            if (_inPartialColumn) throw new InvalidOperationException("Write was called while in a multi-part column. Call WriteValueStart, WriteValuePart, and WriteValueEnd only for partial columns.");
            if (_currentRowColumnCount > 0) WriteCellDelimiter(_stream);
            _currentRowColumnCount++;

            WriteValueStart();
            WriteValuePart(value);
            WriteValueEnd();
        }

        protected void WriteUnwrapped(String8 value)
        {
            if (_currentRowColumnCount >= _columnCount) throw new InvalidOperationException(String.Format("Writing too many columns for row {0:n0}. Wrote {1:n0}, expected {2:n0} columns.", _rowCountWritten, _currentRowColumnCount, _columnCount));
            if (_inPartialColumn) throw new InvalidOperationException("Write was called while in a multi-part column. Call WriteValueStart, WriteValuePart, and WriteValueEnd only for partial columns.");
            if (_currentRowColumnCount > 0) WriteCellDelimiter(_stream);
            _currentRowColumnCount++;

            WriteColumnName();
            value.WriteTo(_stream);
        }

        public void WriteValueStart()
        {
            if (_currentRowColumnCount >= _columnCount) throw new InvalidOperationException(String.Format("Writing too many columns for row {0:n0}. Wrote {1:n0}, expected {2:n0} columns.", _rowCountWritten, _currentRowColumnCount, _columnCount));
            if (_currentRowColumnCount > 0) WriteCellDelimiter(_stream);
            _inPartialColumn = true;

            WriteColumnName();

            // Write the value leading quote
            _stream.WriteByte(UTF8.Quote);
        }

        public void WriteValueEnd()
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueEnd called but WriteValueStart was never called.");

            _inPartialColumn = false;
            _stream.WriteByte(UTF8.Quote);
            _currentRowColumnCount++;
        }

        public void WriteValuePart(int part)
        {
            String8.FromInteger(part, _typeConversionBuffer).WriteTo(_stream);
        }

        public void WriteValuePart(byte c)
        {
            _stream.WriteByte(c);
        }

        public void WriteValuePart(bool part)
        {
            (part ? True8 : False8).WriteTo(_stream);
        }

        public void WriteValuePart(DateTime value)
        {
            String8.FromDateTime(value, _typeConversionBuffer).WriteTo(_stream);
        }

        public void WriteValuePart(String8 value)
        {
            int nextWriteStartIndex = 0;
            int end = value._index + value._length;
            for (int i = value._index; i < end; ++i)
            {
                byte c = value._buffer[i];
                if (c == UTF8.Backslash || c == UTF8.Slash || c == UTF8.Quote || c < 16)
                {
                    int inStringIndex = i - value._index;

                    // Write everything before this escaped portion
                    value.Substring(nextWriteStartIndex, inStringIndex - nextWriteStartIndex).WriteTo(_stream);

                    // Write backslash
                    _stream.WriteByte(UTF8.Backslash);

                    if (c < 16)
                    {
                        // Control Chars: write \uXXXX
                        _stream.WriteByte((byte)'u');
                        _stream.WriteByte(UTF8.Zero);
                        _stream.WriteByte(UTF8.Zero);
                        _stream.WriteByte(UTF8.Zero);

                        if (c < 10)
                        {
                            _stream.WriteByte((byte)(UTF8.Zero + c));
                        }
                        else
                        {
                            _stream.WriteByte((byte)(UTF8.a + c - 10));
                        }
                    }
                    else
                    {
                        // Others: write \\, \/, \"
                        _stream.WriteByte(c);
                    }

                    nextWriteStartIndex = inStringIndex + 1;
                }
            }

            value.Substring(nextWriteStartIndex).WriteTo(_stream);
        }

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
                if(_rowCountWritten > 0)
                {
                    _stream.WriteByte(UTF8.Newline);
                    _stream.WriteByte((byte)']');
                }
                
                _stream.Dispose();
                _stream = null;
            }
        }
    }
}

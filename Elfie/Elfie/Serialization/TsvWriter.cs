using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

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

        public TsvWriter(string tsvFilePath, IEnumerable<string> columnNames) : 
            this(new FileStream(tsvFilePath, FileMode.Create, FileAccess.Write, FileShare.None), columnNames)
        { }

        public TsvWriter(Stream stream, IEnumerable<string> columnNames)
        {
            _stream = stream;
            _cellDelimiter = (byte)'\t';
            _columnCount = columnNames.Count();
            _currentRowColumnCount = 0;
            _rowCount = 0;

            _typeConversionBuffer = new byte[30];

            // Write header row
            String8Block buffer = new String8Block();
            foreach (string columnName in columnNames)
            {
                Write(buffer.GetCopy(columnName));
            }

            NextRow();

            if (_columnCount == 0) throw new TsvWriterException("No columns were passed to contructor. TSV must have at least one column.");
        }

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

        public void Write(int value)
        {
            WriteCellSeparator();
            String8.FromInteger(value, _typeConversionBuffer).WriteTo(_stream);
        }

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

        public int RowNumber
        {
            get { return _rowCount; }
        }

        public long BytesWritten
        {
            get { return _stream.Position; }
        }

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

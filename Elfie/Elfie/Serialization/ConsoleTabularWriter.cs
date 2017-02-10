using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  ConsoleTabularWriter emits rows directly to Console.Out.
    ///  It makes reporting code agnostic between writing files or writing to the Console.
    /// </summary>
    public class ConsoleTabularWriter : ITabularWriter
    {
        private StreamWriter _consoleOutStream;

        private int _currentRowColumnCount;
        private int _rowCountWritten;

        private int _columnCount;
        private List<string> _columnNames;

        public ConsoleTabularWriter()
        {
            _consoleOutStream = new StreamWriter(Console.OpenStandardOutput());
        }

        public long BytesWritten
        {
            get { return -1; }
        }

        public int RowCountWritten
        {
            get { return _rowCountWritten;  }
        }

        public void SetColumns(IEnumerable<string> columnNames)
        {
            _columnNames = new List<string>(columnNames);
            _columnCount = _columnNames.Count;

            for (int i = 0; i < _columnNames.Count; ++i)
            {
                if (i > 0) _consoleOutStream.Write('\t');
                _consoleOutStream.Write(_columnNames[i]);
            }

            _consoleOutStream.WriteLine();
        }

        public void Write(bool value)
        {
            if (_currentRowColumnCount++ > 0) _consoleOutStream.Write('\t');
            _consoleOutStream.Write(value);
        }

        public void Write(int value)
        {
            if (_currentRowColumnCount++ > 0) _consoleOutStream.Write('\t');
            _consoleOutStream.Write(value);
        }

        public void Write(String8 value)
        {
            if (_currentRowColumnCount++ > 0) _consoleOutStream.Write('\t');
            value.WriteTo(_consoleOutStream);
        }

        public void WriteValueStart()
        {
            if (_currentRowColumnCount++ > 0) _consoleOutStream.Write('\t');
        }

        public void WriteValuePart(int part)
        {
            _consoleOutStream.Write(part);
        }

        public void WriteValuePart(bool part)
        {
            _consoleOutStream.Write(part);
        }

        public void WriteValuePart(byte c)
        {
            _consoleOutStream.Write((char)c);
        }

        public void WriteValuePart(String8 part)
        {
            part.WriteTo(_consoleOutStream);
        }

        public void WriteValueEnd()
        { }

        public void NextRow()
        {
            this._consoleOutStream.WriteLine();

            _rowCountWritten++;
            _currentRowColumnCount = 0;
        }

        public void Dispose()
        {
            if(this._consoleOutStream != null)
            {
                this._consoleOutStream.Dispose();
                this._consoleOutStream = null;
            }
        }
    }
}

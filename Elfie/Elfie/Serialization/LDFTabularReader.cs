using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    public class LdfTabularReader : ITabularReader
    {
        private Stream _stream;

        private String8Block _columnNamesBlock;
        private Dictionary<string, int> _columnIndices;
        private List<string> _columnNames;

        private byte[] _buffer;
        private String8Set _blockLines;
        private PartialArray<int> _linePositionArray;
        private int _nextRowFirstLine;

        public IReadOnlyList<string> Columns => _columnNames;

        public int RowCountRead { get; private set; }

        public long BytesRead => _stream.Position;

        public int CurrentRowColumns => throw new System.NotImplementedException();

        public LdfTabularReader(string filePath) : this(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        { }

        public LdfTabularReader(Stream stream)
        {
            _stream = stream;

            _columnNamesBlock = new String8Block();
            _columnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _columnNames = new List<string>();

            _buffer = new byte[64 * 1024];
            _linePositionArray = new PartialArray<int>();

            ReadColumns();
        }

        #region ReadColumns
        private void ReadColumns()
        {
            // Allocate a fixed array to hold split lines
            _linePositionArray = new PartialArray<int>(1024);

            // Allocate a block to hold copies of unique column names
            _columnNamesBlock = new String8Block();

            // Walk the whole LDF by line looking for every unique column name found
            Dictionary<String8, int> columnsFound = new Dictionary<String8, int>();
            int lengthRead = _stream.Read(_buffer, 0, _buffer.Length);

            while (true)
            {
                // Read a block from the file
                String8 block = new String8(_buffer, 0, lengthRead);

                // Split the block into lines
                String8Set lines = block.Split(UTF8.Newline, _linePositionArray);

                // Read and track all column names down to the second-to-last line
                for (int i = 0; i < lines.Count - 1; ++i)
                {
                    ReadColumnLine(lines[i], columnsFound);
                }

                String8 lastLine = lines[lines.Count - 1];

                // If we ran out of file, read the last line and stop
                if (lengthRead < _buffer.Length)
                {
                    ReadColumnLine(lastLine, columnsFound);
                    break;
                }

                // If this was one big line, double the buffer to read more
                if (lines.Count == 1)
                {
                    byte[] newBuffer = new byte[_buffer.Length * 2];
                    System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _buffer.Length);
                    _buffer = newBuffer;
                }

                // Save the last line and read another block
                System.Buffer.BlockCopy(_buffer, 0, _buffer, lastLine._index, lastLine.Length);
                lengthRead = lastLine.Length + _stream.Read(_buffer, lastLine.Length, _buffer.Length - lastLine.Length);
            }
        }

        private void ReadColumnLine(String8 line, Dictionary<String8, int> columnsFound)
        {
            // Skip empty and continuation lines
            if (line.Length == 0 || line[0] == UTF8.CR || line[0] == UTF8.Space) return;

            // Find the column name part of the line
            String8 columnName = line.BeforeFirst(UTF8.Colon);

            // If we haven't seen this column name before, add it to our collection
            if (!columnName.IsEmpty() && !columnsFound.ContainsKey(columnName))
            {
                int columnIndex = columnsFound.Count;
                columnsFound[_columnNamesBlock.GetCopy(columnName)] = columnIndex;

                string columnNameString = columnName.ToString();
                _columnNames.Add(columnNameString);
                _columnIndices[columnNameString] = columnIndex;
            }
        }

        #endregion

        protected String8Set Split(String8 block, PartialArray<int> rowPositionArray, PartialArray<int> cellPositionArray)
        {
            // Split every line
            block.Split(UTF8.Newline, cellPositionArray);

            // Identify row boundaries (a completely empty line [\n\n or \r\n\r\n] is a new 'row')
            rowPositionArray.Clear();

            for (int i = 1; i < cellPositionArray.Count; ++i)
            {
                int difference = cellPositionArray[i] - cellPositionArray[i - 1];

                if (difference == 1)
                {
                    // \n\n is a row boundary
                    rowPositionArray.Add(cellPositionArray[i]);
                }
                else if (difference == 2)
                {
                    // \n\r\n is a row boundary
                    if (block[cellPositionArray[i] - 1] == UTF8.CR)
                    {
                        rowPositionArray.Add(cellPositionArray[i]);
                    }
                }
            }

            return new String8Set(block, 1, rowPositionArray);
        }

        public bool TryGetColumnIndex(string columnNameOrIndex, out int columnIndex)
        {
            if (_columnIndices.TryGetValue(columnNameOrIndex, out columnIndex)) return true;
            if (int.TryParse(columnNameOrIndex, out columnIndex) && columnIndex >= 0 && columnIndex < _columnNames.Count) return true;

            columnIndex = -1;
            return false;
        }

        public ITabularValue Current(int index)
        {
            throw new System.NotImplementedException();
        }

        public bool NextRow()
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            if(_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }
    }
}

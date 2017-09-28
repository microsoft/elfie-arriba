using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{


    public class LdfTabularReader : ITabularReader
    {
        private static String8 MultiValueDelimiter = String8.Convert("; ", new byte[2]);

        private Stream _stream;

        private String8Block _columnNamesBlock;
        private Dictionary<String8, int> _columnIndices8;
        private Dictionary<string, int> _columnIndices;
        private List<string> _columnNames;

        private byte[] _buffer;
        private String8Set _blockLines;
        private PartialArray<int> _linePositionArray;
        private int _nextRowFirstLineIndex;

        private String8TabularValue[] _currentRowValues;

        public IReadOnlyList<string> Columns => _columnNames;
        public int CurrentRowColumns => _columnNames.Count;
        public long BytesRead => _stream.Position;

        public int RowCountRead { get; private set; }

        public LdfTabularReader(string filePath) : this(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        { }

        public LdfTabularReader(Stream stream)
        {
            _stream = stream;

            _columnNamesBlock = new String8Block();
            _columnIndices8 = new Dictionary<String8, int>();
            _columnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _columnNames = new List<string>();

            _buffer = new byte[64 * 1024];
            _linePositionArray = new PartialArray<int>();

            ReadColumns();

            _currentRowValues = new String8TabularValue[_columnNames.Count];
            for(int i = 0; i < _currentRowValues.Length; ++i)
            {
                _currentRowValues[i] = new String8TabularValue();
            }
        }

        #region ReadColumns
        private void ReadColumns()
        {
            // Allocate a fixed array to hold split lines
            _linePositionArray = new PartialArray<int>(1024);

            // Allocate a block to hold copies of unique column names
            _columnNamesBlock = new String8Block();

            // Walk the whole LDF by line looking for every unique column name found
            int lengthRead = _stream.Read(_buffer, 0, _buffer.Length);

            while (true)
            {
                // Read a block from the file
                String8 block = new String8(_buffer, 0, lengthRead);

                // Split the block into lines
                _blockLines = block.Split(UTF8.Newline, _linePositionArray);

                // Read and track all column names down to the second-to-last line
                for (int i = 0; i < _blockLines.Count - 1; ++i)
                {
                    ReadColumnLine(_blockLines[i]);
                }

                String8 lastLine = _blockLines[_blockLines.Count - 1];

                // If we ran out of file, read the last line and stop
                if (lengthRead < _buffer.Length)
                {
                    ReadColumnLine(lastLine);
                    break;
                }

                // If this was one big line, double the buffer to read more
                if (_blockLines.Count == 1)
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

        private void ReadColumnLine(String8 line)
        {
            // Skip empty and continuation lines
            if (line.Length == 0 || line[0] == UTF8.CR || line[0] == UTF8.Space) return;

            // Find the column name part of the line
            String8 columnName = line.BeforeFirst(UTF8.Colon);

            // If we haven't seen this column name before, add it to our collection
            if (!columnName.IsEmpty() && !_columnIndices8.ContainsKey(columnName))
            {
                int columnIndex = _columnIndices8.Count;
                _columnIndices8[_columnNamesBlock.GetCopy(columnName)] = columnIndex;

                string columnNameString = columnName.ToString();
                _columnNames.Add(columnNameString);
                _columnIndices[columnNameString] = columnIndex;
            }
        }
        #endregion

        public bool TryGetColumnIndex(string columnNameOrIndex, out int columnIndex)
        {
            if (_columnIndices.TryGetValue(columnNameOrIndex, out columnIndex)) return true;
            if (int.TryParse(columnNameOrIndex, out columnIndex) && columnIndex >= 0 && columnIndex < _columnNames.Count) return true;

            columnIndex = -1;
            return false;
        }

        public ITabularValue Current(int index)
        {
            return _currentRowValues[index];
        }

        public bool NextRow()
        {
            return false;
        }

        //    // Clear values for row
        //    for(int i = 0; i < _currentRowValues.Length; ++i)
        //    {
        //        _currentRowValues[i].SetValue(String8.Empty);
        //    }

        //    // Read available complete lines
        //    String8 currentPropertyName = String8.Empty;
        //    String8 currentPropertyValue = String8.Empty;

        //    int currentLineIndex = _nextRowFirstLineIndex;
        //    while (currentLineIndex < _blockLines.Count - 1)
        //    {
        //        String8 line = _blockLines[currentLineIndex];

        //        // Trim trailing CR, if found
        //        if (line.EndsWith(UTF8.CR)) line = line.Substring(0, line.Length - 1);

        //        // An empty line means the end of this row
        //        if (line.Length == 0)
        //        {
        //            // Set the last value
        //            SetColumnValue(currentPropertyName, currentPropertyValue);

        //            // Return that a row was found
        //            return true;
        //        }

        //        // Look for a wrapped line
        //        if (line[0] == UTF8.Space)
        //        {
        //            // If found, concatenate the value after the space onto the value so far
        //            line = line.Substring(1);
        //            Buffer.BlockCopy(_buffer, line._index, _buffer, currentPropertyValue._index + currentPropertyValue.Length, line.Length);
        //            currentPropertyValue = new String8(_buffer, currentPropertyValue._index, currentPropertyValue._length + line.Length);
        //        }
        //        else
        //        {
        //            // Get the column name for this value
        //            String8 columnName = line.BeforeFirst(UTF8.Colon);

        //            // If it's the same as the previous one, concatenate the values
        //            // ISSUE: Are multi-value base64 values possible?
        //            if(currentColumnName.Equals(columnName))
        //            {
        //                line = line.Substring(columnName.Length + 2);

        //                Buffer.BlockCopy(_buffer, line._index, _buffer, currentPropertyValue._index + currentPropertyValue.Length, line.Length);

        //                currentPropertyValue = new String8(_buffer, currentPropertyValue._index, currentPropertyValue._length + line.Length);
        //            }
        //        }

        //        currentLineIndex++;
        //    }

        //    int lengthRead = _stream.Read(_buffer, 0, _buffer.Length);
        //    String8 block = new String8(_buffer, 0, lengthRead);
        //    _blockLines = block.Split(UTF8.Newline, _linePositionArray);
        //    _nextRowFirstLineIndex = 0;           
        //}

        //private void SetColumnValue(String8 currentPropertyName, String8 currentPropertyValue)
        //{
        //    // TODO: Unescape base64, translate SIDs, convert DateTimes
        //    int columnIndex;
        //    if(_columnIndices8.TryGetValue(currentPropertyName, out columnIndex))
        //    {

        //        _currentRowValues[columnIndex].SetValue(currentPropertyValue);
        //    }
        //}

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

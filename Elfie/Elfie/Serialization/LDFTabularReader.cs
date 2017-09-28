using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    public class LdfTabularReader : ITabularReader
    {
        private static String8 MultiValueDelimiter = String8.Convert(";", new byte[1]);

        private BufferedRowReader _reader;

        private String8Block _columnNamesBlock;
        private Dictionary<String8, int> _columnIndices8;
        private Dictionary<string, int> _columnIndices;
        private List<string> _columnNames;

        private String8Set _blockLines;
        private PartialArray<int> _lineArray;
        private int _nextLineIndex;

        private String8Block _currentRowBlock;
        private String8TabularValue[] _currentRowValues;

        public IReadOnlyList<string> Columns => _columnNames;
        public int CurrentRowColumns => _columnNames.Count;
        public long BytesRead => _reader.BytesRead;

        public int RowCountRead { get; private set; }

        public LdfTabularReader(string filePath) : this(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        { }

        public LdfTabularReader(Stream stream)
        {
            _reader = new BufferedRowReader(stream, SplitRows);

            _columnNamesBlock = new String8Block();
            _columnIndices8 = new Dictionary<String8, int>();
            _columnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _columnNames = new List<string>();

            _currentRowBlock = new String8Block();

            ReadColumns(stream);

            _currentRowValues = new String8TabularValue[_columnNames.Count];
            for (int i = 0; i < _currentRowValues.Length; ++i)
            {
                _currentRowValues[i] = new String8TabularValue();
            }
        }

        #region ReadColumns
        private void ReadColumns(Stream stream)
        {
            byte[] buffer = new byte[64 * 1024];
            _lineArray = new PartialArray<int>(1024, false);

            // Allocate a block to hold copies of unique column names
            _columnNamesBlock = new String8Block();

            // Walk the whole LDF by line looking for every unique column name found
            int lengthRead = stream.Read(buffer, 0, buffer.Length);

            while (true)
            {
                // Read a block from the file
                String8 block = new String8(buffer, 0, lengthRead);

                // Split the block into lines
                _blockLines = block.Split(UTF8.Newline, _lineArray);

                // Read and track all column names down to the second-to-last line
                for (int i = 0; i < _blockLines.Count - 1; ++i)
                {
                    ReadColumnLine(_blockLines[i]);
                }

                String8 lastLine = _blockLines[_blockLines.Count - 1];

                // If we ran out of file, read the last line and stop
                if (lengthRead < buffer.Length)
                {
                    ReadColumnLine(lastLine);
                    break;
                }

                // If this was one big line, double the buffer to read more
                if (_blockLines.Count == 1)
                {
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    System.Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
                    buffer = newBuffer;
                }

                // Save the last line and read another block
                System.Buffer.BlockCopy(buffer, 0, buffer, lastLine._index, lastLine.Length);
                lengthRead = lastLine.Length + stream.Read(buffer, lastLine.Length, buffer.Length - lastLine.Length);
            }

            stream.Seek(0, SeekOrigin.Begin);
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

        private String8Set SplitRows(String8 block, PartialArray<int> rowPositionArray)
        {
            // Split the block into lines (and save the split for use splitting columns)
            _blockLines = block.Split(UTF8.Newline, _lineArray);

            // Reset where which line the next row begins with
            _nextLineIndex = 0;

            rowPositionArray.Clear();
            rowPositionArray.Add(0);

            for (int i = 0; i < _blockLines.Count; ++i)
            {
                String8 line = _blockLines[i];

                // An empty line (or \n\r\n) indicates a new logical row
                if (line.Length == 0 || (line.Length == 1 && line[0] == UTF8.CR))
                {
                    rowPositionArray.Add(_lineArray[i + 1]);
                }
            }

            rowPositionArray.Add(_lineArray[_blockLines.Count]);

            return new String8Set(block, 1, rowPositionArray);
        }

        public bool NextRow()
        {
            _currentRowBlock.Clear();

            String8 row = _reader.NextRow();
            if (row.IsEmpty()) return false;

            // Clear values for row
            for (int i = 0; i < _currentRowValues.Length; ++i)
            {
                _currentRowValues[i].SetValue(String8.Empty);
            }

            // Read available complete lines
            String8 currentPropertyName = String8.Empty;
            String8 currentPropertyValue = String8.Empty;
            bool currentIsBase64 = false;

            int currentLineIndex = _nextLineIndex;
            while (currentLineIndex < _blockLines.Count)
            {
                String8 line = _blockLines[currentLineIndex];

                // Trim trailing CR, if found
                if (line.EndsWith(UTF8.CR)) line = line.Substring(0, line.Length - 1);

                // An empty line or out of lines for the row range
                if (line.Length == 0 || line._index >= row._index + row._length) break;

                // Look for a wrapped line
                if (line[0] == UTF8.Space)
                {
                    // If found, concatenate the value after the space onto the value so far
                    line = line.Substring(1);
                    currentPropertyValue = _currentRowBlock.Concatenate(currentPropertyValue, String8.Empty, line);
                }
                else
                {
                    // Set or Append the value just completed
                    SetColumnValue(currentPropertyName, currentPropertyValue, currentIsBase64);

                    // Split the property name and value [value is after colon and space]
                    currentPropertyName = line.BeforeFirst(UTF8.Colon);
                    currentPropertyValue = line.Substring(currentPropertyName.Length + 2);

                    // Determine if the value is encoded
                    currentIsBase64 = (line[currentPropertyName.Length + 1] == UTF8.Colon);
                    if (currentIsBase64) currentPropertyValue = currentPropertyValue.Substring(1);                    
                }

                currentLineIndex++;
            }

            SetColumnValue(currentPropertyName, currentPropertyValue, currentIsBase64);
            _nextLineIndex = currentLineIndex + 1;

            this.RowCountRead++;
            return true;
        }

        private void SetColumnValue(String8 currentPropertyName, String8 currentPropertyValue, bool isEncoded)
        {
            if (currentPropertyName.IsEmpty()) return;

            if(isEncoded)
            {
                currentPropertyValue = DecodeBase64(currentPropertyValue);
            }

            if (currentPropertyName.CompareTo("objectSid", true) == 0)
            {
                currentPropertyValue = DecodeSid(currentPropertyValue);
            }

            int columnIndex;
            if (_columnIndices8.TryGetValue(currentPropertyName, out columnIndex))
            {
                String8 previousValue = _currentRowValues[columnIndex].ToString8();
                if (!previousValue.IsEmpty())
                {
                    currentPropertyValue = _currentRowBlock.Concatenate(previousValue, MultiValueDelimiter, currentPropertyValue);
                }

                _currentRowValues[columnIndex].SetValue(currentPropertyValue);
            }
        }

        private String8 DecodeBase64(String8 value)
        {
            // Horrible
            byte[] decoded = Convert.FromBase64String(value.ToString());
            return _currentRowBlock.GetCopy(new String8(decoded, 0, decoded.Length));
        }

        private String8 DecodeSid(String8 sidBytes)
        {
            // Slightly Horrible
            SecurityIdentifier sid = new SecurityIdentifier(sidBytes._buffer, sidBytes._index);
            return _currentRowBlock.GetCopy(sid.ToString());
        }

        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  LdfTabularReader is a tabular reader for the LDF or LDIF format, which
    ///  Active Directory tools like LDIFDE produce.
    ///  
    ///  See:
    ///  https://en.wikipedia.org/wiki/LDAP_Data_Interchange_Format.
    ///  http://www.ietf.org/rfc/rfc2425.txt
    ///  
    ///  This implementation internally decodes:
    ///     'objectSid', if seen, into an SDDL formatted SID.
    /// </summary>
    public class LdfTabularReader : ITabularReader
    {
        private static String8 s_multiValueDelimiter = String8.Convert(";", new byte[1]);

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
            _lineArray = new PartialArray<int>(1024, false);

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
            // Make a reader to split the input on newlines
            BufferedRowReader reader = new BufferedRowReader(stream, (block, array) => block.Split(UTF8.Newline, array));

            // Scan the lines for column names (something before a colon)
            while (true)
            {
                String8 line = reader.NextRow();
                if (line.IsEmpty()) break;

                ReadColumnLine(line);
            }

            // Reset the stream for the second read
            stream.Seek(0, SeekOrigin.Begin);
        }

        private void ReadColumnLine(String8 line)
        {
            // Skip empty lines
            if (line.Length == 0) return;

            // Skip record separator, continuation lines, comments, and grouping lines
            byte first = line[0];
            if (first == UTF8.CR || first == UTF8.Space || first == UTF8.Pound || first == UTF8.Dash) return;

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

            for (int i = 0; i < _blockLines.Count - 1; ++i)
            {
                String8 line = _blockLines[i];

                // An empty line (or \n\r\n) indicates a new logical row
                if (line.Length == 0 || (line.Length == 1 && line[0] == UTF8.CR))
                {
                    rowPositionArray.Add(_lineArray[i + 1]);
                }
            }

            rowPositionArray.Add(block.Length + 1);

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

            for (; _nextLineIndex < _blockLines.Count; ++_nextLineIndex)
            {
                String8 line = _blockLines[_nextLineIndex];

                // Skip comment lines and grouping lines
                if (line.StartsWith(UTF8.Pound) || line.StartsWith(UTF8.Dash)) continue;

                // Trim trailing CR, if found
                if (line.EndsWith(UTF8.CR)) line = line.Substring(0, line.Length - 1);

                // An empty line or out of lines for the row range
                if (line.Length == 0 || line.Index >= row.Index + row.Length) break;

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

                    // Split the property name and value [value is after colon and optional space]
                    currentPropertyName = line.BeforeFirst(UTF8.Colon);
                    currentPropertyValue = line.Substring(currentPropertyName.Length + 1);
                    if (currentPropertyValue.StartsWith(UTF8.Space)) currentPropertyValue = currentPropertyValue.Substring(1);

                    // Determine if the value is encoded
                    currentIsBase64 = (line[currentPropertyName.Length + 1] == UTF8.Colon);
                    if (currentIsBase64) currentPropertyValue = currentPropertyValue.Substring(1);
                }
            }

            // Set the last property value
            SetColumnValue(currentPropertyName, currentPropertyValue, currentIsBase64);

            // The next row starts after the row separator line
            _nextLineIndex++;

            this.RowCountRead++;
            return true;
        }

        private void SetColumnValue(String8 currentPropertyName, String8 currentPropertyValue, bool isEncoded)
        {
            if (currentPropertyName.IsEmpty()) return;

            if (isEncoded)
            {
                currentPropertyValue = DecodeBase64(currentPropertyValue);
            }

            if (currentPropertyName.CompareTo("objectSid", true) == 0)
            {
                currentPropertyValue = DecodeSid(currentPropertyValue);
            }

            // TODO: Decode ".0Z" time format

            // TODO: Decode NT file time format

            int columnIndex;
            if (_columnIndices8.TryGetValue(currentPropertyName, out columnIndex))
            {
                String8 previousValue = _currentRowValues[columnIndex].ToString8();
                if (!previousValue.IsEmpty())
                {
                    currentPropertyValue = _currentRowBlock.Concatenate(previousValue, s_multiValueDelimiter, currentPropertyValue);
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
            SecurityIdentifier sid = new SecurityIdentifier(sidBytes.Array, sidBytes.Index);
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

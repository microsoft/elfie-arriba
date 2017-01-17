// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  BaseTabularReader is a high performance reader base class for tabular formats
    ///  (columns in rows). Descendants specify how to split rows and cells, including
    ///  unescaping values.
    ///  
    ///  BaseTabularReader allocates a single 64KB byte[] buffer and reads into it.
    ///  Values returned from CurrentRow(index) are String8 structs referencing the buffer,
    ///  so no other allocations occur during reading.
    ///  
    ///  String8 values *must be copied*, since subsequent NextRow calls will overwrite
    ///  the buffer content.
    ///  
    ///  Look up the indices of columns you want to read outside any loops.
    ///  
    ///  Usage:
    ///  using (BaseTabularReader r = new XReader(loadFromPath, true))
    ///  {
    ///     // Look up column indices outside the loop
    ///     int titleIndex = r.ColumnIndex("Title");
    ///     int descriptionIndex = r.ColumnIndex("Description");
    ///     int itemTypeIndex = r.ColumnIndex("ItemType");
    ///
    ///     // Use NextRow() and Current[index] to read values
    ///     while (r.NextRow())
    ///     {
    ///         String8 title = r.Current[titleIndex];
    ///         String8 description = r.Current[descriptionIndex];
    ///         int itemType = r.Current[itemTypeIndex].ToInteger();
    ///         
    ///         // COPY String8s to be kept
    ///     }
    /// }
    /// </summary>
    public abstract class BaseTabularReader : IDisposable
    {
        private Stream _reader;

        private List<string> _columnHeadingsList;
        private Dictionary<string, int> _columnHeadings;

        private byte[] _buffer;
        private int _rowCountRead;
        private int _nextRowIndexInBlock;
        private String8Set _currentBlock;
        private String8Set _currentRow;
        private PartialArray<int> _rowPositionArray;
        private PartialArray<int> _cellPositionArray;

        /// <summary>
        ///  Construct a BaseTabularReader to read the given file.
        /// </summary>
        /// <param name="filePath">File Path to file to read</param>
        /// <param name="hasHeaderRow">True to read the first row as column names, False not to pre-read anything</param>
        public BaseTabularReader(string filePath, bool hasHeaderRow = true) :
            this(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), hasHeaderRow)
        { }

        /// <summary>
        ///  Construct a BaseTabularReader to read the given stream.
        /// </summary>
        /// <param name="stream">Stream to read</param>
        /// <param name="hasHeaderRow">True to read the first row as column names, False not to pre-read anything</param>
        public BaseTabularReader(Stream stream, bool hasHeaderRow = true)
        {
            _reader = stream;

            _columnHeadingsList = new List<string>();
            _columnHeadings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            _buffer = new byte[64 * 1024];
            _rowCountRead = 0;
            _nextRowIndexInBlock = 0;
            _rowPositionArray = new PartialArray<int>(64, false);
            _cellPositionArray = new PartialArray<int>(1024, false);

            // Read the heading row and record heading positions
            if (hasHeaderRow)
            {
                if (!NextRow()) throw new IOException("Reader didn't find any rows when trying to read a header row.");

                for (int i = 0; i < _currentRow.Count; ++i)
                {
                    string columnName = this.Current[i].ToString();
                    _columnHeadingsList.Add(columnName);
                    _columnHeadings[columnName] = i;
                }
            }
        }

        protected abstract String8Set SplitCells(String8 row, PartialArray<int> cellPositionArray);

        protected abstract String8Set SplitRows(String8 block, PartialArray<int> rowPositionArray);

        /// <summary>
        ///  Return the column headings found. The set is empty if there was no heading row.
        /// </summary>
        public IReadOnlyList<string> Columns
        {
            get { return _columnHeadingsList; }
        }

        /// <summary>
        ///  Look up the column index of a given column by name.
        ///  Will only work if the TSV had a header row.
        ///  Column names are case insensitive.
        ///  Will throw if the column name wasn't found.
        /// </summary>
        /// <param name="columnNameOrIndex">Column name for which to find column index, or already an integer index</param>
        /// <returns>Index of column in TSV. Throws if column isn't found or no header row was read.</returns>
        public int ColumnIndex(string columnNameOrIndex)
        {
            int columnIndex = 0;

            // Try to find the column as a name
            if (_columnHeadings.TryGetValue(columnNameOrIndex, out columnIndex)) return columnIndex;

            // See if the column is a parsable integer index
            if (int.TryParse(columnNameOrIndex, out columnIndex) && _columnHeadings.Count > columnIndex) return columnIndex;

            throw new ColumnNotFoundException(String.Format("Column Name \"{0}\" not found in file.\nKnown Columns: \"{1}\"", columnNameOrIndex, String.Join(", ", _columnHeadings.Keys)));
        }

        /// <summary>
        ///  Return the cells for the current row.
        ///  Get a single cell with reader.Current[columnIndex]
        /// </summary>
        /// <returns>String8Set with the cells for the current row.</returns>
        public String8Set Current
        {
            get { return _currentRow; }
        }

        /// <summary>
        ///  Returns the number of rows read so far.
        ///  If no newlines in rows, the RowCountRead is the line number of the current row.
        /// </summary>
        public int RowCountRead
        {
            get { return _rowCountRead; }
        }

        /// <summary>
        ///  Returns the number of columns in the current row. Used to handle
        ///  rows which are shorter than expected, if desired.
        /// </summary>
        public int CurrentRowColumns
        {
            get { return _currentRow.Count; }
        }

        /// <summary>
        ///  Move the reader to the next row. This must be called before
        ///  reading the first row.
        /// </summary>
        /// <returns>True if another row exists, False if the TSV is out of content</returns>
        public bool NextRow()
        {
            // If we're on the last row, ask for more (we don't read the last row in case it was only partially read into the buffer)
            if (_nextRowIndexInBlock >= _currentBlock.Count - 1)
            {
                NextBlock();
            }

            // If there are no more rows, return false
            if (_nextRowIndexInBlock >= _currentBlock.Count) return false;

            // Get the next (complete) row from the current block
            String8 currentLine = _currentBlock[_nextRowIndexInBlock];

            // Strip leading UTF8 BOM, if found, on first row
            if (_rowCountRead == 0)
            {
                if (currentLine.Length >= 3 && currentLine[0] == 0xEF && currentLine[1] == 0xBB && currentLine[2] == 0xBF)
                {
                    currentLine = currentLine.Substring(3);
                }
            }

            // Split the line into cells
            _currentRow = SplitCells(currentLine, _cellPositionArray);

            _rowCountRead++;
            _nextRowIndexInBlock++;

            return true;
        }

        /// <summary>
        ///  NextBlock is called by NextRow before reading the last row in _currentBlock.
        ///  Since the file is read in blocks, the last row is usually incomplete.
        ///  
        ///  If there's more file content, NextBlock should copy the last row to the start
        ///  of the buffer, read more content, and reset _currentBlock to the new split rows
        ///  and _nextRowIndexInBlock to zero (telling NextRow to read that row next).
        ///  
        ///  If there's no more file, the last row is complete. NextBlock must return
        ///  without changing _currentBlock or _nextRowIndexInBlock to tell NextRow it's safe
        ///  to return to the user.
        ///  
        ///  NextRow will call NextBlock *again* after the last row. NextBlock must again
        ///  not change anything to tell NextRow that there's nothing left.
        ///  
        ///  So, NextBlock must:
        ///   - Copy the last row to the start of the buffer (if not already there)
        ///   - Read more content to fill the buffer
        ///   - Split the buffer into rows
        ///   - Stop at end-of-file or when a full row was read
        ///   - Double the buffer until one of these conditions is met
        ///   
        ///   - Reset nextRowInIndexBlock *only if* a row was shifted or read
        /// </summary>
        private void NextBlock()
        {
            int bufferLengthFilledStart = 0;

            // Copy the last row to the start of the buffer (if not already there)
            if (_currentBlock.Count > 1)
            {
                String8 lastRow = _currentBlock[_currentBlock.Count - 1];
                lastRow.WriteTo(_buffer, 0);
                bufferLengthFilledStart = lastRow.Length;

                // Reset the next row to read (since we shifted a row)
                _nextRowIndexInBlock = 0;
            }

            int bufferLengthFilled = bufferLengthFilledStart;

            while (true)
            {
                // Read more content to fill the buffer
                bufferLengthFilled += _reader.Read(_buffer, bufferLengthFilled, _buffer.Length - bufferLengthFilled);

                // Split the buffer into rows
                _currentBlock = SplitRows(new String8(_buffer, 0, bufferLengthFilled), _rowPositionArray);

                // Stop at end-of-file (read didn't fill buffer)
                if (bufferLengthFilled < _buffer.Length) break;

                // Stop when a full row was read (split found at least two parts)
                if (_currentBlock.Count > 1) break;

                // Otherwise, double the buffer (until a full row or end of file)
                byte[] newBuffer = new byte[_buffer.Length * 2];
                _buffer.CopyTo(newBuffer, 0);
                _buffer = newBuffer;
            }

            // If we read new content, reset the next row to read
            if (bufferLengthFilled > bufferLengthFilledStart) _nextRowIndexInBlock = 0;
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

    [Serializable]
    public class ColumnNotFoundException : Exception
    {
        public ColumnNotFoundException() { }
        public ColumnNotFoundException(string message) : base(message) { }
        public ColumnNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected ColumnNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

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
    ///  USAGE
    ///  =====
    ///  String8Block block = new String8Block();
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
    ///         // Copy values to be kept across rows
    ///         String8 title = block.GetCopy(r.Current[titleIndex]);
    ///         
    ///         // Use values directly if used only before NextRow
    ///         String8 description = r.Current[descriptionIndex].ToString8();
    ///         
    ///         // Use TryTo calls to convert values without allocation or boxing.
    ///         int itemType;
    ///         r.Current[itemTypeIndex].TryToInteger(out itemType);
    ///     }
    ///     
    ///     // Release String8Block memory used for copies when you're done with them
    ///     block.Clear();
    /// }
    /// </summary>
    public abstract class BaseTabularReader : ITabularReader
    {
        private BufferedRowReader _reader;

        protected List<string> _columnHeadingsList;
        protected Dictionary<string, int> _columnHeadings;

        private String8Set _currentRowColumns;
        private PartialArray<int> _cellPositionArray;

        private String8TabularValue[] _valueBoxes;

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
            _reader = new BufferedRowReader(stream, SplitRows);

            _columnHeadingsList = new List<string>();
            _columnHeadings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            _cellPositionArray = new PartialArray<int>(1024, false);

            // Read the heading row and record heading positions
            if (hasHeaderRow)
            {
                if (!NextRow()) throw new IOException("Reader didn't find any rows when trying to read a header row.");

                for (int i = 0; i < _currentRowColumns.Count; ++i)
                {
                    string columnName = this.Current(i).ToString();
                    _columnHeadingsList.Add(columnName);
                    _columnHeadings[columnName] = i;
                }

                // Header row doesn't count toward row count read
                RowCountRead = 0;
            }
        }

        // Break a row from the file into contiguous ranges for each logical cell
        protected abstract String8Set SplitCells(String8 row, PartialArray<int> cellPositionArray);

        // Break a block from the file into contiguous ranges for each logical row
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
        ///  Will only work if the file had a header row.
        ///  Column names are case insensitive.
        ///  Will set to -1 and return false if not found.
        /// </summary>
        /// <param name="columnNameOrIndex">Column name for which to find column index, or already an integer index</param>
        /// <param name="columnIndex">Column Index of column if found; -1 otherwise</param>
        /// <returns>True if column found; False otherwise</returns>
        public bool TryGetColumnIndex(string columnNameOrIndex, out int columnIndex)
        {
            // Try to find the column as a name
            if (_columnHeadings.TryGetValue(columnNameOrIndex, out columnIndex)) return true;

            // See if the column is a parsable integer index
            if (int.TryParse(columnNameOrIndex, out columnIndex) && columnIndex >= 0) return true;

            // Not found, so set to -1
            columnIndex = -1;
            return false;
        }

        /// <summary>
        ///  Return the cells for the current row.
        ///  Get a single cell with reader.Current[columnIndex].
        /// </summary>
        /// <returns>String8Set with the cells for the current row.</returns>
        public ITabularValue Current(int index)
        {
            _valueBoxes[index].SetValue(_currentRowColumns[index]);
            return _valueBoxes[index];
        }

        /// <summary>
        ///  Returns the number of rows read so far.
        ///  If no newlines in rows, the RowCountRead is the line number of the current row.
        /// </summary>
        public int RowCountRead { get; protected set; }

        /// <summary>
        ///  Return how many bytes were read so far.
        /// </summary>
        public long BytesRead
        {
            get { return _reader.BytesRead; }
        }

        /// <summary>
        ///  Returns the number of columns in the current row. Used to handle
        ///  rows which are shorter than expected, if desired.
        /// </summary>
        public int CurrentRowColumns
        {
            get { return _currentRowColumns.Count; }
        }

        /// <summary>
        ///  Move the reader to the next row. This must be called before
        ///  reading the first row.
        /// </summary>
        /// <returns>True if another row exists, False if the TSV is out of content</returns>
        public virtual bool NextRow()
        {
            String8 row = _reader.NextRow();
            if (row.IsEmpty()) return false;

            // Split the line into cells
            _currentRowColumns = SplitCells(row, _cellPositionArray);

            this.RowCountRead++;

            // Allocate a set of reusable String8TabularValues to avoid per-cell-value allocation or boxing.
            if (_valueBoxes == null || _valueBoxes.Length < _currentRowColumns.Count)
            {
                _valueBoxes = new String8TabularValue[_currentRowColumns.Count];

                for (int i = 0; i < _valueBoxes.Length; ++i)
                {
                    _valueBoxes[i] = new String8TabularValue();
                }
            }

            return true;
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

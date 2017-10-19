// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

using Arriba.Extensions;
using Arriba.Structures;

namespace Arriba.Serialization.Csv
{
    /// <summary>
    /// Reads CSV content from a stream. 
    /// </summary>
    public class CsvReader : IDisposable, IEnumerable<CsvRow>
    {
        private readonly StreamReader _reader = null;
        private readonly CsvReaderSettings _config = null;
        private readonly Dictionary<string, int> _columnNameLookup = null;
        private CsvRow _firstRow = null;

        private string[] _columnNames;
        private bool _initalRead = true;
        private bool _disposed = false;
        private int _columnCount;
        private long _streamInitialPosition = 0;


        public CsvReader(string inputFilePath, CsvReaderSettings config = null) :
            this(new FileStream(inputFilePath, FileMode.Open), config)
        { }

        /// <summary>
        /// Initializes a new CsvReader instance
        /// </summary>
        /// <param name="input">Input stream containing the csv file</param>
        /// <param name="config"></param>
        public CsvReader(Stream input, CsvReaderSettings readerConfig = null)
        {
            if (input == null) throw new ArgumentNullException("input");

            if (input.CanSeek)
            {
                _streamInitialPosition = input.Position;
            }

            if (readerConfig == null)
            {
                _config = new CsvReaderSettings();
            }
            else
            {
                _config = readerConfig;
            }

            _reader = new StreamReader(input, Encoding.UTF8, true, _config.ReadBufferSize, !_config.DisposeStream);
            _columnNameLookup = new Dictionary<string, int>(_config.ColumnNameComparer);

            _firstRow = this.ReadRows(attemptToYieldFromCache: false).FirstOrDefault();

            // Assume that the first row is the expected number of columns unless specified. 

            if (_firstRow == null)
            {
                throw new CsvReaderException("Failed to read first row from csv file.");
            }

            _columnCount = _firstRow.Length;

            string[] columnNames = null;


            // If we expect headers, set the column names to the values of the first row. 
            // Otherwise generate column names col1,col2,...
            if (_config.HasHeaders)
            {
                if (_config.TrimColumnNames)
                {
                    columnNames = _firstRow.StringColumns.Select(c => c.Trim()).ToArray();
                }
                else
                {
                    columnNames = _firstRow.StringColumns.ToArray();
                }
            }
            else
            {
                int startAt = _config.IncludeRowNumberAsColumn ? 0 : 1;
                columnNames = Enumerable.Range(startAt, _columnCount).Select(i => "col" + i).ToArray();
            }

            if (columnNames == null || columnNames.Length == 0)
            {
                throw new CsvReaderException("Unable to detect any cells");
            }

            if (_config.IncludeRowNumberAsColumn)
            {
                columnNames[0] = _config.RowNumberColumnName;
            }

            this.ColumnNames = columnNames;
        }

        /// <summary>
        /// Gets the count of columns within the CSV.
        /// </summary>
        /// <remarks>
        /// This is either the count of headers, or the count of columns in the first row.
        /// </remarks>
        public int ColumnCount
        {
            get
            {
                this.ThrowIfDisposed();
                return _columnCount;
            }
        }

        /// <summary>
        /// Gets the header titles for the csv file. 
        /// </summary>
        public string[] ColumnNames
        {
            get
            {
                this.ThrowIfDisposed();
                return _columnNames;
            }
            private set
            {
                _columnNames = value;

                _columnNameLookup.Clear();

                for (int i = 0; i < value.Length; i++)
                {
                    _columnNameLookup.Add(value[i], i);
                }
            }
        }

        /// <summary>
        /// Reads the Csv file as a data table 
        /// </summary>
        /// <returns>Data table</returns>
        public DataTable ReadAsDataTable()
        {
            DataTable dataTable = new DataTable();

            dataTable.BeginLoadData();
            dataTable.Columns.AddRange(this.ColumnNames.Select(col => new DataColumn(col)).ToArray());

            foreach (var row in this.Rows)
            {
                dataTable.Rows.Add(row.StringColumns.ToArray());
            }

            dataTable.EndLoadData();

            return dataTable;
        }


        public IEnumerable<DataBlock> ReadAsDataBlockBatch(int batchSize, bool allowPartialFile = false)
        {
            // Create an array per column
            Value[][] columns = new Value[this.ColumnCount][];
            for (int columnIndex = 0; columnIndex < this.ColumnCount; ++columnIndex)
            {
                columns[columnIndex] = new Value[batchSize];
            }

            int blockRowIndex = 0;
            IEnumerator<CsvRow> rows = this.Rows.GetEnumerator();

            while(true)
            {
                // Get the next row.Stop if invalid or end-of-file
                CsvRow row;
                try
                {
                    if (!rows.MoveNext()) break;
                    row = rows.Current;
                }
                catch(CsvReaderException) when (allowPartialFile)
                {
                    // Invalid Row. Stop.
                    break;
                }

                // Copy cell values into the column arrays
                for (int columnIndex = 0; columnIndex < this.ColumnCount; ++columnIndex)
                {
                    columns[columnIndex][blockRowIndex] = row[columnIndex];
                }

                blockRowIndex++;

                // If we have a full batch, wrap in a DataBlock and insert
                if (blockRowIndex == batchSize)
                {
                    yield return new DataBlock(this.ColumnNames, blockRowIndex, columns);
                    blockRowIndex = 0;
                }
            }

            // If there are remaining rows, insert them [arrays can be bigger than rowCount]
            if (blockRowIndex > 0)
            {
                yield return new DataBlock(this.ColumnNames, blockRowIndex, columns);
            }
        }

        /// <summary>
        /// Gets an enumerable of rows within the csv file. 
        /// </summary>
        public IEnumerable<CsvRow> Rows
        {
            get
            {
                this.ThrowIfDisposed();

                if (!_initalRead)
                {
                    ResetStream();
                }

                _initalRead = false;

                return this.ReadRows(attemptToYieldFromCache: true);
            }
        }

        private IEnumerable<CsvRow> ReadRows(bool attemptToYieldFromCache)
        {
            int rowNumber = 0;

            // Yield the first row, if asked to, and it does not contain headers. 
            if (attemptToYieldFromCache && !_config.HasHeaders)
            {
                yield return _firstRow;
                rowNumber++;
            }

            string line = null;
            bool unevenQuoted = true;
            int linesReadForRow = 0;

            // Foreach line in the buffer
            while ((line = _reader.ReadLine()) != null)
            {
                CsvCellRange[] ranges = null;

                rowNumber++;
                linesReadForRow = 1;

                do
                {
                    ranges = DelimeterRangeSplit(line, _config.Delimiter, _config.IncludeRowNumberAsColumn, out unevenQuoted, maximum: this.ColumnCount);

                    // Uneven number of quotes deteted, read the next line and add it to the current buffer. 
                    if (unevenQuoted)
                    {
                        linesReadForRow++;

                        if (_config.MaximumSingleCellLines > 0 && linesReadForRow > _config.MaximumSingleCellLines)
                        {
                            throw new CsvReaderException(
                                StringExtensions.Format("Attempted to read more than {0} lines for single row starting at line {1}. Likely mismatched quotes for a cell.", _config.MaximumSingleCellLines, rowNumber));
                        }

                        var newline = _reader.ReadLine();

                        if (newline == null)
                        {
                            ranges = null;
                            throw new CsvReaderException(
                                StringExtensions.Format("Reached end of file while attempting to parse a multi line cell starting at {0}.", rowNumber));
                        }

                        line = String.Concat(line, Environment.NewLine, newline);
                    }
                } while (unevenQuoted);

                // If an uneven buffer is detected, read a new line and combine it with the previous line, do this until we get an even
                // quoted buffer

                if (ranges != null)
                {
                    yield return new CsvRow(rowNumber, line, ranges, _columnNameLookup, _config);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the CSV reader. 
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (_config.DisposeStream)
            {
                _reader.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>Enumertor.</returns>
        public IEnumerator<CsvRow> GetEnumerator()
        {
            this.ThrowIfDisposed();
            return this.Rows.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>Enumertor.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            this.ThrowIfDisposed();
            return this.GetEnumerator();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        private void ResetStream()
        {
            if (!_reader.BaseStream.CanSeek)
            {
                throw new CsvReaderException("Cannot reset csv reader position the underlying stream does not support seeking.");
            }

            _reader.BaseStream.Seek(_streamInitialPosition, SeekOrigin.Begin);
            _reader.DiscardBufferedData();

            // Re-read the first row. This kind of sucks, be we can't seek to an exact position as the stream reader 
            // will buffer (event if we ask it not to, it has a minimum size of 128 bytes for a buffer), so we don't know
            // how far into the stream we are after the first read. 
            var unused = this.ReadRows(attemptToYieldFromCache: false).FirstOrDefault();
        }

        private unsafe static CsvCellRange[] DelimeterRangeSplit(string item, char delim, bool includeRowNumber, out bool uneven, int maximum = 0)
        {
            uneven = false;
            // Size to construct an unknown range array. 
            const int initialUnknownSize = 4;

            CsvCellRange[] row = new CsvCellRange[maximum == 0 ? initialUnknownSize : maximum];
            int currentColumn = 0;

            if (includeRowNumber)
            {
                row[0].IsRowNumberVirtualCell = true;
                currentColumn++;
            }

            const char quote = '\"';
            int quotes = 0;
            int current = 0;
            int length = item.Length;
            int trimSize = 0;
            bool endOfLine = false;
            CsvCellRange final;

            // Pointer enumeration for the string helps shave around 20% of the cost over foreach char in string. 
            // NOTE: A potential provement would be to do sizeof(intptr) enumeration, and mask 
            fixed (char* pBuff = item)
            {
                for (int i = 0; i <= length; i++)
                {
                    if (pBuff[i] == quote)
                    {
                        ++quotes;
                    }

                    current++;

                    endOfLine = pBuff[i] == '\0';

                    // Are we at the end of a cell? 
                    //  - End of line
                    //  - At a delimeter and not in a quote or next to a double quote
                    if (endOfLine ||
                       (pBuff[i] == delim && (quotes == 0 || ((pBuff[i - 1] == quote) && (quotes & 1) == 0))))
                    {
                        // Calculate how much to trim, if the cell is quoted, .e.g. "Value" this will strip the quotes at the start and end. 
                        trimSize = quotes >= 2 ? 1 : 0;

                        //It's a stuct, this is fine. 
                        final.Start = i - (current - 1) + trimSize;
                        final.Length = (current - 1) - (trimSize * 2);
                        final.ContainsQuotes = quotes > 2;
                        final.IsRowNumberVirtualCell = false;

                        // Dont add empty last items if we dont know the size, we're done, get outa here
                        if (maximum == 0 && endOfLine && final.Length == 0)
                        {
                            break;
                        }

                        row[currentColumn] = final;
                        currentColumn++;

                        // Is there enough room left? 
                        if (currentColumn == row.Length)
                        {
                            // If a maximum was specified, then exit, assume we've read enough 
                            if (maximum > 0)
                            {
                                break;
                            }

                            // Otherwise grow the array. 
                            Array.Resize(ref row, row.Length * 2);
                        }

                        // Safe to keep itereating, reset our tracking variables if we are not at the end of the string. 

                        if (!endOfLine)
                        {
                            current = 0;
                            quotes = 0;
                        }
                    }
                }
            }

            uneven = (quotes & 1) != 0;

            // If the size is not known, resize the array to the current column count
            if (maximum == 0)
            {
                Array.Resize(ref row, currentColumn);
            }

            return row;
        }
    }
}

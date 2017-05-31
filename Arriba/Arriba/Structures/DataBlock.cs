// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Arriba.Extensions;
using Arriba.Model.Column;

namespace Arriba.Structures
{
    /// <summary>
    ///  DataBlock contains an in-memory set of data - values accessible by
    ///  row and column. DataBlocks are passed to AddOrUpdate to edit data and
    ///  returned by Select when querying data.
    /// </summary>
    public class DataBlock
    {
        private object[] _valuesByColumn;
        public IList<ColumnDetails> Columns { get; private set; }
        public int ColumnCount { get; private set; }
        public int RowCount { get; private set; }

        /// <summary>
        ///  Construct a DataBlock with a given set of column names only
        ///  and a number of rows.
        /// </summary>
        /// <param name="columnNames">Names of columns in block</param>
        /// <param name="rowCount">Number of rows in block</param>
        public DataBlock(IEnumerable<string> columnNames, int rowCount) :
            this(ColumnDetails.FromNames(columnNames), rowCount)
        { }

        /// <summary>
        ///  Construct a DataBlock with a given set of column names and
        ///  the pre-built arrays for each row. [NOT EFFICIENT]
        /// </summary>
        /// <param name="columnNames">Names of columns in block</param>
        /// <param name="rowCount">Number of rows in block</param>
        public DataBlock(IEnumerable<string> columnNames, Array rows) :
            this(ColumnDetails.FromNames(columnNames), (rows == null ? 0 : rows.GetLength(0)))
        {
            if (rows == null) throw new ArgumentNullException("rows");

            for (int i = 0; i < this.RowCount; ++i)
            {
                Array rowArray = (Array)rows.GetValue(i);
                if (rowArray == null) throw new ArgumentNullException(String.Format("rows[{0}]", i));
                this.SetRow(i, rowArray);
            }
        }

        /// <summary>
        ///  Construct a DataBlock with a given set of column names and
        ///  the pre-built arrays for each column [allowing strongly typed columns].
        /// </summary>
        /// <param name="columnNames">Names of columns in block</param>
        /// <param name="columns">System.Array containing a System.Array per column</param>
        public DataBlock(IEnumerable<string> columnNames, int rowCount, Array columns) :
            this(ColumnDetails.FromNames(columnNames), rowCount, columns)
        { }

        /// <summary>
        ///  Construct a DataBlock with a given set of column names and
        ///  the pre-built arrays for each column [allowing strongly typed columns].
        /// </summary>
        /// <param name="columnNames">Names of columns in block</param>
        /// <param name="columns">System.Array containing a System.Array per column</param>
        public DataBlock(IList<ColumnDetails> columnDetails, int rowCount, Array columns) : this(columnDetails)
        {
            if (columns == null) throw new ArgumentNullException("columns");

            this.RowCount = rowCount;

            _valuesByColumn = new Array[this.ColumnCount];
            for (int i = 0; i < this.ColumnCount; ++i)
            {
                Array columnArray = (Array)columns.GetValue(i);
                if (columnArray == null) throw new ArgumentNullException(String.Format("columns[{0}]", i));
                this.SetColumn(i, columnArray);
            }
        }

        /// <summary>
        ///  Construct a DataBlock with a given set of columns (with details)
        ///  and a number of rows.
        /// </summary>
        /// <param name="details">Columns in the block</param>
        /// <param name="rowCount">Number of rows in block</param>
        public DataBlock(IList<ColumnDetails> details, int rowCount) : this(details)
        {
            this.RowCount = rowCount;
            _valuesByColumn = new object[this.ColumnCount];

            if (rowCount > 0)
            {
                for (int i = 0; i < this.ColumnCount; ++i)
                {
                    _valuesByColumn[i] = new object[rowCount];
                }
            }
        }

        /// <summary>
        ///  Construct a DataBlock for the given ColumnDetails but don't allocate arrays yet.
        /// </summary>
        /// <param name="details"></param>
        protected DataBlock(IList<ColumnDetails> details)
        {
            if (details == null) throw new ArgumentNullException("details");
            this.Columns = details;
            this.ColumnCount = this.Columns.Count;
        }

        #region Value Get/Set
        /// <summary>
        ///  Get or set the value for the given row and column. Use
        ///  GetValue and SetValue for better performance (inlining).
        /// </summary>
        /// <param name="rowIndex">Index of Row to get/set</param>
        /// <param name="columnIndex">Index of Column to get/set</param>
        /// <returns>Value at row/column provided.</returns>
        public object this[int rowIndex, int columnIndex]
        {
            get { return GetValue(rowIndex, columnIndex); }
            set { SetValue(rowIndex, columnIndex, value); }
        }

        /// <summary>
        ///  Get the value from the DataBlock for the given row and column.
        /// </summary>
        /// <param name="rowIndex">Index of row of value</param>
        /// <param name="columnIndex">index of column of value</param>
        /// <returns>Value at row, column</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetValue(int rowIndex, int columnIndex)
        {
            if (rowIndex >= this.RowCount || rowIndex < 0) throw new ArgumentOutOfRangeException("rowIndex");
            if (columnIndex >= this.ColumnCount || columnIndex < 0) throw new ArgumentOutOfRangeException("columnIndex");

            return ((Array)_valuesByColumn.GetValue(columnIndex)).GetValue(rowIndex);
        }

        /// <summary>
        ///  Set the value in the DataBlock for the given row and column.
        /// </summary>
        /// <param name="rowIndex">Index of row of value</param>
        /// <param name="columnIndex">Index of column of value</param>
        /// <param name="value">Value to set</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(int rowIndex, int columnIndex, object value)
        {
            if (columnIndex >= this.ColumnCount || columnIndex < 0) throw new ArgumentOutOfRangeException("columnIndex");

            // Allow setting rowIndex above RowCount if the column array is large enough [paging scenarios]
            Array columnArray = (Array)_valuesByColumn.GetValue(columnIndex);
            if (rowIndex >= columnArray.GetLength(0) || rowIndex < 0) throw new ArgumentOutOfRangeException("rowIndex");

            columnArray.SetValue(value, rowIndex);
        }

        /// <summary>
        ///  Set an entire row of values at once.
        /// </summary>
        /// <param name="rowIndex">RowIndex of Row to Update</param>
        /// <param name="values">Values for each column in the row.</param>
        public void SetRow(int rowIndex, Array values)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length != this.ColumnCount) throw new ArgumentException(StringExtensions.Format("Unable to set row values; {0} columns in block but {1} values passed for row.", this.ColumnCount, values.Length));

            for (int columnIndex = 0; columnIndex < this.ColumnCount; ++columnIndex)
            {
                SetValue(rowIndex, columnIndex, values.GetValue(columnIndex));
            }
        }

        /// <summary>
        ///  Set an entire column of values at once.
        /// </summary>
        /// <param name="columnIndex">ColumnIndex of Column to Update</param>
        /// <param name="values">Values for each row in the column.</param>
        public void SetColumn(int columnIndex, Array values)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length < this.RowCount) throw new ArgumentException(StringExtensions.Format("Unable to set column values; {0} rows in block but {1} values passed for column.", this.ColumnCount, values.Length));

            // Set the entire column at once
            _valuesByColumn.SetValue(values, columnIndex);
        }

        /// <summary>
        ///  Get a column of values directly.
        ///  
        /// NOTE: The Array may be longer than the DataBlock.RowCount.
        /// Only the first RowCount items are valid. 
        /// </summary>
        /// <param name="columnIndex">Index of column to return</param>
        /// <returns>Array of values from column</returns>
        internal Array GetColumn(int columnIndex)
        {
            return (Array)_valuesByColumn.GetValue(columnIndex);
        }
        #endregion

        /// <summary>
        ///  Return the index of the column with the given name in this DataBlock or -1
        ///  if it was not found.
        /// </summary>
        /// <param name="name">Name of column to find</param>
        /// <returns>Index of Column or -1 if not found</returns>
        public int IndexOfColumn(string name)
        {
            for (int columnIndex = 0; columnIndex < this.ColumnCount; ++columnIndex)
            {
                if (this.Columns[columnIndex].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return columnIndex;
                }
            }

            return -1;
        }

        /// <summary>
        ///  Return this DataBlock with the same rows but only the requested
        ///  subset of columns. [Debuggability]
        /// </summary>
        /// <param name="columns">Column Names to keep</param>
        /// <returns>DataBlock with same rows but only requested columns</returns>
        public DataBlock StripToColumns(ICollection<string> columns)
        {
            if (columns == null) throw new ArgumentNullException("columns");

            // Find the column details and indexes for the columns to strip
            List<ColumnDetails> strippedColumns = new List<ColumnDetails>();
            List<int> strippedColumnIndexes = new List<int>();

            foreach (string columnName in columns)
            {
                int i;
                for (i = 0; i < this.ColumnCount; ++i)
                {
                    if (this.Columns[i].Name.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        strippedColumns.Add(this.Columns[i]);
                        strippedColumnIndexes.Add(i);
                        break;
                    }
                }

                if (i == this.ColumnCount) throw new ArgumentException(StringExtensions.Format("Column '{0}' to strip is not in DataBlock. Columns: {1}", columnName, String.Join(", ", this.Columns)));
            }

            // Create the stripped down block and copy the relevant columns to it
            DataBlock strippedBlock = new DataBlock(strippedColumns, this.RowCount);

            for (int newColumnIndex = 0; newColumnIndex < columns.Count; ++newColumnIndex)
            {
                // Find the column in this block
                int oldColumnIndex = strippedColumnIndexes[newColumnIndex];

                // Copy columns to the stripped block
                strippedBlock.SetColumn(newColumnIndex, this.GetColumn(oldColumnIndex));
            }

            return strippedBlock;
        }

        /// <summary>
        ///  Reset RowCount of DataBlock. RowCount may be reset as long as all column arrays are long enough.
        /// </summary>
        /// <param name="count">New RowCount to set</param>
        public void SetRowCount(int count)
        {
            for (int columnIndex = 0; columnIndex < this.ColumnCount; ++columnIndex)
            {
                Array column = (Array)_valuesByColumn[columnIndex];
                if (column.Length < count) throw new ArgumentException(StringExtensions.Format("Unable to set rowCount to {0:n0}; column '{1}' is only length {2:n0}", count, this.Columns[columnIndex].Name, column.Length));
            }

            this.RowCount = count;
        }

        /// <summary>
        /// Returns a read-only projection of this datablock
        /// </summary>
        /// <returns>this datablock wrapped as a readonly projection</returns>
        public ReadOnlyDataBlock AsReadOnly()
        {
            return new ReadOnlyDataBlock(this);
        }

        public static implicit operator ReadOnlyDataBlock(DataBlock b)
        {
            return b.AsReadOnly();
        }

        public struct ReadOnlyDataBlock
        {
            public ReadOnlyDataBlock(DataBlock block) : this(block, null, -1, -1)
            {
            }

            private ReadOnlyDataBlock(DataBlock block, int[] itemIndexes, int startingIndex, int length)
            {
                if (block == null)
                    throw new ArgumentNullException("block");

                if (itemIndexes != null)
                {
                    if (itemIndexes.Length != block.RowCount) throw new ArgumentException("itemIndexes");
                    if (startingIndex >= itemIndexes.Length) throw new IndexOutOfRangeException("startingIndex");
                    if (startingIndex + length > itemIndexes.Length) throw new IndexOutOfRangeException("length");
                }

                _block = block;
                _itemIndexes = itemIndexes;
                _startingIndex = startingIndex;

                if (itemIndexes == null)
                {
                    RowCount = block.RowCount;
                }
                else if (startingIndex == -1)
                {
                    RowCount = 0;
                }
                else
                {
                    RowCount = length;
                }
            }

            public IList<ColumnDetails> Columns { get { return _block.Columns; } }

            public int ColumnCount { get { return _block.ColumnCount; } }

            public int RowCount { get; private set; }

            public object this[int rowIndex, int columnIndex]
            {
                get
                {
                    int realRowIndex = GetRealIndex(rowIndex);
                    return _block[realRowIndex, columnIndex];
                }
            }

            public object GetValue(int rowIndex, int columnIndex)
            {
                int realRowIndex = GetRealIndex(rowIndex);
                return _block.GetValue(realRowIndex, columnIndex);
            }

            public int IndexOfColumn(string name)
            {
                return _block.IndexOfColumn(name);
            }

            public T GetValueT<T>(int rowIndex, int columnIndex)
            {
                T[] array = (T[])_block._valuesByColumn[columnIndex];
                int realRowIndex = GetRealIndex(rowIndex);
                return array[realRowIndex];
            }

            internal Type GetTypeForColumn(int columnIndex)
            {
                Array sourceData = _block.GetColumn(columnIndex);
                return sourceData.GetType().GetElementType();
            }

            internal Array GetColumn(int columnIndex)
            {
                if (_itemIndexes != null)
                    throw new NotSupportedException("Getting a column array of a projection is not supported");

                return _block.GetColumn(columnIndex);
            }

            internal ReadOnlyDataBlock ProjectChain(int[] itemIndexes, int startingIndex, int length)
            {
                return new ReadOnlyDataBlock(_block, itemIndexes, startingIndex, length);
            }

            private int GetRealIndex(int rowIndex)
            {
                if (_itemIndexes == null) { return rowIndex; }

                if (rowIndex < 0 || rowIndex > RowCount)
                    throw new ArgumentException("rowIndex");

                return _itemIndexes[_startingIndex + rowIndex];
            }

            private int[] _itemIndexes;
            private int _startingIndex;
            private DataBlock _block;
        }
    }
}

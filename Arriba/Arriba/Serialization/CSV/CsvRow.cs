// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Arriba.Structures;

namespace Arriba.Serialization.Csv
{
    /// <summary>
    /// Represents a single row within a CSV file. 
    /// </summary>
    public class CsvRow : IEnumerable<string>, IEnumerable<Value>
    {
        private readonly string _line;
        private readonly CsvCellRange[] _ranges;
        private readonly Dictionary<string, int> _lookup;
        private readonly CsvReaderSettings _settings;

        internal CsvRow(int rowNumber, string line, CsvCellRange[] ranges, Dictionary<string, int> headerLookup, CsvReaderSettings settings)
        {
            _lookup = headerLookup;
            _line = line;
            _ranges = ranges;
            this.Length = ranges.Length;
            this.RowNumber = rowNumber;
            _settings = settings;
        }

        /// <summary>
        /// Gets the count of cells in the row. 
        /// </summary>
        public int Length
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the row number of the row
        /// </summary>
        public int RowNumber { get; private set; }


        /// <summary>
        /// Gets the enumerable of columns within the row.
        /// </summary>
        public IEnumerable<string> StringColumns
        {
            get
            {
                foreach (var range in _ranges)
                {
                    yield return GetRawValueForRange(range).ToString();
                }
            }
        }

        /// <summary>
        /// Gets the enumerable of columns within the row.
        /// </summary>
        public IEnumerable<Value> ValueColumns
        {
            get
            {
                foreach (var range in _ranges)
                {
                    yield return Value.Create(GetRawValueForRange(range));
                }
            }
        }

        /// <summary>
        /// Gets the element at the specified index. 
        /// </summary>
        /// <param name="index">Index of the element.</param>
        /// <returns>Value at the specified index.</returns>
        public Value this[int index]
        {
            get
            {
                return Value.Create(this.GetRawValueForRange(_ranges[index]));
            }
        }

        /// <summary>
        /// Gets the element with the specified column name. 
        /// </summary>
        /// <param name="columnName">Column name.</param>
        /// <returns>Value for the specified column name.</returns>
        public Value this[string columnName]
        {
            get
            {
                return this[_lookup[columnName]];
            }
        }

        /// <summary>
        /// Gets the string value for the specified range. 
        /// </summary>
        /// <param name="range">Cell range.</param>
        /// <returns>String value for range.</returns>
        private object GetRawValueForRange(CsvCellRange range)
        {
            if (range.IsRowNumberVirtualCell)
            {
                return this.RowNumber;
            }
            else if (range.Length == 0)
            {
                return String.Empty;
            }

            // TODO: Hotspot 
            string cell = _line.Substring(range.Start, range.Length);

            if (range.ContainsQuotes)
            {
                return cell.Replace("\"\"", "\"");
            }

            return cell;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>Enumertor.</returns>
        public IEnumerator<string> GetEnumerator()
        {
            return this.StringColumns.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>Enumertor.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>Enumertor.</returns>
        IEnumerator<Value> IEnumerable<Value>.GetEnumerator()
        {
            return this.ValueColumns.GetEnumerator();
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Arriba.Serialization.Csv
{
    /// <summary>
    /// Settings for csv reading. 
    /// </summary>
    public class CsvReaderSettings
    {
        public CsvReaderSettings()
        {
            this.DisposeStream = true;
            this.ReadBufferSize = 1024 * 64;
            this.MaximumSingleCellLines = 256;
            this.Delimiter = ',';
            this.HasHeaders = true; // RFC denotes that headers should be included
            this.ColumnNameComparer = StringComparer.OrdinalIgnoreCase;
            this.RowNumberColumnName = "RowNumber";
            this.TrimColumnNames = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to include the row number as a column. 
        /// </summary>
        public bool IncludeRowNumberAsColumn { get; set; }

        /// <summary>
        /// Gets or sets the column name to use for the row number column if IncludeRowNumberAsColumn is true. 
        /// </summary>
        public string RowNumberColumnName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the CSV file has a header (columnName) row. 
        /// </summary>
        public bool HasHeaders { get; set; }

        /// <summary>
        /// Gets a sets a value indicating whether to dispose the underlying stream when the reader is disposed. 
        /// </summary>
        public bool DisposeStream { get; set; }

        /// <summary>
        /// Gets or a values a value indicating whether to trim left and right padded whitespace from column names.
        /// </summary>
        public bool TrimColumnNames { get; set; }

        /// <summary>
        /// Gets or sets the size of the buffer used when reading the stream. 
        /// </summary>
        public int ReadBufferSize { get; set; }

        /// <summary>
        /// Gets or sets the delimiter to use to split CSV columns. 
        /// </summary>
        /// <remarks>Defaults to ','.</remarks>
        public char Delimiter { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates the maximum number of lines in a single cell. 
        /// </summary>
        public int MaximumSingleCellLines { get; set; }

        /// <summary>
        /// Gets or sets the comparer used for column name lookups.
        /// </summary>
        /// <remarks>
        /// Defaults to ordinal ignore case. 
        /// </remarks>
        public IEqualityComparer<String> ColumnNameComparer { get; set; }
    }
}

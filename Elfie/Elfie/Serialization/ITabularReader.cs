using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    public interface ITabularReader : IDisposable
    {
        /// <summary>
        ///  Return the column headings found. The set is empty if there was no heading row.
        /// </summary>
        IReadOnlyList<string> Columns { get; }

        /// <summary>
        ///  Look up the column index of a given column by name.
        ///  Column names are case insensitive.
        ///  Will throw if the column name wasn't found.
        /// </summary>
        /// <param name="columnNameOrIndex">Column name for which to find column index, or already an integer index</param>
        /// <returns>Index of column in source. Throws if the column wasn't found.</returns>
        int ColumnIndex(string columnNameOrIndex);

        /// <summary>
        ///  Return the cells for the current row.
        ///  Get a single cell with reader.Current[columnIndex]
        ///  Converts values to 
        /// </summary>
        /// <returns>String8Set with the cells for the current row.</returns>
        String8Set Current { get; }

        /// <summary>
        ///  Returns the number of rows read so far.
        ///  If no newlines in rows, the RowCountRead is the line number of the current row.
        /// </summary>
        int RowCountRead { get; }

        /// <summary>
        ///  Returns the number of columns in the current row. Used to handle
        ///  rows which are shorter than expected, if desired.
        /// </summary>
        int CurrentRowColumns { get; }

        /// <summary>
        ///  Move the reader to the next row. This must be called before
        ///  reading the first row.
        /// </summary>
        /// <returns>True if another row exists, False if the source is out of content</returns>
        bool NextRow();
    }
}

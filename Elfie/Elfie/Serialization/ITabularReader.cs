// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  ITabularReader implements reading tabular data from arbitrary sources.
    ///  TabularReader returns values as ITabularValue, which can be converted to multiple types without allocation, conversion, or boxing.
    ///  Elfie readers natively return string values as String8s, which don't require per value allocation.
    ///  
    ///  NOTE: Values read from ITabularReader must be copied before NextRow is called.
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
    public interface ITabularReader : IDisposable
    {
        /// <summary>
        ///  Return the column headings found. The set is empty if there was no heading row.
        /// </summary>
        IReadOnlyList<string> Columns { get; }

        /// <summary>
        ///  Look up the column index of a given column by name.
        ///  Column names are case insensitive.
        ///  Will set to -1 and return false if not found.
        /// </summary>
        /// <param name="columnNameOrIndex">Column name for which to find column index, or already an integer index</param>
        /// <param name="columnIndex">Column Index of column if found; -1 otherwise</param>
        /// <returns>True if column found; False otherwise</returns>
        bool TryGetColumnIndex(string columnNameOrIndex, out int columnIndex);

        /// <summary>
        ///  Return a cell for the current row.
        ///  IConvertible has methods to convert values to String8, string, int, bool, DateTime, etc.
        /// </summary>
        /// <param name="index">Zero-based column index</param>
        /// <returns>ITabularValue for the desired column in the current row</returns>
        ITabularValue Current(int index);

        /// <summary>
        ///  Returns the number of rows read so far.
        ///  If no newlines in rows, the RowCountRead is the line number of the current row.
        /// </summary>
        int RowCountRead { get; }

        /// <summary>
        ///  Return how many bytes were read so far, if the implementation knows.
        /// </summary>
        long BytesRead { get; }

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

    public static class ITabularReaderExtensions
    {
        /// <summary>
        ///  Return a cell for the current row or String.Empty if the row doesn't have
        ///  enough columns.
        /// </summary>
        /// <param name="reader">ITabularReader</param>
        /// <param name="index">Zero-based column index</param>
        /// <returns>ITabularValue for column</returns>
        public static ITabularValue CurrentOrEmpty(this ITabularReader reader, int index)
        {
            if (reader.CurrentRowColumns > index) return reader.Current(index);
            return String8TabularValue.Empty;
        }

        /// <summary>
        ///  Look up the column index of a given column by name.
        ///  Will only work if the file had a header row.
        ///  Column names are case insensitive.
        ///  Will throw if the column name wasn't found.
        /// </summary>
        /// <param name="columnNameOrIndex">Column name for which to find column index, or already an integer index</param>
        /// <returns>Index of column in TSV. Throws if column isn't found or no header row was read.</returns>
        public static int ColumnIndex(this ITabularReader reader, string columnNameOrIndex)
        {
            int columnIndex;
            if (reader.TryGetColumnIndex(columnNameOrIndex, out columnIndex)) return columnIndex;

            throw new ColumnNotFoundException(String.Format("Column Name \"{0}\" not found in file.\nKnown Columns: \"{1}\"", columnNameOrIndex, String.Join(", ", reader.Columns)));
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    public interface ITabularWriter : IDisposable
    {
        /// <summary>
        ///  Identify the columns to be written.
        ///  Must be called before anything else.
        /// </summary>
        /// <param name="columnNames">Set of column names each row will write.</param>
        void SetColumns(IEnumerable<string> columnNames);

        /// <summary>
        ///  Write a value to the current row.
        /// </summary>
        /// <param name="value">String8 to write to the current row.</param>
        void Write(String8 value);

        /// <summary>
        ///  Write a UTC DateTime to the current row.
        ///  The value is converted without allocations.
        /// </summary>
        /// <param name="value">Value to write</param>
        void Write(DateTime value);

        /// <summary>
        ///  Write a long to the current row.
        ///  The value is converted without allocations.
        /// </summary>
        /// <param name="value">Value to write</param>
        void Write(long value);

        /// <summary>
        ///  Write a double to the current row.
        ///  The value is converted without allocations.
        /// </summary>
        /// <param name="value">Value to write</param>
        void Write(double value);

        /// <summary>
        ///  Write a boolean to the current row.
        ///  The value is converted without allocations.
        /// </summary>
        /// <param name="value">Value to write</param>
        void Write(bool value);

        /// <summary>
        ///  Write a single UTF8 character to the current row.
        ///  The value is converted without allocations.
        /// </summary>
        /// <param name="value">Value to write</param>
        void Write(byte value);

        /// <summary>
        ///  Write the beginning of a cell value which will be written in parts.
        ///  Used for concatenated values.
        /// </summary>
        void WriteValueStart();

        /// <summary>
        ///  Write the end of a cell value which was written in parts.
        ///  Used for concatenated values.
        /// </summary>
        void WriteValueEnd();

        /// <summary>
        ///  Write a value as part of the current cell value.
        /// </summary>
        /// <param name="part">String8 to write to the current cell.</param>
        void WriteValuePart(String8 part);

        /// <summary>
        ///  Write a UTC DateTime as part of a single cell value.
        ///  Callers must call WriteValueStart and WriteValueEnd around WriteValuePart calls.
        /// </summary>
        /// <param name="value">Value to write</param>
        void WriteValuePart(DateTime value);

        /// <summary>
        ///  Write an integer as part of a single cell value.
        ///  Callers must call WriteValueStart and WriteValueEnd around WriteValuePart calls.
        /// </summary>
        /// <param name="part">Value to write</param>
        void WriteValuePart(int part);

        /// <summary>
        ///  Write a boolean as part of a single cell value.
        ///  Callers must call WriteValueStart and WriteValueEnd around WriteValuePart calls.
        /// </summary>
        /// <param name="part">Value to write</param>
        void WriteValuePart(bool part);

        /// <summary>
        ///  Write a single UTF8 byte as part of the current cell value.
        /// </summary>
        /// <param name="c">Character to write to the current cell.</param>
        void WriteValuePart(byte c);

        /// <summary>
        ///  Write a row separator and start the next row.
        ///  NextRow must be called after the row values are written.
        ///  NextRow validates that the correct number of values were written.
        /// </summary>
        void NextRow();

        /// <summary>
        ///  Return the number of rows written so far.
        ///  In rows without newlines, one less than the line number of the current row.
        /// </summary>
        int RowCountWritten { get; }

        /// <summary>
        ///  Return how many bytes were written out so far, if the implementation knows.
        /// </summary>
        long BytesWritten { get; }
    }
}

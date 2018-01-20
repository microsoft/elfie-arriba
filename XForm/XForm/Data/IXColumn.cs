// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace XForm.Data
{
    /// <summary>
    ///  IXColumn represents a logical column in an IXTable.
    ///  It could wrap a constant, normal column, enumeration (a set of values and indices), or function result.
    /// </summary>
    public interface IXColumn
    {
        /// <summary>
        ///  ColumnDetails for this column.
        /// </summary>
        ColumnDetails ColumnDetails { get; }

        /// <summary>
        ///  ArraySelector for the current enumeration position of the column.
        /// </summary>
        ArraySelector CurrentSelector { get; }

        /// <summary>
        ///  Call to subscribe to the column and get a function which returns an XArray for the
        ///  values of this column for the rows currently enumerated to.
        /// </summary>
        /// <returns>Func&lt;XArray&gt; which retrieves the column values for the current rows when called.</returns>
        Func<XArray> CurrentGetter();

        /// <summary>
        ///  Call to subscribe to the column and get a function which can seek to specific rows.
        ///  Returns null if the column doesn't support seeking.
        /// </summary>
        /// <returns>>Func&lt;ArraySelector, XArray&gt; to get specific row values, null if seek not supported</returns>
        Func<ArraySelector, XArray> SeekGetter();

        /// <summary>
        ///  Call to subscribe to the column and get a function to get all distinct values for this column
        ///  if it has a constrained set of values (Constant or Enumeration).
        ///  Returns null if the column doesn't have a restricted set of values.
        ///  Returns a SingleValue XArray for constants.
        /// </summary>
        /// <returns>Func&lt;XArray&gt; to get distinct values, or null if values aren't constrained.</returns>
        Func<XArray> ValuesGetter();

        /// <summary>
        ///  Call to subscribe to the column and get a function to get an XArray of the value index for
        ///  each row currently enumerated to. 
        ///  Returns null if the column doesn't have a restricted set of values.
        /// </summary>
        /// <returns>Func&lt;XArray&gt; to get current row value indices, or null if values aren't constrained.</returns>
        Func<ArraySelector, XArray> IndicesGetter();
    }
}

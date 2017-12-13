// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace XForm.Data
{
    /// <summary>
    ///  IDataBatchEnumerator is the interface type all XForm Pipeline stages must implement.
    ///  It represents tabular data enumerable in batches of rows.
    /// </summary>
    public interface IDataBatchEnumerator : IDisposable
    {
        /// <summary>
        ///  Get the columns available from this source.
        /// </summary>
        IReadOnlyList<ColumnDetails> Columns { get; }

        /// <summary>
        ///  Go back to the first rows from this source again.
        /// </summary>
        void Reset();

        /// <summary>
        ///  Request the next batch of rows from the source of up to desiredCount rows.
        /// </summary>
        /// <param name="desiredCount">Desired Row count; fewer or more rows may be returned.</param>
        /// <returns>Row count returned, zero if no more rows available</returns>
        int Next(int desiredCount);

        /// <summary>
        ///  Return the row count in the current batch.
        /// </summary>
        int CurrentBatchRowCount { get; }

        /// <summary>
        ///  Return a function which returns the DataBatch for the desired column
        ///  for the current batch of rows.
        /// </summary>
        /// <param name="columnIndex">Index of column to provide a getter for</param>
        /// <returns>Function which will return the DataBatch for columnName for the current row batch on each call</returns>
        Func<DataBatch> ColumnGetter(int columnIndex);
    }

    /// <summary>
    ///  IDataBatchList is a seekable extension to IDataBatchEnumerator, exposed by
    ///  sources and pipelines which are able to seek to specific rows in the set
    ///  efficiently.
    /// </summary>
    public interface IDataBatchList : IDataBatchEnumerator
    {
        /// <summary>
        ///  Get the total row count of this list.
        /// </summary>
        int Count { get; }

        /// <summary>
        ///  Get a specific set of rows.
        /// </summary>
        /// <param name="selector">ArraySelector indicating which specific rows to get</param>
        void Get(ArraySelector selector);
    }
}

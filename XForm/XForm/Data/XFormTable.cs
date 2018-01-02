// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using XForm.Extensions;
using XForm.IO;
using XForm.IO.StreamProvider;

namespace XForm.Data
{
    /// <summary>
    ///  XFormTable provides static helper functions to expose XForm in a friendly way
    ///  in code.
    /// </summary>
    public static class XFormTable
    {
        /// <summary>
        ///  Build an XForm Table wrapping in-memory arrays with all rows available.
        /// </summary>
        /// <example>
        ///  int[] id = Enumerable.Range(0, 1024).ToArray();
        ///  int[] score = ...
        ///  
        ///  WorkflowContext context = new WorkflowContext();
        ///  XFormTable.FromArrays(1024)
        ///     .WithColumn("ID", id)
        ///     .WithColumn("Score", score)
        ///     .Query("where [Score] > 90", context)
        ///     .Count();
        /// </example>
        /// <param name="totalCount"></param>
        /// <returns></returns>
        public static ArrayTable FromArrays(int totalCount)
        {
            return new ArrayTable(totalCount);
        }

        /// <summary>
        ///  Read a binary format table from disk and return it.
        /// </summary>
        /// <param name="tableName">Table Name to load</param>
        /// <param name="context">WorkflowContext with where to load from, as-of-date of version to load, and other context</param>
        /// <returns>IDataBatchEnumerator of table</returns>
        public static IDataBatchEnumerator Load(string tableName, WorkflowContext context)
        {
            return new BinaryTableReader(context.StreamProvider, context.StreamProvider.LatestBeforeCutoff(LocationType.Table, tableName, CrawlType.Full, context.RequestedAsOfDateTime).Path);
        }
    }
}

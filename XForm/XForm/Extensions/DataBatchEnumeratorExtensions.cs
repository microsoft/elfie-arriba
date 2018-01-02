// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using XForm.Data;
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Query;

namespace XForm.Extensions
{
    public struct RunResult
    {
        public long RowCount { get; set; }
        public bool IsComplete { get; set; }
        public TimeSpan Timeout { get; set; }
        public TimeSpan Elapsed { get; set; }
    }

    public static class DataBatchEnumeratorExtensions
    {
        public const int DefaultBatchSize = 10240;

        #region Query
        /// <summary>
        ///  Run an XQL Query on this source and return the query result.
        /// </summary>
        /// <param name="source">IDataBatchEnumerator to query</param>
        /// <param name="xqlQuery">XQL query to run</param>
        /// <param name="context">WorkflowContext for loading location, as-of-date, and other context</param>
        /// <returns>IDataBatchEnumerator of result</returns>
        public static IDataBatchEnumerator Query(this IDataBatchEnumerator source, string xqlQuery, WorkflowContext context)
        {
            return XqlParser.Parse(xqlQuery, source, context);
        }

        #endregion

        #region Run
        /// <summary>
        ///  Run a Query, dispose the source, and return the count of rows from the query.
        /// </summary>
        /// <param name="pipeline">IDataBatchEnumerator of source or query to run</param>
        /// <param name="batchSize">Number of rows to process on each iteration</param>
        /// <returns>Count of rows in this source or query.</returns>
        public static long RunAndDispose(this IDataBatchEnumerator pipeline, int batchSize = DefaultBatchSize)
        {
            using (pipeline)
            {
                return RunWithoutDispose(pipeline, batchSize);
            }
        }

        /// <summary>
        ///  Run a Query but don't dispose the source, and return the count of rows from the query.
        /// </summary>
        /// <param name="pipeline">IDataBatchEnumerator of source or query to run</param>
        /// <param name="batchSize">Number of rows to process on each iteration</param>
        /// <returns>Count of rows in this source or query.</returns>
        public static long RunWithoutDispose(this IDataBatchEnumerator pipeline, int batchSize = DefaultBatchSize)
        {
            long rowsWritten = 0;
            while (true)
            {
                int batchCount = pipeline.Next(batchSize);
                if (batchCount == 0) break;
                rowsWritten += batchCount;
            }
            return rowsWritten;
        }

        /// <summary>
        ///  Run the query for up to a provided timeout, and return the row count found, whether the query finished,
        ///  and the runtime if it completed.
        ///  
        ///  NOTE: Does not dispose the query; caller must do so.
        /// </summary>
        /// <param name="pipeline">IDataBatchEnumerator of source or query to run</param>
        /// <param name="timeout">Time limit for runtime</param>
        /// <param name="batchSize">Number of rows to process in each iteration</param>
        /// <returns>RunResult with whether query completed, runtime so far, and row count so far</returns>
        public static RunResult RunUntilTimeout(this IDataBatchEnumerator pipeline, TimeSpan timeout, int batchSize = DefaultBatchSize)
        {
            RunResult result = new RunResult();
            result.Timeout = timeout;

            Stopwatch w = Stopwatch.StartNew();
            while (true)
            {
                int batch = pipeline.Next(batchSize);
                result.RowCount += batch;

                if (batch == 0)
                {
                    result.IsComplete = true;
                    break;
                }

                if (w.Elapsed > timeout) break;
            }

            result.Elapsed = w.Elapsed;
            return result;
        }
        #endregion

        #region Get Simple Results
        /// <summary>
        ///  Get the count of rows from the source.
        /// </summary>
        /// <param name="pipeline">IDataBatchEnumerator to count</param>
        /// <param name="batchSize">Number of rows to process on each iteration</param>
        /// <returns>Count of rows in this source or query.</returns>
        public static long Count(this IDataBatchEnumerator pipeline, int batchSize = DefaultBatchSize)
        {
            return RunAndDispose(pipeline, batchSize);
        }

        /// <summary>
        ///  Get a single value from the source (the first column in the first row).
        /// </summary>
        /// <typeparam name="T">Data Type of result</typeparam>
        /// <param name="pipeline">IDatBatchEnumerator to run</param>
        /// <returns>Single value result (first column, first row)</returns>
        public static T Single<T>(this IDataBatchEnumerator pipeline)
        {
            Func<DataBatch> getter = pipeline.ColumnGetter(0);
            using (pipeline)
            {
                pipeline.Next(1);

                DataBatch batch = getter();
                T[] array = (T[])(getter().Array);
                return array[batch.Index(0)];
            }
        }

        /// <summary>
        ///  Get pages of Lists of values from a single column from the source.
        /// </summary>
        /// <example>
        ///  foreach(List&lt;int&gt; in XFormTable.FromArrays(10000).With("Values", array).ToList("Values", 1024))
        ///  {
        ///     ...
        ///  }
        /// </example>
        /// <typeparam name="T">Type of values in column</typeparam>
        /// <param name="pipeline">IDataBatchEnumerator to run</param>
        /// <param name="columnName">Column Name to retrieve values from</param>
        /// <param name="batchSize">Maximum row count to retrieve per page</param>
        /// <returns>Pages of the result column in a List</returns>
        public static IEnumerable<List<T>> ToList<T>(this IDataBatchEnumerator pipeline, string columnName, int batchSize = DefaultBatchSize)
        {
            List<T> result = new List<T>(batchSize);

            using (pipeline)
            {
                Func<DataBatch> getter = pipeline.ColumnGetter(pipeline.Columns.IndexOfColumn(columnName));

                while (pipeline.Next(batchSize) != 0)
                {
                    DataBatch batch = getter();
                    T[] array = (T[])batch.Array;
                    for (int i = 0; i < batch.Count; ++i)
                    {
                        result.Add(array[batch.Index(i)]);
                    }

                    yield return result;
                    result.Clear();
                }
            }
        }
        #endregion

        #region Save
        /// <summary>
        ///  Save the source or query as a binary format table with the given name.
        /// </summary>
        /// <param name="source">IDataBatchEnumerator to save</param>
        /// <param name="tableName">Table Name to save table as</param>
        /// <param name="context">WorkflowContext for location to save to, as-of-date of result, and other context</param>
        /// <param name="batchSize">Number of rows to process in each iteration</param>
        /// <returns>Row Count Written</returns>
        public static long Save(this IDataBatchEnumerator source, string tableName, WorkflowContext context, int batchSize = DefaultBatchSize)
        {
            string tableRootPath = context.StreamProvider.Path(LocationType.Table, tableName, CrawlType.Full, context.RequestedAsOfDateTime);
            return new BinaryTableWriter(source, context, tableRootPath).RunAndDispose();
        }
        #endregion
    }
}

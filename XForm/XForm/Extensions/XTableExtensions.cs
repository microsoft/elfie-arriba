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

    public static class XTableExtensions
    {
        public const int DefaultBatchSize = 10240;

        #region Query
        /// <summary>
        ///  Run an XQL Query on this source and return the query result.
        /// </summary>
        /// <param name="source">IXTable to query</param>
        /// <param name="xqlQuery">XQL query to run</param>
        /// <param name="context">XDatabaseContext for loading location, as-of-date, and other context</param>
        /// <returns>IXTable of result</returns>
        public static IXTable Query(this IXTable source, string xqlQuery, XDatabaseContext context)
        {
            return XqlParser.Parse(xqlQuery, source, context);
        }

        #endregion

        #region Run
        /// <summary>
        ///  Run a Query, dispose the source, and return the count of rows from the query.
        /// </summary>
        /// <param name="pipeline">IXTable of source or query to run</param>
        /// <param name="xarraySize">Number of rows to process on each iteration</param>
        /// <returns>Count of rows in this source or query.</returns>
        public static long RunAndDispose(this IXTable pipeline, int xarraySize = DefaultBatchSize)
        {
            using (pipeline)
            {
                return RunWithoutDispose(pipeline, xarraySize);
            }
        }

        /// <summary>
        ///  Run a Query but don't dispose the source, and return the count of rows from the query.
        /// </summary>
        /// <param name="pipeline">IXTable of source or query to run</param>
        /// <param name="xarraySize">Number of rows to process on each iteration</param>
        /// <returns>Count of rows in this source or query.</returns>
        public static long RunWithoutDispose(this IXTable pipeline, int xarraySize = DefaultBatchSize)
        {
            long rowsWritten = 0;
            while (true)
            {
                int batchCount = pipeline.Next(xarraySize);
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
        /// <param name="pipeline">IXTable of source or query to run</param>
        /// <param name="timeout">Time limit for runtime</param>
        /// <param name="xarraySize">Number of rows to process in each iteration</param>
        /// <returns>RunResult with whether query completed, runtime so far, and row count so far</returns>
        public static RunResult RunUntilTimeout(this IXTable pipeline, TimeSpan timeout, int xarraySize = DefaultBatchSize)
        {
            RunResult result = new RunResult();
            result.Timeout = timeout;

            Stopwatch w = Stopwatch.StartNew();
            while (true)
            {
                int xarray = pipeline.Next(xarraySize);
                result.RowCount += xarray;

                if (xarray == 0)
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
        /// <param name="pipeline">IXTable to count</param>
        /// <param name="xarraySize">Number of rows to process on each iteration</param>
        /// <returns>Count of rows in this source or query.</returns>
        public static long Count(this IXTable pipeline, int xarraySize = DefaultBatchSize)
        {
            return RunAndDispose(pipeline, xarraySize);
        }

        /// <summary>
        ///  Get a single value from the source (the first column in the first row).
        /// </summary>
        /// <typeparam name="T">Data Type of result</typeparam>
        /// <param name="pipeline">IDatxarrayEnumerator to run</param>
        /// <returns>Single value result (first column, first row)</returns>
        public static T Single<T>(this IXTable pipeline)
        {
            Func<XArray> getter = pipeline.Columns[0].CurrentGetter();
            using (pipeline)
            {
                pipeline.Next(1);

                XArray xarray = getter();
                T[] array = (T[])(getter().Array);
                return array[xarray.Index(0)];
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
        /// <param name="pipeline">IXTable to run</param>
        /// <param name="columnName">Column Name to retrieve values from</param>
        /// <param name="xarraySize">Maximum row count to retrieve per page</param>
        /// <returns>Pages of the result column in a List</returns>
        public static IEnumerable<List<T>> ToList<T>(this IXTable pipeline, string columnName, int xarraySize = DefaultBatchSize)
        {
            List<T> result = new List<T>(xarraySize);

            using (pipeline)
            {
                Func<XArray> getter = pipeline.Columns.Find(columnName).CurrentGetter();

                while (pipeline.Next(xarraySize) != 0)
                {
                    XArray xarray = getter();
                    T[] array = (T[])xarray.Array;
                    for (int i = 0; i < xarray.Count; ++i)
                    {
                        result.Add(array[xarray.Index(i)]);
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
        /// <param name="source">IXTable to save</param>
        /// <param name="tableName">Table Name to save table as</param>
        /// <param name="context">XDatabaseContext for location to save to, as-of-date of result, and other context</param>
        /// <param name="xarraySize">Number of rows to process in each iteration</param>
        /// <returns>Row Count Written</returns>
        public static long Save(this IXTable source, string tableName, XDatabaseContext context, int xarraySize = DefaultBatchSize)
        {
            string tableRootPath = context.StreamProvider.Path(LocationType.Table, tableName, CrawlType.Full, context.RequestedAsOfDateTime);
            return new BinaryTableWriter(source, context, tableRootPath).RunAndDispose();
        }
        #endregion
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

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
        /// <summary>
        ///  Number of rows to read on each Next() call, if not overridden by caller
        /// </summary>
        /// <remarks>
        ///  For normal scale tables, performance improved up to 10,240 rows and then flattened out.
        ///  For huge tables, disk I/O seems better for larger batch sizes.
        ///  The batch size determines the memory use, though usually individual rows aren't especially large.
        /// </remarks>
        public const int DefaultBatchSize = 20480; // 10240;

        #region Next Overloads
        /// <summary>
        ///  Get the next batch of rows from the table.
        /// </summary>
        /// <param name="table">IXTable to enumerate</param>
        /// <returns>Count of rows returned; zero means no more rows</returns>
        public static int Next(this IXTable table)
        {
            return table.Next(DefaultBatchSize, default(CancellationToken));
        }

        /// <summary>
        ///  Get a specific desired count of rows from the table.
        /// </summary>
        /// <param name="table">IXTable to enumerate</param>
        /// <param name="desiredCount">Maximum number of rows to return</param>
        /// <returns>Count of rows returned; zero means no more rows</returns>
        public static int Next(this IXTable table, int desiredCount)
        {
            return table.Next(desiredCount, default(CancellationToken));
        }
        #endregion

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
        /// <param name="cancellationToken">Token to allow early cancellation</param>
        /// <param name="batchSize">Number of rows to process on each iteration</param>
        /// <returns>Count of rows in this source or query.</returns>
        public static RunResult RunAndDispose(this IXTable pipeline, CancellationToken cancellationToken = default(CancellationToken), int batchSize = DefaultBatchSize)
        {
            using (pipeline)
            {
                return RunWithoutDispose(pipeline, cancellationToken, batchSize);
            }
        }

        /// <summary>
        ///  Run a Query but don't dispose the source, and return the count of rows from the query.
        /// </summary>
        /// <param name="pipeline">IXTable of source or query to run</param>
        /// <param name="cancellationToken">Token to allow early cancellation</param>
        /// <param name="batchSize">Number of rows to process on each iteration</param>
        /// <returns>Count of rows in this source or query.</returns>
        public static RunResult RunWithoutDispose(this IXTable pipeline, CancellationToken cancellationToken = default(CancellationToken), int batchSize = DefaultBatchSize)
        {
            RunResult result = new RunResult();
            Stopwatch w = Stopwatch.StartNew();

            while (true)
            {
                int batchCount = pipeline.Next(batchSize, cancellationToken);
                result.RowCount += batchCount;

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                else if (batchCount == 0)
                {
                    result.IsComplete = true;
                    break;
                }
                
            }

            result.Elapsed = w.Elapsed;
            return result;
        }

        /// <summary>
        ///  Run the query for up to a provided timeout, and return the row count found, whether the query finished,
        ///  and the runtime if it completed.
        ///  
        ///  NOTE: Does not dispose the query; caller must do so.
        /// </summary>
        /// <param name="pipeline">IXTable of source or query to run</param>
        /// <param name="timeout">Time limit for runtime</param>
        /// <param name="batchSize">Number of rows to process in each iteration</param>
        /// <returns>RunResult with whether query completed, runtime so far, and row count so far</returns>
        public static RunResult RunUntilTimeout(this IXTable pipeline, TimeSpan timeout, int batchSize = DefaultBatchSize)
        {
            using (CancellationTokenSource source = new CancellationTokenSource())
            {
                // Setup the timeout
                CancellationToken cancellationToken = source.Token;
                if (timeout != TimeSpan.Zero && timeout != TimeSpan.MaxValue) source.CancelAfter(timeout);

                // Run until timeout
                RunResult result = RunWithoutDispose(pipeline, cancellationToken, batchSize);

                // Tag the timeout on the result and return it
                result.Timeout = timeout;
                return result;
            }
        }
        #endregion

        #region Get Simple Results
        /// <summary>
        ///  Get the count of rows from the source.
        /// </summary>
        /// <param name="pipeline">IXTable to count</param>
        /// <param name="batchSize">Number of rows to process on each iteration</param>
        /// <returns>Count of rows in this source or query.</returns>
        public static long Count(this IXTable pipeline, CancellationToken cancellationToken = default(CancellationToken), int batchSize = DefaultBatchSize)
        {
            return RunAndDispose(pipeline, cancellationToken, batchSize).RowCount;
        }

        /// <summary>
        ///  Get a single value from the source (the first column in the first row).
        /// </summary>
        /// <typeparam name="T">Data Type of result</typeparam>
        /// <param name="pipeline">IDatxarrayEnumerator to run</param>
        /// <returns>Single value result (first column, first row)</returns>
        public static T Single<T>(this IXTable pipeline, CancellationToken cancellationToken = default(CancellationToken))
        {
            Func<XArray> getter = pipeline.Columns[0].CurrentGetter();
            using (pipeline)
            {
                pipeline.Next(1, cancellationToken);

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
        /// <param name="batchSize">Maximum row count to retrieve per page</param>
        /// <returns>Pages of the result column in a List</returns>
        public static IEnumerable<List<T>> ToList<T>(this IXTable pipeline, string columnName, CancellationToken cancellationToken = default(CancellationToken), int batchSize = DefaultBatchSize)
        {
            List<T> result = new List<T>(batchSize);

            using (pipeline)
            {
                Func<XArray> getter = pipeline.Columns.Find(columnName).CurrentGetter();

                while (pipeline.Next(batchSize, cancellationToken) != 0)
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
            return BinaryTableWriter.Build(source, context, tableRootPath).RunAndDispose().RowCount;
        }
        #endregion

        #region ConcatenatedTable handling
        /// <summary>
        ///  WrapParallel builds a parallel copy of the query stage for each source in ConcatenatingTable sources.
        ///  It is used to allow running optimized query stages and running in parallel when multiple tables are involved.
        /// </summary>
        /// <remarks>
        ///  WrapParallel can only be used by verbs where the output when run on the concatenated inputs rows from many sources
        ///  produces the same output as running in parallel on each source and then concatenating the result rows.
        /// </remarks>
        /// <param name="source">IXTable to wrap</param>
        /// <param name="builder">Wrapping function</param>
        /// <returns>Wrapped IXTable</returns>
        public static IXTable WrapParallel(this IXTable source, XqlParser parser, Func<IXTable, IXTable> builder)
        {
            ConcatenatedTable cSource = source as ConcatenatedTable;
            if(cSource != null)
            {
                Position currentPosition = parser.CurrentPosition;
                return ConcatenatedTable.Build(cSource.Sources.Select((s) =>
                {
                    parser.RewindTo(currentPosition);
                    return builder(s);
                }));
            }

            return builder(source);
        }

        #endregion
    }
}

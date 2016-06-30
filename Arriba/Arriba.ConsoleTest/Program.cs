// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Arriba.Diagnostics;
using Arriba.Extensions;
using Arriba.Model;
using Arriba.Model.Aggregations;
using Arriba.Model.Query;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.ConsoleTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //SetCountPerformance();
            FromAndPerformance();
            //Table table = LoadTable("Sample");

            //AggregateDistinctTest(table);
            //DistinctTest(table, "Resolution", "Pri = 3");
            //SearchTest(table, "Priority = 3", false, null);
            //SearchTest(table, "editor Pri = 3", true, new string[] { "ID", "Title", "Resolution" });
            //QueryPerformanceTest(table, "Priority = 1 AND Platform");
        }

        private static void FromAndPerformance()
        {
            ShortSet s0 = new ShortSet(ushort.MaxValue);
            ShortSet s1 = new ShortSet(ushort.MaxValue);
            s1.Not();
            ShortSet s2 = Arriba.Test.ShortSetTests.BuildRandom(ushort.MaxValue, 10000, new Random());

            s2.Count();

            int iterations = 3000000;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                s0.FromAnd(s1, s2);
            }
            w.Stop();

            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = iterations / milliseconds;
            Trace.Write(String.Format("{0:n0}\r\n", s0.Count()));
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.\r\n", iterations, milliseconds, operationsPerMillisecond));
        }

        private static void SetCountPerformance()
        {
            ShortSet s0 = new ShortSet(ushort.MaxValue);
            ShortSet s1 = new ShortSet(ushort.MaxValue);
            s1.Not();
            ShortSet s2 = Arriba.Test.ShortSetTests.BuildRandom(ushort.MaxValue, 10000, new Random());

            ushort value = 0;
            ushort value2 = 0;
            ushort value3 = 0;

            int iterations = 1000000;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                value = s0.Count();
                value2 = s1.Count();
                value3 = s2.Count();

                //value = (ushort)ShortSet.CallOverheadTest();
                //value2 = (ushort)ShortSet.CallOverheadTest();
                //value3 = (ushort)ShortSet.CallOverheadTest();
            }
            w.Stop();

            double milliseconds = w.ElapsedMilliseconds;
            double operationsPerMillisecond = iterations / milliseconds;
            Trace.Write(String.Format("{0:n0}, {1:n0}, {2:n0}\r\n", value, value2, value3));
            Trace.Write(String.Format("{0:n0} operations in {1:n0} milliseconds; {2:n0} per millisecond.\r\n", iterations, milliseconds, operationsPerMillisecond));
        }

        private static Table LoadTable(string tableName)
        {
            Table table = new Table();

            Trace.Write("Loading Table '{0}'...\r\n", tableName);

            Trace.Write(String.Format("\tDisk Size: {0}\r\n", BinarySerializable.Size(String.Format(@"Tables\{0}", tableName)).SizeString()));
            Trace.Write(String.Format("\tMemory Use: {0}\r\n", Memory.MeasureObjectSize(() => { table.Load(tableName); return table; }).SizeString()));


            return table;
        }

        private static void AggregateDistinctTest(Table table)
        {
            AggregationResult result = null;

            Trace.Write("Aggregating Bugs...");
            {
                for (int i = 0; i < 50; ++i)
                {
                    PivotQuery query = new PivotQuery();
                    query.AggregationColumns = new string[] { "CID" };
                    query.Aggregator = new CountAggregator();
                    query.Where = SelectQuery.ParseWhere("CD > 1/1/2013");
                    query.Dimensions.Add(new DateHistogramPivotDimension("Created Date") { Interval = DateHistogramInterval.Month });
                    query.Dimensions.Add(new DistinctValuePivotDimension("Area Level 2"));
                    query.Dimensions.Add(new DistinctValuePivotDimension("Area Level 3"));

                    //AggregationQuery query = new AggregationQuery();
                    //query.AggregationColumns = new string[] { "CID" };
                    //query.Aggregator = new CountAggregator();
                    //query.Where = SelectQuery.ParseWhere("CD > 1/1/2013");
                    //query.Dimensions.Add(new AggregationDimension("Priority", "P0", "P1", "P2", "P3"));
                    //query.Dimensions.Add(new AggregationDimension("Area Path", "p:Platform", "p:Fundamentals"));

                    //table.RunParallel = false;
                    result = table.Query(query);
                }
            }

            Console.WriteLine();
            //ShowResult(result.Values);
        }

        private static void DistinctTest(Table table, string column, string where)
        {
            int iterations = 1000;

            DistinctQuery query = new DistinctQuery(column, where, 100);
            DistinctResult result = null;

            Trace.Write("Aggregating Bugs...");

            for (int i = 0; i < iterations; ++i)
            {
                result = table.Query(query);
            }

            ShowResult(result.Values);
        }

        private static void SearchTest(Table table, string queryString, bool highlight = true, string[] columns = null)
        {
            int iterations = 100;
            int[] pageSizes = new int[] { 50, 100, 200, 400 };

            SelectResult result = null;

            foreach (int pageSize in pageSizes)
            {
                SelectQuery query = new SelectQuery();
                query.Columns = columns ?? new string[] { "*" };
                query.TableName = "Bugs";
                query.Where = SelectQuery.ParseWhere(queryString);
                query.Count = (ushort)pageSize;
                if (highlight) query.Highlighter = new Highlighter("[", "]");

                Console.WriteLine("\r\n{0}\r\n", query);

                Trace.Write(String.Format("Querying Bugs ({0})...\r\n", pageSize));

                for (int i = 0; i < iterations; ++i)
                {
                    result = table.Select(query);
                }
            }

            Console.WriteLine("Found {0:n0} items in {1}", result.Total, result.Runtime.ToFriendlyString());
            Console.WriteLine();
            //ShowResult(result.Values);
        }

        private static void AggregateTest(Table table)
        {
            int iterations = 1000;

            AggregationQuery query = new AggregationQuery();
            query.Aggregator = new Model.Aggregations.SumAggregator();
            query.AggregationColumns = new string[] { "ID" };
            query.TableName = "Bugs";
            query.Where = SelectQuery.ParseWhere("\"Created Date\" < \"2013-01-01\"");

            query.Dimensions.Add(new AggregationDimension("Priority", "Priority < 0", "Priority = 0", "Priority = 1", "Priority > 1"));
            query.Dimensions.Add(new AggregationDimension("Framework", "Maui", "Apex", "Tao"));
            //query.Dimensions.Add(new AggregationDimension("Created Month",
            //    "\"Created Date\" >= \"2011-01-01\" AND \"Created Date\" < \"2011-02-01\"",
            //    "\"Created Date\" >= \"2011-02-01\" AND \"Created Date\" < \"2011-03-01\"",
            //    "\"Created Date\" >= \"2011-03-01\" AND \"Created Date\" < \"2011-04-01\""
            //));

            AggregationResult result = null;
            Console.WriteLine("\r\n{0}\r\n", query);

            Trace.Write("Aggregating Bugs...");

            for (int i = 0; i < iterations; ++i)
            {
                result = table.Query(query);
            }

            Console.WriteLine();
            ShowResult(result.Values);
        }

        private static void QueryPerformanceTest(Table table, string fullQueryWhere)
        {
            int passes = 100;
            int iterations = passes * fullQueryWhere.Length;

            SelectQuery query = new SelectQuery();
            query.Columns = new string[] { "ID", "Title" };
            query.TableName = "Bugs";
            query.Count = 25;
            query.Highlighter = new Highlighter("[", "]");

            SelectResult result = null;

            Trace.Write(String.Format("Querying Bugs [{0:n0} iterations]...\r\n", iterations));
            ProgressWriter p = new ProgressWriter(passes);
            {
                for (int pass = 0; pass < passes; ++pass)
                {
                    for (int index = 1; index <= fullQueryWhere.Length; ++index)
                    {
                        query.Where = SelectQuery.ParseWhere(fullQueryWhere.Substring(0, index));
                        result = table.Select(query);
                    }

                    p.IncrementProgress();
                }
            }

            Trace.Write(String.Format("\r\n{0}\r\n", query));
            Trace.Write(String.Format("Found {0:n0} items in {1}\r\n", result.Total, result.Runtime.ToFriendlyString()));
            Console.WriteLine();
            ShowResult(result.Values);
        }

        private static void ShowResult(DataBlock block)
        {
            for (int col = 0; col < block.ColumnCount; ++col)
            {
                if (col > 0) Trace.Write("\t");
                Trace.Write(block.Columns[col].Name);
            }
            Trace.Write("\r\n");

            for (int row = 0; row < block.RowCount; ++row)
            {
                for (int col = 0; col < block.ColumnCount; ++col)
                {
                    if (col > 0) Trace.Write("\t");
                    Trace.Write(block[row, col]);
                }

                Trace.Write("\r\n");
            }
        }

        private static bool CompareTables(Table expected, Table actual)
        {
            bool allComparisonsIdentical = true;

            // Compare overall total
            allComparisonsIdentical &= CompareAggregate(expected, actual, new AggregationQuery() { Aggregator = new CountAggregator(), TableName = expected.Name });

            // Compare a set of queries
            List<string> compareQueries = new List<string>() { "Priority = 1", "\"Created Date\" > \"2014-01-15\"", "CHIP", "Platform", "Scott Louvau", "AutoImage Parallel" };
            foreach (string queryString in compareQueries)
            {
                allComparisonsIdentical &= CompareSelect(expected, actual, new SelectQuery() { Columns = new string[] { "ID" }, Count = 1000, Where = SelectQuery.ParseWhere(queryString), TableName = expected.Name });
            }

            // Report success, if nothing failed
            if (allComparisonsIdentical)
            {
                Trace.WriteLine(String.Format("SUCCESS. All comparisons identical between '{0}' and '{1}'", expected.Name, actual.Name));
            }

            return allComparisonsIdentical;
        }

        private static bool CompareSelect(Table expected, Table actual, SelectQuery query)
        {
            StringBuilder differences = new StringBuilder();

            SelectResult resultExpected = expected.Select(query);
            SelectResult resultActual = actual.Select(query);

            if (resultExpected.Total != resultActual.Total)
            {
                differences.AppendLine(String.Format("Total; Expect: {0:n0}, Actual {1:n0}.", resultExpected.Total, resultActual.Total));
            }

            CompareValues(resultExpected.Values, resultActual.Values, differences);

            if (differences.Length > 0)
            {
                Trace.WriteLine(String.Format("{0}\r\n========================================\r\n{1}\r\n========================================\r\n\r\n", query, differences));
            }

            return differences.Length == 0;
        }

        private static bool CompareAggregate(Table expected, Table actual, AggregationQuery query)
        {
            StringBuilder differences = new StringBuilder();

            AggregationResult resultExpected = expected.Query(query);
            AggregationResult resultActual = actual.Query(query);

            if (resultExpected.Total != resultActual.Total)
            {
                differences.AppendLine(String.Format("Total; Expect: {0:n0}, Actual {1:n0}.", resultExpected.Total, resultActual.Total));
            }

            CompareValues(resultExpected.Values, resultActual.Values, differences);

            if (differences.Length > 0)
            {
                Trace.WriteLine(String.Format("{0}\r\n========================================\r\n{1}\r\n========================================\r\n\r\n", query, differences));
            }

            return differences.Length == 0;
        }

        private static void CompareValues(DataBlock expected, DataBlock actual, StringBuilder differences)
        {
            if (expected.RowCount != actual.RowCount)
            {
                differences.AppendLine(String.Format("RowCount; Expect: {0:n0}, Actual: {1:n0}", expected.RowCount, actual.RowCount));
            }

            if (expected.ColumnCount != actual.ColumnCount)
            {
                differences.AppendLine(String.Format("ColumnCount; Expect: {0:n0}, Actual: {1:n0}", expected.RowCount, actual.RowCount));
            }

            string expectedColumns = String.Join(", ", expected.Columns);
            string actualColumns = String.Join(", ", actual.Columns);
            if (!expectedColumns.Equals(actualColumns))
            {
                differences.AppendLine(String.Format("Columns;\r\n\tExpect: {0:n0},\r\n\tActual: {1:n0}", expectedColumns, actualColumns));
            }

            for (int rowIndex = 0; rowIndex < expected.RowCount; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex < expected.ColumnCount; ++columnIndex)
                {
                    object expectedValue = expected[rowIndex, columnIndex];
                    object actualValue = actual[rowIndex, columnIndex];

                    if (expectedValue == null && actualValue == null) continue;

                    if (expectedValue == null || actualValue == null || !expectedValue.Equals(actualValue))
                    {
                        differences.AppendLine(String.Format("[{0:n0}], {1:n0};\r\n\tExpect: {2},\r\n\tActual: {3}", rowIndex, columnIndex, expectedValue ?? "NULL", actualValue ?? "NULL"));
                    }
                }
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Arriba.Extensions;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Query;
using Arriba.Serialization.Csv;
using Arriba.Structures;

namespace Arriba.Csv
{
    internal class Program
    {
        public const int QueryResultLimit = 40;
        public const int BatchSize = 250;

        private const string Usage = @"
    Arriba.Csv builds or queries Arriba tables from CSV source data.
    Source data must include a unique ID column (the first column whose name ends with 'ID' or the first column).
    
    Arriba.Csv /mode:build /table:<TableName> /csvPath:<CsvFilePath> [/maximumCount:<RowLimitForTable>]? [/columns:""C1,C2,C3""]?
    Arriba.Csv /mode:query /table:<TableName> [/select:<ColumnList>]? [/orderBy:<ColumnName>]? [/count:<CountToShow>]?

    Ex:
      Arriba.Csv /mode:build /table:SP500 /csvPath:""C:\Temp\SP500 Price History.csv"" /maximumCount:50000
      Arriba.Csv /mode:query /table:SP500 /select:""Date, Adj Close"" /count:30
";

        private static void Main(string[] args)
        {
            try
            {
                CommandLine c = CommandLine.Parse();

                string mode = c.GetString("mode").ToLowerInvariant();
                switch (mode)
                {
                    case "build":
                        Build(false, c.GetString("table"), c.GetString("csvPath"), c.GetInt("maximumCount", 100000), c.GetString("columns", null));
                        break;
                    case "add":
                        Build(true, c.GetString("table"), c.GetString("csvPath"), c.GetInt("maximumCount", 100000), c.GetString("columns", null));
                        break;
                    case "query":
                        Query(c.GetString("table"), c.GetString("select", ""), c.GetString("orderBy", ""), c.GetInt("count", QueryResultLimit));
                        break;
                    default:
                        Console.WriteLine(Usage);
                        break;
                }
            }
            catch (CommandLineUsageException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(Usage);
            }
        }

        private static void Build(bool load, string tableName, string csvFilePath, int maximumCount, string columns)
        {
            Stopwatch w = Stopwatch.StartNew();
            Console.WriteLine("Building Arriba table '{0}' from '{1}'...", tableName, csvFilePath);

            IList<string> columnNames = null;
            if (!String.IsNullOrEmpty(columns)) columnNames = SplitAndTrim(columns);

            Table table;
            if (load)
            {
                table = new Table();
                table.Load(tableName);
            }
            else
            {
                table = new Table(tableName, maximumCount);
            }

            using (CsvReader reader = new CsvReader(csvFilePath))
            {
                long rowsImported = 0;

                foreach (DataBlock block in reader.ReadAsDataBlockBatch(BatchSize))
                {
                    DataBlock toInsert = block;
                    if (columnNames != null) toInsert = toInsert.StripToColumns(columnNames);

                    if(load && rowsImported == 0)
                    {
                        table.AddColumnsFromBlock(toInsert);
                    }

                    table.AddOrUpdate(toInsert);

                    rowsImported += toInsert.RowCount;
                    Console.Write(".");
                }

                Console.WriteLine();
                Console.WriteLine("Added {0:n0} rows. Saving...", rowsImported);
            }

            table.Save();
            w.Stop();
            Console.WriteLine("Done in {0}.", w.Elapsed.ToFriendlyString());
        }

        private static void Query(string tableName, string columnsToSelect, string orderByColumn, int countToShow)
        {
            Console.WriteLine("Loading '{0}'...", tableName);

            Table table = new Table();
            Stopwatch w = Stopwatch.StartNew();
            table.Load(tableName);
            w.Stop();
            Console.WriteLine("Done. Loaded '{0}' ({1:n0} rows) in {2}.", tableName, table.Count, w.Elapsed.ToFriendlyString());

            Console.Write(" > ");
            string query = Console.ReadLine();

            IEnumerable<string> selectList = SplitAndTrim(columnsToSelect);
            if (selectList.Count() == 0) selectList = table.ColumnDetails.Select((cd) => cd.Name);

            SelectQuery q = new SelectQuery(selectList, query);
            if (!String.IsNullOrEmpty(orderByColumn)) q.OrderByColumn = orderByColumn;
            q.Count = (countToShow < ushort.MaxValue ? (ushort)countToShow : ushort.MaxValue);

            w = Stopwatch.StartNew();
            SelectResult result = table.Select(q);
            w.Stop();

            StringBuilder output = new StringBuilder();

            output.AppendFormat("{0:n0} results for \"{1}\" found in {2}.\r\n", result.Total, result.Query.Where.ToString(), w.Elapsed.ToFriendlyString());
            output.AppendLine();

            IList<ColumnDetails> columns = result.Values.Columns;

            for (int columnIndex = 0; columnIndex < columns.Count; ++columnIndex)
            {
                if (columnIndex > 0) output.Append("\t");
                output.Append(columns[columnIndex].Name);
            }

            output.AppendLine();

            for (int rowIndex = 0; rowIndex < result.CountReturned; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex < columns.Count; ++columnIndex)
                {
                    if (columnIndex > 0) output.Append("\t");
                    output.Append(result.Values[rowIndex, columnIndex]);
                }

                output.AppendLine();
            }

            Console.WriteLine(output.ToString());
        }

        private static IList<string> SplitAndTrim(string commaDelimitedList)
        {
            if (String.IsNullOrEmpty(commaDelimitedList)) return new string[0];

            List<string> result = new List<string>();
            foreach (string value in commaDelimitedList.Split(','))
            {
                if (String.IsNullOrEmpty(value)) continue;
                result.Add(value.Trim());
            }

            return result;
        }
    }
}

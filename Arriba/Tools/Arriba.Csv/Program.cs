﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Arriba.Client.Serialization.Json;
using Arriba.Diagnostics;
using Arriba.Extensions;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Query;
using Arriba.Serialization.Csv;
using Arriba.Structures;

using Newtonsoft.Json;

namespace Arriba.Csv
{
    internal class Program
    {
        public const int QueryResultLimit = 40;
        public const int BatchSize = 250;

        private const string Usage = @"
    Arriba.Csv builds or queries Arriba tables from CSV source data.
    Source data must include a unique ID column (the first column whose name ends with 'ID' or the first column).
    'build' to create a new table and add all data.
    'decorate' mode used to add new columns to existing rows only.
    'query' to run a query from the command line.
    'getSettings' to write out JSON settings from a table.
    'setSettings' to configure a table from JSON settings.
    
    Arriba.Csv /mode:build /table:<TableName> /csvPath:<CsvFilePath> [/maximumCount:<RowLimitForTable>]? [/columns:""C1,C2,C3""]? [/settings:<SettingsJsonPath>]?
    Arriba.Csv /mode:query /table:<TableName> [/select:<ColumnList>]? [/orderBy:<ColumnName>]? [/count:<CountToShow>]?
    Arriba.Csv /mode:decorate /table:<TableName> /csvPath:<CsvFilePath> [/maximumCount:<RowLimitForTable>]? [/columns:""C1,C2,C3""]?
    Arriba.Csv /mode:getSettings /table:<TableName> /path:<SettingsJsonToWritePath>
    Arriba.Csv /mode:setSettings /table:<TableName> /path:<SettingsJsonToReadPath>

    Ex:
      Arriba.Csv /mode:build /table:SP500 /csvPath:""C:\Temp\SP500 Price History.csv"" /maximumCount:50000
      Arriba.Csv /mode:query /table:SP500 /select:""Date, Adj Close"" /count:30
";
        private static JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Formatting = Formatting.Indented, Converters = ConverterFactory.GetArribaConverters() };

        public enum AddMode
        {
            Build = 0,
            Append = 1,
            Decorate = 2
        }

        private static int Main(string[] args)
        {
            try
            {
                CommandLine c = CommandLine.Parse();

                string mode = c.GetString("mode").ToLowerInvariant();
                switch (mode)
                {
                    case "build":
                        Build(AddMode.Build, c.GetString("table"), c.GetString("csvPath"), c.GetInt("maximumCount", 100000), c.GetString("columns", null), c.GetString("settings", null));
                        break;
                    case "append":
                        Build(AddMode.Append, c.GetString("table"), c.GetString("csvPath"), c.GetInt("maximumCount", 100000), c.GetString("columns", null), c.GetString("settings", null));
                        break;
                    case "decorate":
                        Build(AddMode.Decorate, c.GetString("table"), c.GetString("csvPath"), c.GetInt("maximumCount", 100000), c.GetString("columns", null));
                        break;
                    case "query":
                        Query(c.GetString("table"), c.GetString("select", ""), c.GetString("orderBy", ""), c.GetInt("count", QueryResultLimit));
                        break;
                    case "getsettings":
                        GetSettings(c.GetString("table"), c.GetString("path"));
                        break;
                    case "setsettings":
                        SetSettings(c.GetString("table"), c.GetString("path"));
                        break;
                    default:
                        Console.WriteLine(Usage);
                        break;
                }

                return 0;
            }
            catch (CommandLineUsageException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(Usage);
                return -1;
            }
            catch(Exception ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine(ex.Message);
                return -2;
            }
        }

        private static IList<ColumnDetails> ParseSchemaFile(string schemaFilePath)
        {
            List<ColumnDetails> columns = new List<ColumnDetails>();
            foreach (string line in File.ReadAllLines(schemaFilePath))
            {
                string[] values = line.Split('\t');
                ColumnDetails d;

                if (values.Length == 1)
                {
                    d = new ColumnDetails(values[0]);
                }
                else if (values.Length == 2)
                {
                    d = new ColumnDetails(values[0], values[1], null);
                }
                else if (values.Length == 3)
                {
                    d = new ColumnDetails(values[0], values[1], null);
                }
                else if(values.Length == 5)
                {
                    d = new ColumnDetails(values[0], values[1], values[2], values[3], bool.Parse(values[4]));
                }
                else
                {
                    throw new InvalidOperationException(String.Format("Schema Files must be tab delimited, and may contain 1, 2, 3, or 5 values:\r\nName\tType?\tDefault?\tAlias?\tIsPrimaryKey\r\n. Line Read: {0}", line));
                }

                columns.Add(d);
            }

            return columns;
        }

        private static void Build(AddMode mode, string tableName, string csvFilePath, int maximumCount, string columns, string settingsJsonPath = null)
        {
            Stopwatch w = Stopwatch.StartNew();
            Console.WriteLine("{0} Arriba table '{1}' from '{2}'...", mode, tableName, csvFilePath);

            IList<string> columnNames = null;
            if (!String.IsNullOrEmpty(columns)) columnNames = SplitAndTrim(columns);

            // Build or load table
            Table table;
            if (mode == AddMode.Build)
            {
                table = new Table(tableName, maximumCount);
            }
            else
            { 
                table = new Table();
                table.Load(tableName);
            }

            // Configure table
            if (!String.IsNullOrEmpty(settingsJsonPath))
            {
                SetSettings(table, LoadSettings(settingsJsonPath));
            }

            // Always add missing columns. Add rows only when not in 'decorate' mode
            AddOrUpdateOptions options = new AddOrUpdateOptions();
            options.AddMissingColumns = true;
            options.Mode = (mode == AddMode.Decorate ? AddOrUpdateMode.UpdateAndIgnoreAdds: AddOrUpdateMode.AddOrUpdate);

            using (CsvReader reader = new CsvReader(csvFilePath))
            {
                long rowsImported = 0;

                foreach (DataBlock block in reader.ReadAsDataBlockBatch(BatchSize))
                {
                    DataBlock toInsert = block;
                    if (columnNames != null) toInsert = toInsert.StripToColumns(columnNames);

                    table.AddOrUpdate(toInsert.AsReadOnly(), options);

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
            SelectResult result = table.Query(q);
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

        private static CombinedSettings LoadSettings(string settingsJsonPath)
        { 
            CombinedSettings settings = new CombinedSettings();
            string settingsJson = File.ReadAllText(settingsJsonPath);
            return JsonConvert.DeserializeObject<CombinedSettings>(settingsJson, serializerSettings);
        }

        private static void GetSettings(string tableName, string settingsJsonPath)
        {
            Console.WriteLine("Reading settings from '{0}' and writing to '{1}'...", tableName, settingsJsonPath);

            CombinedSettings settings = new CombinedSettings();

            SecureDatabase db = new SecureDatabase();
            settings.Security = db.Security(tableName);

            Table t = db[tableName];
            settings.ItemCountLimit = t.PartitionCount * ushort.MaxValue;
            settings.Schema = new List<ColumnDetails>(t.ColumnDetails);

            string settingsJson = JsonConvert.SerializeObject(settings, serializerSettings);
            File.WriteAllText(settingsJsonPath, settingsJson);
        }

        private static void SetSettings(string tableName, string settingsJsonPath)
        {
            Console.WriteLine("Applying settings from '{0}' to '{1}'...", settingsJsonPath, tableName);

            // Read settings file
            CombinedSettings settings = LoadSettings(settingsJsonPath);

            // Create table, if required
            SecureDatabase db = new SecureDatabase();
            if (!db.TableExists(tableName))
            {
                db.AddTable(tableName, settings.ItemCountLimit);
            }

            // Apply the settings
            SetSettings(db[tableName], settings);

        }

        private static void SetSettings(Table table, CombinedSettings settings)
        {
            // Set and write security
            SecureDatabase db = new SecureDatabase();
            db.SetSecurity(table.Name, settings.Security);
            db.SaveSecurity(table.Name);

            // Create/Modify columns
            foreach (ColumnDetails cd in settings.Schema)
            {
                table.AddColumn(cd);
            }
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

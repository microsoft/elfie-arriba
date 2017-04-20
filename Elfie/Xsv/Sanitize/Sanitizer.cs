using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using System.Collections.Generic;
using XsvConcat;

namespace Xsv.Sanitize
{
    public class Sanitizer
    {
        public ISanitizerProvider Provider { get; set; }
        public int HashKeyHash { get; set; }
        public string SpecFilePath { get; set; }

        public string SampleColumnName { get; set; }
        public double SampleProbability { get; set; }

        public HashSet<String8> EchoValues { get; set; }
        public HashSet<string> DropColumns { get; set; }
        public Dictionary<string, IColumnHandler> HandlersByColumn { get; set; }

        public Sanitizer(string specFilePath, string hashKey)
        {
            this.Provider = new SanitizerProvider();
            this.HashKeyHash = hashKey.GetHashCode();
            this.SpecFilePath = specFilePath;

            // Track values to Echo. Empty string is always Echoed.
            this.EchoValues = new HashSet<String8>();
            this.EchoValues.Add(String8.Empty);

            // Track columns to drop (exclude from output)
            this.DropColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Track the map handler for columns being mapped
            this.HandlersByColumn = new Dictionary<string, IColumnHandler>(StringComparer.OrdinalIgnoreCase);

            LoadSpec();
        }

        public void LoadSpec()
        {
            using (ITabularReader r = new CsvReader(this.SpecFilePath, false))
            {
                while (r.NextRow())
                {
                    string command = r.Current(0).ToString();
                    string columnName = r.Current(1).ToString();

                    switch (command.ToLowerInvariant())
                    {
                        case "sample":
                            this.SampleColumnName = columnName;
                            this.SampleProbability = double.Parse(r.Current(2).ToString());
                            if (this.SampleProbability < 0.0 || this.SampleProbability > 1.0) throw new UsageException($"SanitizeSpec sample probability ({r.Current(2)}) is out of range. It must be between zero and one.");
                            break;
                        case "echo":
                            for (int i = 1; i < r.CurrentRowColumns; ++i)
                            {
                                this.EchoValues.Add(r.Current(i).ToString8());
                            }
                            break;
                        case "drop":
                            this.DropColumns.Add(columnName);
                            break;
                        case "map":
                            this.HandlersByColumn.Add(columnName, new EchoColumnHandler(this.EchoValues, new MapColumnHandler(this.HashKeyHash, r.Current(2).ToString(), this.Provider, RowToArray(r, 3))));
                            break;
                        default:
                            throw new UsageException($"SanitizeSpec mode '{command}' is unknown. Supported modes: sample, echo, drop, map.");
                    }
                }
            }
        }

        internal static string[] RowToArray(ITabularReader reader, int fromColumnIndex = 0)
        {
            int length = reader.CurrentRowColumns - fromColumnIndex;

            string[] result = new string[length];
            for (int i = 0; i < length; ++i)
            {
                result[i] = reader.Current(fromColumnIndex + i).ToString();
            }

            return result;
        }

        private IColumnHandler[] GetHandlersByColumnIndex(IReadOnlyList<string> columnNames, out List<string> columnNamesToOutput)
        {
            columnNamesToOutput = new List<string>();
            IColumnHandler[] handlers = new IColumnHandler[columnNames.Count];

            for (int i = 0; i < columnNames.Count; ++i)
            {
                string columnName = columnNames[i];

                IColumnHandler handler;
                if(this.DropColumns.Contains(columnName))
                {
                    handler = new DropColumnHandler();
                }
                else
                {
                    columnNamesToOutput.Add(columnName);

                    if (!this.HandlersByColumn.TryGetValue(columnName, out handler))
                    {
                        handler = new KeepColumnHandler();
                    }
                }

                handlers[i] = handler;
            }

            return handlers;
        }

        public void Sanitize(string inputFile, string outputFile)
        {
            using (ITabularReader reader = TabularFactory.BuildReader(inputFile))
            {
                List<string> columnsToOutput;
                IColumnHandler[] handlers = GetHandlersByColumnIndex(reader.Columns, out columnsToOutput);

                int sampleColumnIndex = (String.IsNullOrEmpty(this.SampleColumnName) ? -1 : reader.ColumnIndex(this.SampleColumnName));
                uint sampleInclusionCutoff = (uint)(uint.MaxValue * this.SampleProbability);

                using (ITabularWriter writer = TabularFactory.BuildWriter(outputFile))
                {
                    writer.SetColumns(columnsToOutput);

                    while(reader.NextRow())
                    {
                        if(sampleColumnIndex > -1)
                        {
                            uint sampleValueHash = MapColumnHandler.Hash(reader.Current(sampleColumnIndex).ToString8(), 0);
                            if (sampleValueHash > sampleInclusionCutoff) continue;
                        }

                        for(int i = 0; i < reader.CurrentRowColumns; ++i)
                        {
                            handlers[i].Sanitize(reader.Current(i), writer);
                        }

                        writer.NextRow();
                    }
                }
            }
        }
    }
}

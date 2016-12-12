// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace XsvConcat
{
    internal class Program
    {
        private const string Usage =
@"Usage: Xsv <mode> <inputFile> <outputFile> [<options>]
   Xsv concat <input> <output>:
     Concatenate values by the first column value, excluding duplicates.
     Input must be sorted by the first column to concatenate.

  Xsv notStartsWith <input> <output> <valueColumnIndexOrName> <nameColumnIndexOrName>:
     Copy the input, excluding rows where row[valueIndex].StartsWith(row[nameIndex]).
            ";

        private static int Main(string[] args)
        {
            if (args == null || args.Length < 3)
            {
                Console.WriteLine(Usage);
                return -1;
            }

            string mode = args[0].ToLowerInvariant();
            string inputFilePath = args[1];
            string outputFilePath = args[2];

            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine("ERROR: Input file, \"{0}\", was not found.", inputFilePath);
                return -2;
            }

            try
            {
                using (new TraceWatch(String.Empty))
                {
                    using (BaseTabularReader reader = BuildReader(inputFilePath))
                    {
                        using (BaseTabularWriter writer = BuildWriter(outputFilePath, reader.Columns))
                        {
                            switch (mode)
                            {
                                case "concat":
                                    Trace.WriteLine(String.Format("Concatenating \"{0}\" values on first column into \"{1}\"...", inputFilePath, outputFilePath));
                                    Concatenate(reader, writer, String8.Convert("; ", new byte[2]));

                                    break;
                                case "notstartswith":
                                    if (args.Length < 5)
                                    {
                                        Console.WriteLine(Usage);
                                        return -1;
                                    }

                                    int valueIndex = GetColumnIndex(args[3], reader);
                                    int nameIndex = GetColumnIndex(args[4], reader);

                                    Trace.WriteLine(String.Format("Writing \"{0}\" values into \"{1}\" where !row[{2}].StartsWith(row[{3}])", inputFilePath, outputFilePath, args[3], args[4]));
                                    NotStartsWith(reader, writer, valueIndex, nameIndex);

                                    break;
                                default:
                                    throw new NotSupportedException(String.Format("XSV mode \"{0}\" is unknown. Run without arguments to see valid modes.", mode));
                            }

                            Trace.WriteLine(String.Format("Read: {0}, {1:n0} rows.", new FileInfo(inputFilePath).Length.SizeString(), reader.RowCountRead));
                            Trace.WriteLine(String.Format("Wrote: {0}, {1:n0} rows.", writer.BytesWritten.SizeString(), writer.RowCountWritten));
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
                return -1;
            }
        }

        private static BaseTabularReader BuildReader(string filePath, bool hasHeaderRow = true)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            switch (extension)
            {
                case ".csv":
                    return new CsvReader(filePath, hasHeaderRow);
                case ".tsv":
                case ".tab":
                case ".txt":
                    return new TsvReader(filePath, hasHeaderRow);
                default:
                    throw new NotSupportedException(String.Format("Xsv does not support file extension \"{0}\". Pass a .tsv or .csv file.", extension));
            }
        }

        private static BaseTabularWriter BuildWriter(string filePath, IEnumerable<string> columnNames, bool writeHeaderRow = true)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            switch (extension)
            {
                case ".csv":
                    return new CsvWriter(filePath, columnNames, writeHeaderRow);
                case ".tsv":
                case ".tab":
                case ".txt":
                    return new TsvWriter(filePath, columnNames, writeHeaderRow);
                default:
                    throw new NotSupportedException(String.Format("Xsv does not support file extension \"{0}\". Pass a .tsv or .csv file.", extension));
            }
        }

        private static int GetColumnIndex(string columnIndexOrName, BaseTabularReader reader)
        {
            int columnIndex;

            if (int.TryParse(columnIndexOrName, out columnIndex))
            {
                if (columnIndex >= reader.Columns.Count)
                {
                    throw new ArgumentOutOfRangeException(String.Format("Input file had {0:n0} columns; column index {1:n0} out of range.", reader.Columns.Count, columnIndex));
                }
            }
            else
            {
                // Look up column index. Will throw if not found
                columnIndex = reader.ColumnIndex(columnIndexOrName);
            }

            return columnIndex;
        }

        private static void NotStartsWith(BaseTabularReader reader, BaseTabularWriter writer, int valueColumnIndex, int nameColumnIndex)
        {
            while (reader.NextRow())
            {
                String8 name = reader.Current[nameColumnIndex];
                String8 value = reader.Current[valueColumnIndex];

                if (!value.StartsWith(name))
                {
                    for (int i = 0; i < reader.CurrentRowColumns; ++i)
                    {
                        writer.Write(reader.Current[i]);
                    }

                    writer.NextRow();
                }
            }
        }

        private static void Concatenate(BaseTabularReader reader, BaseTabularWriter writer, String8 delimiter)
        {
            String8Block block = new String8Block();
            String8[] lastValues = new String8[reader.CurrentRowColumns];
            String8[] combinedValues = new String8[reader.CurrentRowColumns];

            while (reader.NextRow())
            {
                String8 firstColumn = reader.Current[0];

                if (reader.RowCountRead == 2)
                {
                    // First Row - Get the first ID only
                    combinedValues[0] = block.GetCopy(firstColumn);
                }
                else if (firstColumn.CompareTo(combinedValues[0], true) != 0)
                {
                    // If we have a new ID (and not first row)

                    // Write concatenated values for previous ID
                    WriteCombinedRow(writer, combinedValues);

                    // Reset for this ID
                    block.Clear();
                    combinedValues[0] = block.GetCopy(firstColumn);

                    for (int i = 1; i < combinedValues.Length; ++i)
                    {
                        combinedValues[i] = String8.Empty;
                    }
                }

                // Concatenate non-duplicate values to "row in progress"
                for (int i = 1; i < reader.CurrentRowColumns; ++i)
                {
                    String8 value = reader.Current[i];

                    if (lastValues[i] != value)
                    {
                        lastValues[i] = value;
                        combinedValues[i] = block.Concatenate(combinedValues[i], delimiter, value);
                    }
                }
            }

            // After last row, write out values so far
            WriteCombinedRow(writer, combinedValues);
        }

        private static void WriteCombinedRow(BaseTabularWriter writer, String8[] values)
        {
            for (int i = 0; i < values.Length; ++i)
            {
                writer.Write(values[i]);
            }

            writer.NextRow();
        }
    }
}

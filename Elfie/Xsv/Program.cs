// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

using Xsv.Where;
using System.IO;
using System.Linq;

namespace Xsv
{
    public class Program
    {
        private const string Usage =
@"Usage: Xsv <mode> <inputFile> <outputFile> [<options>]
    Xsv copy <input> <output> [<col,col,col>]:
     Copy the input to the output (to convert format).
     Pass comma delimited column names to copy only specific columns.

    Xsv where <input> <columnIdentifier> <operator> <value> <output|cout|"""">
     Write the row to the output where the column matches the value.
     Omit the output to count results only.

    Xsv distinct <input> <columnIdentifier> <output|cout|"""">
     Write each distinct value seen in the column in input to the output.

    Xsv first <input> <output> <rowCount>
     Copy the first <rowCount> rows from input to output.

    Xsv append <inputFileOrFolder> <outputToAppendTo> <inputFileNamePattern?>
     Append rows from input(s) to an existing output file.
     Pass a folder path to merge all files in the folder, as long as columns are the same.

    Xsv rowId <input> <output> <firstId?>
     Add an incrementing integer ID column as the first column, starting with the provided value or 1.

    Xsv concat <input> <output>:
     Concatenate values by the first column value, excluding duplicates.
     Input must be sorted by the first column to concatenate.

    Xsv concatCol <input> <output> <col1> <separator> <col2> <outColName>
     Concatenate column values together: out['outColName'] = row['col1'] + separator + row['col2'].

    Xsv notStartsWith <input> <output> <valueColumnIndexOrName> <nameColumnIndexOrName>:
     Copy the input, excluding rows where row[valueIndex].StartsWith(row[nameIndex]).

    Xsv compare <oldInputPath> <newInputPath> <output> <columnIndexOrName>
     Compare the set of values for the column between the two inputs and write the differences.

    Xsv onlyIn <input> <output> <onlyInFilePath> <onlyInColumnIdentifier>
     Copy rows from input to output if the 'onlyInColumnIdentifier' was also found in 'onlyInFilePath'.

    Xsv onlyLatest <inputFolder> <outputFile> <idColumnIdentifier>
     Copy the last row for each ID from the input folder to the output file, walking inputs alphabetically.

    Xsv htmlInnerText <input> <output> <columnIdentifier>
     Copy rows from input to output, converting HTML in the column with the inner text equivalent.

    Xsv sanitize <input> <output> <specFile> <hashKey>
     Sanitize (re-map identifying values) from input to output using specFile rules.
     Makes safe sample data from sensitive data by remapping values.

    Xsv sanitizeValue <value> <columnName> <specFile> <hashKey>
     Translate a single value from a given column. Used to map values to allow
     investigations on sanitized data.
";

        public static int Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            if (args == null || args.Length < 3)
            {
                Trace.WriteLine(Usage);
                return -1;
            }

            string mode = args[0].ToLowerInvariant();

            try
            {
                using (new TraceWatch(String.Empty))
                {
                    switch (mode)
                    {
                        case "copy":
                            Trace.WriteLine(String.Format("Copy \"{0}\" to \"{1}\"...", args[1], args[2]));
                            if (args.Length < 4)
                            {
                                Copy(args[1], args[2]);
                            }
                            else
                            {
                                Copy(args[1], args[2], args[3]);
                            }
                            break;
                        case "first":
                            if (args.Length < 4) throw new UsageException("first requires an input, output, and row count.");
                            Trace.WriteLine(String.Format("Getting first {2} rows from \"{0}\" into \"{1}\"...", args[1], args[2], args[3]));
                            Copy(args[1], args[2], int.Parse(args[3]));
                            break;
                        case "distinct":
                            if (args.Length < 4) throw new UsageException("distinct requires an input, output, and column identifier.");
                            Trace.WriteLine(String.Format("Writing Distinct values for {2} from \"{0}\" into \"{1}\"...", args[1], args[2], args[3]));
                            Distinct(args[1], args[2], args[3]);
                            break;
                        case "append":
                            if (args.Length < 3) throw new UsageException("append requires an input and output");
                            Trace.WriteLine(String.Format("Appending from \"{0}\" to \"{1}\"...", args[1], args[2]));
                            Append(args[1], args[2], (args.Length > 3 ? args[3] : null));
                            break;
                        case "rowid":
                            if (args.Length < 3) throw new UsageException("rowid requires an input and output");
                            Trace.WriteLine(String.Format("Adding autoincrementing ID column from \"{0}\" to \"{1}\"...", args[1], args[2]));
                            RowId(args[1], args[2], (args.Length > 3 ? int.Parse(args[3]) : 1));
                            break;
                        case "concat":
                            Trace.WriteLine(String.Format("Concatenating \"{0}\" values on first column into \"{1}\"...", args[1], args[2]));
                            Concatenate(args[1], args[2], String8.Convert("; ", new byte[2]));
                            break;
                        case "concatcol":
                            if (args.Length < 7) throw new UsageException("concatCol requires input, output, col1, separator, col2, outColName");
                            Trace.WriteLine(String.Format("Concatenating \"[{2}] + \"{3}\" + [{4}]\" from \"{0}\" into column \"{5}\" in \"{1}\"...", args[0], args[1], args[2], args[3], args[4], args[5], args[6]));
                            ConcatenateColumn(args[1], args[2], args[3], args[4], args[5], args[6]);
                            break;
                        case "notstartswith":
                            if (args.Length < 5) throw new UsageException("notStartsWith requires a value and name column to be passed.");
                            Trace.WriteLine(String.Format("Writing \"{0}\" values into \"{1}\" where !row[{2}].StartsWith(row[{3}])", args[1], args[2], args[3], args[4]));
                            NotStartsWith(args[1], args[2], args[3], args[4]);
                            break;
                        case "compare":
                            if (args.Length < 5) throw new UsageException("compare requires two input files, an output file, and a column identifier to compare.");
                            Trace.WriteLine(String.Format("Comparing values for \"{0}\" values between \"{1}\" and \"{2}\"...", args[1], args[2], args[3], args[4]));
                            Compare(args[1], args[2], args[3], args[4]);
                            break;
                        case "onlyin":
                            if (args.Length < 5) throw new UsageException("onlyIn requires a second input file and column identifier");
                            Trace.WriteLine(String.Format("Writing \"{0}\" values into \"{1}\" where \"{2}\" also had the same \"{3}\"...", args[1], args[2], args[3], args[4]));
                            OnlyIn(args[1], args[2], args[3], args[4]);
                            break;
                        case "onlylatest":
                            if (args.Length < 4) throw new UsageException("onlyLatest requires an input folder, output file, and column identifier");
                            Trace.WriteLine(String.Format("Copying latest rows by \"{2}\" from \"{0}\" into \"{1}\"...", args[1], args[2], args[3]));
                            OnlyLatest(args[1], args[2], args[3]);
                            break;
                        case "sanitize":
                            if (args.Length < 5) throw new UsageException("sanitize requires input, output, specFile, hashKey");
                            Trace.WriteLine(String.Format("Sanitizing \"{0}\" into \"{1}\" using \"{2}\"...", args[1], args[2], args[3]));
                            Xsv.Sanitize.Sanitizer s = new Xsv.Sanitize.Sanitizer(args[3], args[4]);
                            s.Sanitize(args[1], args[2]);
                            break;
                        case "sanitizevalue":
                            if (args.Length < 5) throw new UsageException("sanitize requires value, columnName, specFile, hashKey");
                            Trace.WriteLine(String.Format("Sanitizing \"{0}\" from column \"{1}\" using \"{2}\"...", args[1], args[2], args[3]));
                            Trace.WriteLine(new Xsv.Sanitize.Sanitizer(args[3], args[4]).Translate(args[1], args[2]));
                            break;
                        case "htmlinnertext":
                            if (args.Length < 4) throw new UsageException("htmlInnerText requires an input file, output file, and column identifier");
                            Trace.WriteLine(String.Format("Converting Html to Text in \"{2}\" from \"{0}\" into \"{1}\"...", args[1], args[2], args[3]));
                            HtmlInnerText(args[1], args[2], args[3]);
                            break;
                        case "where":
                            if (args.Length < 3) throw new UsageException("where requires input, column, operator, value");
                            Where(args[1], args[2], args[3], args[4], (args.Length > 5 ? args[5] : null));
                            break;
                        default:
                            throw new NotSupportedException(String.Format("XSV mode \"{0}\" is unknown. Run without arguments to see valid modes.", mode));
                    }
                }

                return 0;
            }
            catch (UsageException ex)
            {
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(Usage);
                return -2;
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Trace.WriteLine("ERROR: " + ex.ToString());
                return -1;
            }
        }

        private static void Copy(string inputFilePath, string outputFilePath, int rowLimit = -1)
        {
            using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
            {
                using (ITabularWriter writer = TabularFactory.BuildWriter(outputFilePath))
                {
                    writer.SetColumns(reader.Columns);

                    while (reader.NextRow())
                    {
                        for (int i = 0; i < reader.CurrentRowColumns; ++i)
                        {
                            writer.Write(reader.Current(i).ToString8());
                        }

                        writer.NextRow();
                        if (writer.RowCountWritten == rowLimit) break;
                    }

                    WriteSizeSummary(reader, writer);
                }
            }
        }

        private static void Copy(string inputFilePath, string outputFilePath, string columnsDelimited)
        {
            List<string> columns = new List<string>();
            foreach (string columnName in columnsDelimited.Split(','))
            {
                columns.Add(columnName.Trim());
            }

            using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
            {
                int[] columnIndices = new int[columns.Count];
                for (int i = 0; i < columnIndices.Length; ++i)
                {
                    columnIndices[i] = reader.ColumnIndex(columns[i]);
                }

                using (ITabularWriter writer = TabularFactory.BuildWriter(outputFilePath))
                {
                    writer.SetColumns(columns);

                    while (reader.NextRow())
                    {
                        for (int i = 0; i < columnIndices.Length; ++i)
                        {
                            writer.Write(reader.Current(columnIndices[i]).ToString8());
                        }

                        writer.NextRow();
                    }

                    WriteSizeSummary(reader, writer);
                }
            }
        }

        private static void Append(string inputFileOrFolderPath, string outputFilePath, string inputFileNamePattern = null)
        {
            string[] inputFilePaths;

            if (Directory.Exists(inputFileOrFolderPath))
            {
                if (String.IsNullOrEmpty(inputFileNamePattern)) inputFileNamePattern = "*.*";
                inputFilePaths = Directory.GetFiles(inputFileOrFolderPath, inputFileNamePattern);
            }
            else
            {
                inputFilePaths = new string[] { inputFileOrFolderPath };
            }

            ITabularWriter writer = null;
            string writerColumns = null;
            try
            {
                foreach (string inputFilePath in inputFilePaths)
                {
                    using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
                    {
                        // Build the writer, if this is the first file
                        if (writer == null)
                        {
                            writer = TabularFactory.AppendWriter(outputFilePath, reader.Columns);
                            writerColumns = String.Join(", ", reader.Columns);
                        }

                        // Validate columns match
                        string sourceColumns = String.Join(", ", reader.Columns);
                        if (string.Compare(writerColumns, sourceColumns, true) != 0)
                        {
                            throw new InvalidOperationException(string.Format("Can't append to \"{0}\" because the column names don't match.\r\nExpect: {1}\r\nActual: {2}", outputFilePath, writerColumns, sourceColumns));
                        }

                        // Copy the rows
                        CopyRows(reader, writer);

                        // Write a summary for this input file
                        Trace.WriteLine($" {inputFilePath}, {reader.RowCountRead:n0} rows; {reader.BytesRead.SizeString()}");
                    }
                }

                // Write a summary for the output file
                WriteSizeSummary(null, writer);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                    writer = null;
                }
            }
        }

        private static void CopyRows(ITabularReader reader, ITabularWriter writer)
        {
            while (reader.NextRow())
            {
                for (int i = 0; i < reader.CurrentRowColumns; ++i)
                {
                    writer.Write(reader.Current(i).ToString8());
                }

                writer.NextRow();
            }
        }

        private static void RowId(string inputFilePath, string outputFilePath, int firstId = 1)
        {
            int currentId = firstId;

            using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
            {
                using (ITabularWriter writer = TabularFactory.BuildWriter(outputFilePath))
                {
                    List<string> columns = new List<string>();
                    columns.Add("ID");
                    columns.AddRange(reader.Columns);

                    writer.SetColumns(columns);

                    while (reader.NextRow())
                    {
                        writer.Write(currentId);
                        currentId++;

                        for (int i = 0; i < reader.CurrentRowColumns; ++i)
                        {
                            writer.Write(reader.Current(i).ToString8());
                        }

                        writer.NextRow();
                    }

                    WriteSizeSummary(reader, writer);
                }
            }
        }

        private static void Compare(string oldFilePath, string newFilePath, string outputFilePath, string columnIdentifier)
        {
            String8Block block = new String8Block();
            HashSet<String8> oldValues = new HashSet<String8>();
            HashSet<String8> newValues = new HashSet<String8>();

            using (ITabularReader oldReader = TabularFactory.BuildReader(oldFilePath))
            {
                int leftColumnIndex = oldReader.ColumnIndex(columnIdentifier);
                while (oldReader.NextRow())
                {
                    oldValues.Add(block.GetCopy(oldReader.Current(leftColumnIndex)));
                }

                Trace.WriteLine(String.Format("Old: {0:n0} values for \"{1}\" in {2:n0} rows.", oldValues.Count, columnIdentifier, oldReader.RowCountRead));
            }

            using (ITabularReader newReader = TabularFactory.BuildReader(newFilePath))
            {
                int rightColumnIndex = newReader.ColumnIndex(columnIdentifier);
                while (newReader.NextRow())
                {
                    newValues.Add(block.GetCopy(newReader.Current(rightColumnIndex)));
                }

                Trace.WriteLine(String.Format("New: {0:n0} values for \"{1}\" in {2:n0} rows.", newValues.Count, columnIdentifier, newReader.RowCountRead));
            }

            HashSet<String8> oldOnly = new HashSet<String8>(oldValues);
            oldOnly.ExceptWith(newValues);

            HashSet<String8> newOnly = new HashSet<String8>(newValues);
            newOnly.ExceptWith(oldValues);

            Trace.WriteLine(String.Format("{0:n0} values were only in \"{1}\".\r\n{2:n0} values were only in \"{3}\".", oldOnly.Count, oldFilePath, newOnly.Count, newFilePath));

            String8 leftMarker = String8.Convert("-", new byte[1]);
            String8 rightMarker = String8.Convert("+", new byte[1]);
            using (ITabularWriter writer = TabularFactory.BuildWriter(outputFilePath))
            {
                writer.SetColumns(new string[] { "In", columnIdentifier });

                foreach (String8 value in oldOnly)
                {
                    writer.Write(leftMarker);
                    writer.Write(value);
                    writer.NextRow();
                }

                foreach (String8 value in newOnly)
                {
                    writer.Write(rightMarker);
                    writer.Write(value);
                    writer.NextRow();
                }
            }
        }

        private static void OnlyIn(string inputFilePath, string outputFilePath, string onlyInInputFilePath, string onlyInColumnIdentifier)
        {
            String8Block block = new String8Block();
            HashSet<String8> values = new HashSet<String8>();

            // Read values in 'onlyInInputFilePath'
            using (ITabularReader reader = TabularFactory.BuildReader(onlyInInputFilePath))
            {
                int leftColumnIndex = reader.ColumnIndex(onlyInColumnIdentifier);
                while (reader.NextRow())
                {
                    values.Add(block.GetCopy(reader.Current(leftColumnIndex)));
                }
            }

            // Copy from input to output where the column value is in the "only in" set
            using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
            {
                int valueColumnIndex = reader.ColumnIndex(onlyInColumnIdentifier);

                using (ITabularWriter writer = TabularFactory.BuildWriter(outputFilePath))
                {
                    writer.SetColumns(reader.Columns);

                    while (reader.NextRow())
                    {
                        if (values.Contains(reader.Current(valueColumnIndex).ToString8()))
                        {
                            for (int i = 0; i < reader.CurrentRowColumns; ++i)
                            {
                                writer.Write(reader.Current(i).ToString8());
                            }

                            writer.NextRow();
                        }
                    }

                    WriteSizeSummary(reader, writer);
                }
            }
        }

        private static void OnlyLatest(string inputFolderPath, string outputFilePath, string idColumnIdentifier)
        {
            String8Block block = new String8Block();
            Dictionary<String8, Tuple<string, int>> latestFileAndRowByID = new Dictionary<String8, Tuple<string, int>>();
            IReadOnlyList<string> writerColumns = null;

            // Walk the input files to figure out the latest copy of each ID
            Trace.WriteLine($"Identifying latest {idColumnIdentifier} in all files in {inputFolderPath}...");
            int rowCountRead = 0;
            foreach (string inputFilePath in Directory.GetFiles(inputFolderPath))
            {
                using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
                {
                    int idColumnIndex = reader.ColumnIndex(idColumnIdentifier);

                    while (reader.NextRow())
                    {
                        rowCountRead++;
                        String8 id = reader.Current(idColumnIndex).ToString8();
                        id.ToUpperInvariant();

                        // Record the file and row containing this ID, overwriting previous entries
                        latestFileAndRowByID[block.GetCopy(id)] = new Tuple<string, int>(inputFilePath, reader.RowCountRead);
                    }

                    // Capture the columns from the last CSV to write
                    writerColumns = reader.Columns;
                }
            }
            Trace.WriteLine($"Scan Complete. {rowCountRead:n0} rows read; {latestFileAndRowByID.Count:n0} distinct IDs found.");

            using (ITabularWriter writer = TabularFactory.BuildWriter(outputFilePath))
            {
                writer.SetColumns(writerColumns);
                int[] writerColumnIndexInReader = new int[writerColumns.Count];

                foreach (string inputFilePath in Directory.GetFiles(inputFolderPath))
                {
                    using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
                    {
                        // Look up each output column's position in the input file
                        for (int i = 0; i < writerColumns.Count; ++i)
                        {
                            reader.TryGetColumnIndex(writerColumns[i], out writerColumnIndexInReader[i]);
                        }

                        int idColumnIndex = reader.ColumnIndex(idColumnIdentifier);

                        while (reader.NextRow())
                        {
                            String8 id = reader.Current(idColumnIndex).ToString8();
                            id.ToUpperInvariant();

                            // Copy this row to the output file, *if* it's the latest for this ID
                            Tuple<string, int> latestForID = latestFileAndRowByID[id];
                            if (latestForID.Item1 == inputFilePath && latestForID.Item2 == reader.RowCountRead)
                            {
                                for (int i = 0; i < writerColumns.Count; ++i)
                                {
                                    int readerColumnIndex = writerColumnIndexInReader[i];
                                    if (readerColumnIndex >= 0 && readerColumnIndex < reader.CurrentRowColumns)
                                    {
                                        writer.Write(reader.Current(readerColumnIndex).ToString8());
                                    }
                                    else
                                    {
                                        writer.Write(String8.Empty);
                                    }
                                }

                                writer.NextRow();
                            }
                        }
                    }
                }

                WriteSizeSummary(null, writer);
            }
        }

        private static void NotStartsWith(string inputFilePath, string outputFilePath, string valueColumnIdentifier, string nameColumnIdentifier)
        {
            using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
            {
                int valueColumnIndex = reader.ColumnIndex(valueColumnIdentifier);
                int nameColumnIndex = reader.ColumnIndex(nameColumnIdentifier);

                using (ITabularWriter writer = TabularFactory.BuildWriter(outputFilePath))
                {
                    writer.SetColumns(reader.Columns);

                    while (reader.NextRow())
                    {
                        String8 name = reader.Current(nameColumnIndex).ToString8();
                        String8 value = reader.Current(valueColumnIndex).ToString8();

                        if (!value.StartsWith(name))
                        {
                            for (int i = 0; i < reader.CurrentRowColumns; ++i)
                            {
                                writer.Write(reader.Current(i).ToString8());
                            }

                            writer.NextRow();
                        }
                    }

                    WriteSizeSummary(reader, writer);
                }
            }
        }

        private static void Distinct(string inputFilePath, string outputFilePath, string columnIdentifier)
        {
            String8Block block = new String8Block();
            HashSet<String8> distinctValues = new HashSet<String8>();

            using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
            {
                int columnIndex = reader.ColumnIndex(columnIdentifier);

                using (ITabularWriter writer = TabularFactory.BuildWriter(outputFilePath))
                {
                    writer.SetColumns(new string[] { reader.Columns[columnIndex] });

                    while (reader.NextRow())
                    {
                        String8 value = reader.Current(columnIndex).ToString8();

                        if (!distinctValues.Contains(value))
                        {
                            distinctValues.Add(block.GetCopy(value));
                            writer.Write(value);
                            writer.NextRow();
                        }
                    }

                    WriteSizeSummary(reader, writer);
                }
            }
        }

        private static void HtmlInnerText(string inputFilePath, string outputFilePath, string columnsDelimited)
        {
            using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
            {
                List<int> columnIndicesToEscape = columnsDelimited.Split(',').Select((col) => reader.ColumnIndex(col.Trim())).ToList();

                using (ITabularWriter writer = TabularFactory.BuildWriter(outputFilePath))
                {
                    writer.SetColumns(reader.Columns);

                    while (reader.NextRow())
                    {
                        for (int i = 0; i < reader.CurrentRowColumns; ++i)
                        {
                            if (columnIndicesToEscape.Contains(i))
                            {
                                WriteHtmlEscaped(reader.Current(i).ToString8(), writer);
                            }
                            else
                            {
                                writer.Write(reader.Current(i).ToString8());
                            }
                        }

                        writer.NextRow();
                    }

                    WriteSizeSummary(reader, writer);
                }
            }
        }

        public static void WriteHtmlEscaped(String8 value, ITabularWriter writer)
        {
            writer.WriteValueStart();

            int writeFrom = 0;
            while (true)
            {
                // Look for an Html Tag
                int startOfTag = value.IndexOf((byte)'<', writeFrom);
                if (startOfTag == -1) break;

                // Write up to the tag
                writer.WriteValuePart(value.Substring(writeFrom, startOfTag - writeFrom));

                // Find the end of the tag
                int endOfTag = value.IndexOf((byte)'>', startOfTag + 1);
                if (endOfTag == -1)
                {
                    // Error: Unclosed tag, don't write anything else
                    writeFrom = value.Length;
                    break;
                }

                writeFrom = endOfTag + 1;
            }

            // Write the value after the last tag
            writer.WriteValuePart(value.Substring(writeFrom));

            writer.WriteValueEnd();
        }

        private static void Concatenate(string inputFilePath, string outputFilePath, String8 delimiter)
        {
            using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
            {
                using (ITabularWriter writer = TabularFactory.BuildWriter(outputFilePath))
                {
                    writer.SetColumns(reader.Columns);

                    String8Block block = new String8Block();
                    String8[] lastValues = new String8[reader.CurrentRowColumns];
                    String8[] combinedValues = new String8[reader.CurrentRowColumns];

                    while (reader.NextRow())
                    {
                        String8 firstColumn = reader.Current(0).ToString8();

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
                            String8 value = reader.Current(i).ToString8();

                            if (lastValues[i] != value)
                            {
                                lastValues[i] = value;
                                combinedValues[i] = block.Concatenate(combinedValues[i], delimiter, value);
                            }
                        }
                    }

                    // After last row, write out values so far
                    WriteCombinedRow(writer, combinedValues);
                    WriteSizeSummary(reader, writer);
                }
            }
        }

        private static void ConcatenateColumn(string inputFilePath, string outputFilePath, string columnName1, string separator, string columnName2, string outputColumnName)
        {
            String8 separator8 = String8.Convert(separator, new byte[String8.GetLength(separator)]);

            using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
            {
                // Find the columns to concatenate
                int columnIndex1 = reader.ColumnIndex(columnName1);
                int columnIndex2 = reader.ColumnIndex(columnName2);

                // Build an output column list and mapping from output order to input index, with '-1' for the concatenated value
                List<string> outputColumns = new List<string>();
                int[] indexMapping = new int[reader.Columns.Count - 1];
                bool hasConcatenatedColumn = false;

                for (int i = 0; i < reader.Columns.Count; ++i)
                {
                    string columnName = reader.Columns[i];

                    // If this is a column to concatenate...
                    if (columnName.Equals(reader.Columns[columnIndex1], StringComparison.OrdinalIgnoreCase)
                        || columnName.Equals(reader.Columns[columnIndex2], StringComparison.OrdinalIgnoreCase))
                    {
                        // .. if it's the first one, the output column will appear at this position
                        if (!hasConcatenatedColumn)
                        {
                            hasConcatenatedColumn = true;

                            indexMapping[outputColumns.Count] = -1;
                            outputColumns.Add(outputColumnName);
                        }
                    }
                    else
                    {
                        // Otherwise, copy this column through
                        indexMapping[outputColumns.Count] = i;
                        outputColumns.Add(columnName);
                    }
                }

                using (ITabularWriter writer = TabularFactory.BuildWriter(outputFilePath))
                {
                    writer.SetColumns(outputColumns);

                    while (reader.NextRow())
                    {
                        // Write columns in mapped order
                        for (int i = 0; i < indexMapping.Length; ++i)
                        {
                            int sourceColumnIndex = indexMapping[i];

                            if (sourceColumnIndex == -1)
                            {
                                // Write concatenated column
                                writer.WriteValueStart();
                                writer.WriteValuePart(reader.Current(columnIndex1).ToString8());
                                writer.WriteValuePart(separator8);
                                writer.WriteValuePart(reader.Current(columnIndex2).ToString8());
                                writer.WriteValueEnd();
                            }
                            else
                            {
                                writer.Write(reader.Current(sourceColumnIndex).ToString8());
                            }
                        }

                        writer.NextRow();
                    }

                    WriteSizeSummary(reader, writer);
                }
            }
        }

        private static void Where(string inputFilePath, string columnIdentifier, string op, string value, string outputFilePath)
        {
            WhereResult result;

            using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
            {
                using (ITabularWriter writer = (String.IsNullOrEmpty(outputFilePath) ? null : TabularFactory.BuildWriter(outputFilePath)))
                {
                    if (writer == null)
                    {
                        Console.WriteLine($"Counting from '{inputFilePath}' where {columnIdentifier} {op} {value}...");
                    }
                    else
                    {
                        Console.WriteLine($"Writing from '{inputFilePath}' where {columnIdentifier} {op} {value} into '{outputFilePath ?? ""}'...");
                    }

                    result = WhereMatcher.Where(reader, columnIdentifier, op, value, writer);
                }
            }

            Console.WriteLine($"Done. {result.MatchCount:n0} out of {result.RowCount:n0} rows matched.");
        }

        private static void WriteSizeSummary(ITabularReader reader, ITabularWriter writer)
        {
            if (reader != null)
            {
                long bytesRead = reader.BytesRead;
                if (bytesRead <= 0)
                {
                    Trace.WriteLine(String.Format("Read: {0:n0} rows.", reader.RowCountRead));
                }
                else
                {
                    Trace.WriteLine(String.Format("Read: {0}, {1:n0} rows.", bytesRead.SizeString(), reader.RowCountRead));
                }
            }

            if (writer != null)
            {
                long bytesWritten = writer.BytesWritten;
                if (bytesWritten <= 0)
                {
                    Trace.WriteLine(String.Format("Wrote: {0:n0} rows.", writer.RowCountWritten));
                }
                else
                {
                    Trace.WriteLine(String.Format("Wrote: {0}, {1:n0} rows.", bytesWritten.SizeString(), writer.RowCountWritten));
                }
            }
        }

        private static void WriteCombinedRow(ITabularWriter writer, String8[] values)
        {
            for (int i = 0; i < values.Length; ++i)
            {
                writer.Write(values[i]);
            }

            writer.NextRow();
        }
    }

    [Serializable]
    public class UsageException : Exception
    {
        public UsageException() { }
        public UsageException(string message) : base(message) { }
        public UsageException(string message, Exception inner) : base(message, inner) { }
        protected UsageException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

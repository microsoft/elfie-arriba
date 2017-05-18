// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Runtime.Serialization;

namespace XsvConcat
{
    public class Program
    {
        private const string Usage =
@"Usage: Xsv <mode> <inputFile> <outputFile> [<options>]
    Xsv copy <input> <output> [<col,col,col>]:
     Copy the input to the output (to convert format).
     Pass comma delimited column names to copy only specific columns.

    Xsv concat <input> <output>:
     Concatenate values by the first column value, excluding duplicates.
     Input must be sorted by the first column to concatenate.

    Xsv notStartsWith <input> <output> <valueColumnIndexOrName> <nameColumnIndexOrName>:
     Copy the input, excluding rows where row[valueIndex].StartsWith(row[nameIndex]).

    Xsv compare <oldInputPath> <newInputPath> <output> <columnIndexOrName>
     Compare the set of values for the column between the two inputs and write the differences.

    Xsv onlyIn <input> <output> <onlyInFilePath> <onlyInColumnIdentifier>
     Copy rows from input to output if the 'onlyInColumnIdentifier' was also found in 'onlyInFilePath'.

    Xsv sanitize <input> <output> <specFile> <hashKey>
     Sanitize (re-map identifying values) from input to output using specFile rules.
     Makes safe sample data from sensitive data by remapping values.

    Xsv sanitizeValue <value> <columnName> <specFile> <hashKey>
     Translate a single value from a given column. Used to map values to allow
     investigations on sanitized data.

    Xsv where <input> <columnIdentifier> <equalsValue> <output|cout|"""">
     Write the row index and rows where row[columnIdentifier] = <equalsValue>.
     Omit the output to count results only.
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
                        case "concat":
                            Trace.WriteLine(String.Format("Concatenating \"{0}\" values on first column into \"{1}\"...", args[1], args[2]));
                            Concatenate(args[1], args[2], String8.Convert("; ", new byte[2]));
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
                        case "where":
                            if (args.Length < 3) throw new UsageException("row requires input and rowIndex");
                            Where(args[1], args[2], (args.Length > 3 ? args[3] : null), (args.Length > 4 ? TabularFactory.BuildWriter(args[4]) : null));
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

        private static void Copy(string inputFilePath, string outputFilePath)
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
                for(int i = 0; i < columnIndices.Length; ++i)
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

                foreach(String8 value in oldOnly)
                {
                    writer.Write(leftMarker);
                    writer.Write(value);
                    writer.NextRow();
                }

                foreach(String8 value in newOnly)
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

        private static void Where(string inputFilePath, string columnIndentifier, string value, ITabularWriter writer)
        {
            int matchCount = 0;
            int rowCount = 0;

            using (ITabularReader reader = TabularFactory.BuildReader(inputFilePath))
            {
                int rowIndex = (value != null ? -1 : int.Parse(columnIndentifier));
                int colIndex = (value != null ? reader.ColumnIndex(columnIndentifier) : -1);

                while (reader.NextRow())
                {
                    // Match the row index if no value was passed
                    if (rowIndex != -1 && reader.RowCountRead != rowIndex) continue;

                    // Match the column value if passed
                    if (colIndex != -1)
                    {
                        if (reader.CurrentRowColumns <= colIndex) continue;
                        if (reader.Current(colIndex).ToString8().CompareTo(value, true) != 0) continue;
                    }

                    matchCount++;

                    // If this is the matching row, write it
                    if (writer != null)
                    {
                        if(writer.RowCountWritten == 0)
                        {
                            List<string> columns = new List<string>();
                            columns.Add("RowIndex");
                            columns.AddRange(reader.Columns);
                            writer.SetColumns(columns);
                        }

                        writer.Write(reader.RowCountRead);
                        for (int i = 0; i < reader.CurrentRowColumns; ++i)
                        {
                            writer.Write(reader.Current(i).ToString8());
                        }
                        writer.NextRow();
                    }

                    // If we matched row index, we're done
                    if (rowIndex != -1) break;
                }

                rowCount = reader.RowCountRead;
            }

            Console.WriteLine($"Done. {matchCount:n0} out of {rowCount:n0} rows matched.");
        }

        private static void WriteSizeSummary(ITabularReader reader, ITabularWriter writer)
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

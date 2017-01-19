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
using System.Runtime.Serialization;

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

  Xsv compare <oldInputPath> <newInputPath> <output> <columnIndexOrName>
     Compare the set of values for the column between the two inputs and write the differences.

  Xsv onlyIn <input> <output> <onlyInFilePath> <onlyInColumnIdentifier>
     Copy rows from input to output if the 'onlyInColumnIdentifier' was also found in 'onlyInFilePath'.
            ";

        private static int Main(string[] args)
        {
            if (args == null || args.Length < 3)
            {
                Console.WriteLine(Usage);
                return -1;
            }

            string mode = args[0].ToLowerInvariant();

            try
            {
                using (new TraceWatch(String.Empty))
                {
                    switch (mode)
                    {
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
                        default:
                            throw new NotSupportedException(String.Format("XSV mode \"{0}\" is unknown. Run without arguments to see valid modes.", mode));
                    }
                }

                return 0;
            }
            catch (UsageException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(Usage);
                return -2;
            }
            catch (Exception ex) when (!Debugger.IsAttached)
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

        private static void Compare(string oldFilePath, string newFilePath, string outputFilePath, string columnIdentifier)
        {
            String8Block block = new String8Block();
            HashSet<String8> oldValues = new HashSet<String8>();
            HashSet<String8> newValues = new HashSet<String8>();

            using (BaseTabularReader oldReader = BuildReader(oldFilePath))
            {
                int leftColumnIndex = oldReader.ColumnIndex(columnIdentifier);
                while (oldReader.NextRow())
                {
                    oldValues.Add(block.GetCopy(oldReader.Current[leftColumnIndex]));
                }

                Console.WriteLine("Old: {0:n0} values for \"{1}\" in {2:n0} rows.", oldValues.Count, columnIdentifier, oldReader.RowCountRead);
            }

            using (BaseTabularReader newReader = BuildReader(newFilePath))
            {
                int rightColumnIndex = newReader.ColumnIndex(columnIdentifier);
                while (newReader.NextRow())
                {
                    newValues.Add(block.GetCopy(newReader.Current[rightColumnIndex]));
                }

                Console.WriteLine("New: {0:n0} values for \"{1}\" in {2:n0} rows.", newValues.Count, columnIdentifier, newReader.RowCountRead);
            }

            HashSet<String8> oldOnly = new HashSet<String8>(oldValues);
            oldOnly.ExceptWith(newValues);

            HashSet<String8> newOnly = new HashSet<String8>(newValues);
            newOnly.ExceptWith(oldValues);

            Console.WriteLine("{0:n0} values were only in \"{1}\".\r\n{2:n0} values were only in \"{3}\".", oldOnly.Count, oldFilePath, newOnly.Count, newFilePath);

            String8 leftMarker = String8.Convert("-", new byte[1]);
            String8 rightMarker = String8.Convert("+", new byte[1]);
            using (BaseTabularWriter writer = BuildWriter(outputFilePath, new string[] { "In", columnIdentifier }))
            {
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
            using (BaseTabularReader reader = BuildReader(onlyInInputFilePath))
            {
                int leftColumnIndex = reader.ColumnIndex(onlyInColumnIdentifier);
                while (reader.NextRow())
                {
                    values.Add(block.GetCopy(reader.Current[leftColumnIndex]));
                }
            }

            // Copy from input to output where the column value is in the "only in" set
            using (BaseTabularReader reader = BuildReader(inputFilePath))
            {
                int valueColumnIndex = reader.ColumnIndex(onlyInColumnIdentifier);

                using (BaseTabularWriter writer = BuildWriter(outputFilePath, reader.Columns))
                {
                    while (reader.NextRow())
                    {
                        if (values.Contains(reader.Current[valueColumnIndex]))
                        {
                            for (int i = 0; i < reader.CurrentRowColumns; ++i)
                            {
                                writer.Write(reader.Current[i]);
                            }

                            writer.NextRow();
                        }
                    }

                    Trace.WriteLine(String.Format("Read: {0}, {1:n0} rows.", new FileInfo(inputFilePath).Length.SizeString(), reader.RowCountRead));
                    Trace.WriteLine(String.Format("Wrote: {0}, {1:n0} rows.", writer.BytesWritten.SizeString(), writer.RowCountWritten));
                }
            }
        }

        private static void NotStartsWith(string inputFilePath, string outputFilePath, string valueColumnIdentifier, string nameColumnIdentifier)
        {
            using (BaseTabularReader reader = BuildReader(inputFilePath))
            {
                int valueColumnIndex = reader.ColumnIndex(valueColumnIdentifier);
                int nameColumnIndex = reader.ColumnIndex(nameColumnIdentifier);

                using (BaseTabularWriter writer = BuildWriter(outputFilePath, reader.Columns))
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

                    Trace.WriteLine(String.Format("Read: {0}, {1:n0} rows.", new FileInfo(inputFilePath).Length.SizeString(), reader.RowCountRead));
                    Trace.WriteLine(String.Format("Wrote: {0}, {1:n0} rows.", writer.BytesWritten.SizeString(), writer.RowCountWritten));
                }
            }
        }

        private static void Concatenate(string inputFilePath, string outputFilePath, String8 delimiter)
        {
            using (BaseTabularReader reader = BuildReader(inputFilePath))
            {
                using (BaseTabularWriter writer = BuildWriter(outputFilePath, reader.Columns))
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

                    Trace.WriteLine(String.Format("Read: {0}, {1:n0} rows.", new FileInfo(inputFilePath).Length.SizeString(), reader.RowCountRead));
                    Trace.WriteLine(String.Format("Wrote: {0}, {1:n0} rows.", writer.BytesWritten.SizeString(), writer.RowCountWritten));
                }
            }
        }

        private static void WriteCombinedRow(BaseTabularWriter writer, String8[] values)
        {
            for (int i = 0; i < values.Length; ++i)
            {
                writer.Write(values[i]);
            }

            writer.NextRow();
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
}

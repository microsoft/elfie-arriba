// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Xsv.Where
{
    public class WhereResult
    {
        public string ColumnName { get; set; }
        public Operator Op { get; set; }
        public object Value { get; set; }

        public int ColumnIndex { get; set; }
        public int RowCount { get; set; }
        public int MatchCount { get; set; }
    }

    public static class WhereMatcher
    {
        public static WhereResult Where(ITabularReader reader, string columnIdentifier, string operatorString, string valueString, ITabularWriter writer)
        {
            int colIndex = reader.ColumnIndex(columnIdentifier);
            WhereResult result = new WhereResult() { ColumnIndex = colIndex, ColumnName = reader.Columns[colIndex], Op = OperatorExtensions.Parse(operatorString), Value = ConvertToBestType(valueString) };

            Type t = result.Value.GetType();
            if (t == typeof(bool))
            {
                MatchBoolCompare(reader, writer, result);
            }
            else if (t == typeof(int))
            {
                MatchIntCompare(reader, writer, result);
            }
            else if (t == typeof(DateTime))
            {
                MatchDateTimeCompare(reader, writer, result);
            }
            else if (result.Op == Operator.Contains)
            {
                MatchContains(reader, writer, result);
            }
            else if (result.Op == Operator.StartsWith)
            {
                MatchStartsWith(reader, writer, result);
            }
            else
            {
                MatchStringCompare(reader, writer, result);
            }

            result.RowCount = reader.RowCountRead;
            return result;
        }

        private static void MatchBoolCompare(ITabularReader reader, ITabularWriter writer, WhereResult result)
        {
            bool value = (bool)result.Value;

            while (reader.NextRow())
            {
                // Ensure the row has enough columns
                if (reader.CurrentRowColumns <= result.ColumnIndex) continue;

                // Ensure the value converts
                bool columnValue;
                if (!reader.Current(result.ColumnIndex).ToString8().TryToBoolean(out columnValue)) continue;

                int compareResult = columnValue.CompareTo(value);
                if (!result.Op.Matches(compareResult)) continue;

                result.MatchCount++;

                // If this is the matching row, write it
                EchoRow(reader, writer);
            }
        }

        private static void MatchIntCompare(ITabularReader reader, ITabularWriter writer, WhereResult result)
        {
            int value = (int)result.Value;

            while (reader.NextRow())
            {
                // Ensure the row has enough columns
                if (reader.CurrentRowColumns <= result.ColumnIndex) continue;

                // Ensure the value converts
                int columnValue;
                if (!reader.Current(result.ColumnIndex).ToString8().TryToInteger(out columnValue)) continue;

                int compareResult = columnValue.CompareTo(value);
                if (!result.Op.Matches(compareResult)) continue;

                result.MatchCount++;

                // If this is the matching row, write it
                EchoRow(reader, writer);
            }
        }

        private static void MatchDateTimeCompare(ITabularReader reader, ITabularWriter writer, WhereResult result)
        {
            DateTime value = (DateTime)result.Value;

            while (reader.NextRow())
            {
                // Ensure the row has enough columns
                if (reader.CurrentRowColumns <= result.ColumnIndex) continue;

                // Ensure the value converts
                DateTime columnValue;
                if (!reader.Current(result.ColumnIndex).ToString8().TryToDateTime(out columnValue)) continue;

                int compareResult = columnValue.CompareTo(value);
                if (!result.Op.Matches(compareResult)) continue;

                result.MatchCount++;

                // If this is the matching row, write it
                EchoRow(reader, writer);
            }
        }

        private static void MatchStringCompare(ITabularReader reader, ITabularWriter writer, WhereResult result)
        {
            string valueString = (string)result.Value;
            String8 value = String8.Convert(valueString, new byte[String8.GetLength(valueString)]);

            while (reader.NextRow())
            {
                // Ensure the row has enough columns
                if (reader.CurrentRowColumns <= result.ColumnIndex) continue;

                // Match the value
                int compareResult = reader.Current(result.ColumnIndex).ToString8().CompareTo(value);
                if (!result.Op.Matches(compareResult)) continue;

                result.MatchCount++;

                // If this is the matching row, write it
                EchoRow(reader, writer);
            }
        }

        private static void MatchStartsWith(ITabularReader reader, ITabularWriter writer, WhereResult result)
        {
            string valueString = (string)result.Value;
            String8 value = String8.Convert(valueString, new byte[String8.GetLength(valueString)]);

            while (reader.NextRow())
            {
                // Ensure the row has enough columns
                if (reader.CurrentRowColumns <= result.ColumnIndex) continue;

                // Match the value
                if (!reader.Current(result.ColumnIndex).ToString8().StartsWith(value)) continue;

                result.MatchCount++;

                // If this is the matching row, write it
                EchoRow(reader, writer);
            }
        }

        private static void MatchContains(ITabularReader reader, ITabularWriter writer, WhereResult result)
        {
            string valueString = (string)result.Value;
            String8 value = String8.Convert(valueString, new byte[String8.GetLength(valueString)]);

            while (reader.NextRow())
            {
                // Ensure the row has enough columns
                if (reader.CurrentRowColumns <= result.ColumnIndex) continue;

                // Match the value
                if (reader.Current(result.ColumnIndex).ToString8().IndexOf(value) == -1) continue;

                result.MatchCount++;

                // If this is the matching row, write it
                EchoRow(reader, writer);
            }
        }

        private static object ConvertToBestType(string valueString)
        {
            if (string.IsNullOrEmpty(valueString)) return "";

            bool valueAsBool;
            if (bool.TryParse(valueString, out valueAsBool)) return valueAsBool;

            int valueAsInt;
            if (int.TryParse(valueString, out valueAsInt)) return valueAsInt;

            DateTime valueAsDateTime;
            if (DateTime.TryParse(valueString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out valueAsDateTime)) return valueAsDateTime;

            return valueString;
        }

        private static void EchoRow(ITabularReader reader, ITabularWriter writer)
        {
            if (writer != null)
            {
                if (writer.RowCountWritten == 0) writer.SetColumns(reader.Columns);

                for (int i = 0; i < reader.CurrentRowColumns; ++i)
                {
                    writer.Write(reader.Current(i).ToString8());
                }

                writer.NextRow();
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Arriba.Model.Column;
using Arriba.Model.Security;
using Arriba.Serialization;
using Arriba.Serialization.Csv;
using Arriba.Structures;

namespace Arriba.TfsWorkItemCrawler.ItemConsumers
{
    public class CsvWriterItemConsumer : IItemConsumer
    {
        internal const string CsvFileNameDateTimeFormatString = "yyyyMMdd hhmmss";
        internal const string CsvPathForTable = @"Tables\{0}\CSV";
        internal const long CsvSizeLimitBytes = 256 * 1024 * 1024; // 256MB

        private string TableName { get; set; }
        private string ChangedDateColumn { get; set; }
        private IEnumerable<string> ColumnNames { get; set; }
        private CsvWriter Writer { get; set; }

        public CsvWriterItemConsumer(string tableName, string changedDateColumn)
        {
            this.TableName = tableName;
            this.ChangedDateColumn = changedDateColumn;
        }

        public void CreateTable(IList<ColumnDetails> columns, SecurityPermissions permissions)
        { }

        public void Append(DataBlock items)
        {
            for (int row = 0; row < items.RowCount; ++row)
            {
                // If there's no CSV yet or it's full, create a new one with the ChangedDate of the next item as the name
                if (this.Writer == null || this.Writer.BytesWritten > CsvSizeLimitBytes)
                {
                    if (this.Writer != null) this.Writer.Dispose();
                    this.Writer = BuildWriter(GetItemChangedDate(items, row), items.Columns.Select((cd) => cd.Name));
                }

                // Append these values
                for (int col = 0; col < items.ColumnCount; ++col)
                {
                    this.Writer.AppendValue(items[row, col]);
                }

                this.Writer.AppendRowSeparator();
            }

            // Flush after each batch to minimize the chance of an interrupted write
            // [CSV writes are a tiny part of crawler runtime; most times it would be killed while getting the next batch of items]
            this.Writer.Flush();
        }

        private DateTime GetItemChangedDate(DataBlock block, int row)
        {
            int indexOfColumn = block.IndexOfColumn(ChangedDateColumn);
            if (indexOfColumn == -1) throw new ArgumentException(String.Format("DataBlock passed does not have expected Changed Date column '{0}'. Columns found: {1}", ChangedDateColumn, String.Join(", ", block.Columns.Select((cd) => cd.Name))));

            Value changedDateValue = Value.Create(block[row, indexOfColumn]);
            DateTime changedDate;
            if (!changedDateValue.TryConvert<DateTime>(out changedDate))
            {
                throw new ArgumentException(String.Format("Unable to convert DataBlock row {0:n0} column '{1}' value '{2}' into DateTime.", row, ChangedDateColumn, changedDateValue));
            }

            return changedDate;
        }

        public void Save()
        {
            this.Writer.Flush();
        }

        private CsvWriter BuildWriter(DateTime changedDate, IEnumerable<string> columnNames)
        {
            changedDate = changedDate.ToUniversalTime();

            string csvFolder = Path.Combine(BinarySerializable.CachePath, String.Format(CsvPathForTable, this.TableName));
            string csvFilePathWithoutExtension = Path.Combine(csvFolder, changedDate.ToString(CsvFileNameDateTimeFormatString));
            string csvFilePath = csvFilePathWithoutExtension + ".csv";

            Directory.CreateDirectory(csvFolder);

            // Open a CSV for the current date. Fallback to alternate names if columns have changed and try again
            Exception lastException = null;
            for (int attempt = 1; attempt <= 10; ++attempt)
            {
                try
                {
                    return new CsvWriter(new SerializationContext(new FileStream(csvFilePath, FileMode.OpenOrCreate)), columnNames);
                }
                catch (IOException ex)
                {
                    // If CSV is invalid or columns have changed, create a separate one
                    lastException = ex;
                    csvFilePath = String.Format("{0}.{1}.csv", csvFilePathWithoutExtension, attempt);
                }
            }

            throw lastException;
        }

        public void Dispose()
        {
            if (this.Writer != null)
            {
                this.Writer.Dispose();
                this.Writer = null;
            }
        }
    }
}

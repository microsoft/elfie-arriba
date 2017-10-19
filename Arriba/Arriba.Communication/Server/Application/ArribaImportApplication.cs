// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Model.Column;
using Arriba.Monitoring;
using Arriba.Serialization.Csv;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using Arriba.Structures;
using Arriba.Model;

namespace Arriba.Server.Application
{
    [Export(typeof(IRoutedApplication))]
    internal class ArribaImportApplication : ArribaApplication
    {
        private const int BatchSize = 100;

        [ImportingConstructor]
        public ArribaImportApplication(DatabaseFactory f, ClaimsAuthenticationService auth)
            : base(f, auth)
        {
            // POST /table/foo?type=csv -- Import CSV data 
            this.Post(new RouteSpecification("/table/:tableName", new UrlParameter("type", "csv")), this.ValidateWriteAccess, this.CsvAppend);

            // POST /table/foo?type=block -- Import as DataBlock format
            this.PostAsync(new RouteSpecification("/table/:tableName", new UrlParameter("type", "block")), this.ValidateWriteAccessAsync, this.DataBlockAppendAsync);

            // POST /table/foo?type=json -- Post many objects
            this.PostAsync(new RouteSpecification("/table/:tableName", new UrlParameter("type", "json")), this.ValidateWriteAccessAsync, this.JSONArrayAppendAsync);

            // POST /sample?type=csv -- Import CSV data 
            this.Post(new RouteSpecification("/sample", new UrlParameter("type", "csv")), this.CsvSample);
        }

        private class SampleResult
        {
            public ICollection<ColumnDetails> Columns;
            public int RowCount;
        }

        private IResponse CsvSample(IRequestContext ctx, Route route)
        {
            if (!ctx.Request.HasBody)
            {
                return ArribaResponse.BadRequest("Empty request body");
            }

            SampleResult result = new SampleResult();

            var config = new CsvReaderSettings() { DisposeStream = true, HasHeaders = true };
            using (CsvReader reader = new CsvReader(ctx.Request.InputStream, config))
            {
                // Read the CSV fragment into a DataBlock
                DataBlock block = reader.ReadAsDataBlockBatch(10000, true).FirstOrDefault();

                if (block == null) return ArribaResponse.BadRequest("No result content found.");

                // Count the rows actually returned
                result.RowCount = block.RowCount + 1;

                // Insert only the first 100 rows and not the last (partial) row
                block.SetRowCount(Math.Min(block.RowCount - 1, 100));

                // Build a table with the sample
                Table sample = new Table("Sample", 100);
                sample.AddOrUpdate(block, new AddOrUpdateOptions() { AddMissingColumns = true });

                // Return the created columns in the order they appeared in the CSV
                result.Columns = sample.ColumnDetails.OrderBy((cd) => block.IndexOfColumn(cd.Name)).ToList();
                
                // Return the columns and row count from the sample
                return ArribaResponse.Ok(result);
            }
        }

        private IResponse CsvAppend(IRequestContext ctx, Route route)
        {
            if (!ctx.Request.HasBody)
            {
                return ArribaResponse.BadRequest("Empty request body");
            }

            var tableName = GetAndValidateTableName(route);
            var table = this.Database[tableName];

            if (table == null)
            {
                return ArribaResponse.BadRequest("Table {0} is not loaded or does not exist", tableName);
            }

            var response = new ImportResponse();
            response.TableName = tableName;

            var config = new CsvReaderSettings() { DisposeStream = true, HasHeaders = true };

            using (ctx.Monitor(MonitorEventLevel.Information, "Import.Csv", type: "Table", identity: tableName))
            {
                using (CsvReader reader = new CsvReader(ctx.Request.InputStream, config))
                {
                    response.Columns = reader.ColumnNames;

                    foreach (var blockBatch in reader.ReadAsDataBlockBatch(BatchSize))
                    {
                        response.RowCount += blockBatch.RowCount;
                        table.AddOrUpdate(blockBatch, new AddOrUpdateOptions() { AddMissingColumns = true });
                    }
                }
            }

            return ArribaResponse.Ok(response);
        }

        private async Task<IResponse> DataBlockAppendAsync(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            var table = this.Database[tableName];

            if (table == null)
            {
                return ArribaResponse.BadRequest("Table {0} is not loaded or does not exist", tableName);
            }

            using (ctx.Monitor(MonitorEventLevel.Information, "Import.DataBlock", type: "Table", identity: tableName))
            {
                DataBlock block = await ctx.Request.ReadBodyAsync<DataBlock>();
                table.AddOrUpdate(block, new AddOrUpdateOptions() { AddMissingColumns = true });

                ImportResponse response = new ImportResponse();
                response.TableName = tableName;
                response.Columns = block.Columns.Select((cd) => cd.Name).ToArray();
                response.RowCount = block.RowCount;
                return ArribaResponse.Ok(response);
            }
        }

        private async Task<IResponse> JSONArrayAppendAsync(IRequestContext ctx, Route route)
        {
            var content = ctx.Request.Headers["Content-Type"];

            if (String.IsNullOrEmpty(content) || !String.Equals(content, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                return ArribaResponse.BadRequest("Content-Type of {0} was not expected", content);
            }
            else if (!ctx.Request.HasBody)
            {
                return ArribaResponse.BadRequest("Empty request body");
            }

            var tableName = GetAndValidateTableName(route);
            var table = this.Database[tableName];

            if (table == null)
            {
                return ArribaResponse.BadRequest("Table {0} is not loaded or does not exist", tableName);
            }

            var rows = await ctx.Request.ReadBodyAsync<List<Dictionary<string, object>>>();

            var detail = new
            {
                RequestSize = ctx.Request.Headers["Content-Length"],
                RowCount = rows.Count
            };

            using (ctx.Monitor(MonitorEventLevel.Information, "Import.JsonObjectArray", type: "Table", identity: tableName, detail: detail))
            {
                // Read column names from JSON
                var columnDetails = new Dictionary<string, ColumnDetails>();
                foreach (var row in rows)
                {
                    foreach (var property in row)
                    {
                        if (property.Value != null && !columnDetails.ContainsKey(property.Key))
                        {
                            var colDetail = new ColumnDetails(property.Key);
                            columnDetails.Add(property.Key, colDetail);
                        }
                    }
                }

                var columns = columnDetails.Values.ToArray();

                // Insert the data in batches 
                var block = new DataBlock(columns, BatchSize);
                for (int batchOffset = 0; batchOffset < rows.Count; batchOffset += BatchSize)
                {
                    int rowsLeft = rows.Count - batchOffset;
                    int rowsInBatch = BatchSize;

                    if (rowsLeft < BatchSize)
                    {
                        block = new DataBlock(columns, rowsLeft);
                        rowsInBatch = rowsLeft;
                    }

                    for (int blockRowIndex = 0; blockRowIndex < rowsInBatch; ++blockRowIndex)
                    {
                        int sourceRowIndex = blockRowIndex + batchOffset;

                        for (int columnIndex = 0; columnIndex < columns.Length; columnIndex++)
                        {
                            object value = null;

                            if (rows[sourceRowIndex].TryGetValue(columns[columnIndex].Name, out value))
                            {
                                block[blockRowIndex, columnIndex] = value;
                            }
                        }
                    }

                    table.AddOrUpdate(block, new AddOrUpdateOptions() { AddMissingColumns = true });
                }

                using (ctx.Monitor(MonitorEventLevel.Verbose, "table.save"))
                {
                    table.Save();
                }

                return ArribaResponse.Created(new ImportResponse
                {
                    TableName = table.Name,
                    RowCount = rows.Count(),
                    Columns = columns.Select((cd) => cd.Name).ToArray()
                });
            }
        }

        private class ImportResponse
        {
            public string TableName;
            public int RowCount;
            public string[] Columns;
        }
    }
}

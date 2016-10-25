// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Model;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Monitoring;
using Arriba.Serialization;
using Arriba.Serialization.Csv;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using System.IO;
using Arriba.Structures;

namespace Arriba.Server
{
    /// <summary>
    /// Arriba restful application for query operations.
    /// </summary>
    [Export(typeof(IRoutedApplication))]
    internal class ArribaQueryApplication : ArribaApplication
    {
        private const string DefaultFormat = "dictionary";

        [ImportingConstructor]
        public ArribaQueryApplication(DatabaseFactory f, ClaimsAuthenticationService auth)
            : base(f, auth)
        {
            // /table/foo?type=select
            this.GetAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "select")), this.ValidateReadAccessAsync, this.Select);
            this.PostAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "select")), this.ValidateReadAccessAsync, this.Select);

            // /table/foo?type=distinct
            this.GetAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "distinct")), this.ValidateReadAccessAsync, this.Distinct);
            this.PostAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "distinct")), this.ValidateReadAccessAsync, this.Distinct);

            // /table/foo?type=aggregate
            this.GetAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "aggregate")), this.ValidateReadAccessAsync, this.Aggregate);
            this.PostAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "aggregate")), this.ValidateReadAccessAsync, this.Aggregate);

            this.GetAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "pivot")), this.ValidateReadAccessAsync, this.Pivot);
        }

        private async Task<IResponse> Select(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to select from.");
            }

            string outputFormat = ctx.Request.ResourceParameters["fmt"];

            SelectQuery query = new SelectQuery();

            if (ctx.Request.Method == RequestVerb.Post && ctx.Request.HasBody)
            {
                query = await ctx.Request.ReadBodyAsync<SelectQuery>();
            }
            else
            {
                query = SelectFromUrlParameters(ctx);
            }

            query.TableName = tableName;

            Table table = this.Database[tableName];
            SelectResult result = null;

            // If this is RSS, just get the ID column
            if(String.Equals(outputFormat, "rss", StringComparison.OrdinalIgnoreCase))
            {
                query.Columns = new string[] { table.IDColumn.Name };
            }

            using (ctx.Monitor(MonitorEventLevel.Verbose, "Correct", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run server correctors
                query.Correct(this.CurrentCorrectors(ctx));
            }

            using (ctx.Monitor(MonitorEventLevel.Information, "Select", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run the query
                result = table.Select(query);
            }

            // Is this a CSV request? 
            if (String.Equals(outputFormat, "csv", StringComparison.OrdinalIgnoreCase))
            {
                // Do we want to include headers? 
                bool includeHeaders = !String.Equals(ctx.Request.ResourceParameters["h"], "false", StringComparison.OrdinalIgnoreCase);

                // Generate a filename of {TableName}-{Ticks}.csv
                var fileName = String.Format("{0}-{1:yyyyMMdd}.csv", tableName, DateTime.Now);

                // Stream datablock to CSV result
                return ToCsvResponse(result, fileName);
            }
            else if(String.Equals(outputFormat, "rss", StringComparison.OrdinalIgnoreCase))
            {
                return ToRssResponse(result, "", query.TableName + ": " + query.Where, ctx.Request.ResourceParameters["iURL"]);
            }

            // Regular, serialize result object 
            return ArribaResponse.Ok(result);
        }

        private static SelectQuery SelectFromUrlParameters(IRequestContext ctx)
        {
            SelectQuery query = new SelectQuery();

            query.Where = SelectQuery.ParseWhere(ctx.Request.ResourceParameters["q"]);
            query.OrderByColumn = ctx.Request.ResourceParameters["ob"];

            string columns = ctx.Request.ResourceParameters["cols"];
            if (!String.IsNullOrEmpty(columns))
            {
                query.Columns = columns.Split(',');
            }

            string take = ctx.Request.ResourceParameters["t"];
            if (!String.IsNullOrEmpty(take))
            {
                query.Count = UInt16.Parse(take);
            }

            string skip = ctx.Request.ResourceParameters["s"];
            if (!String.IsNullOrEmpty(skip))
            {
                query.Skip = UInt32.Parse(skip);
            }

            string sortOrder = ctx.Request.ResourceParameters["so"];
            if (!String.IsNullOrEmpty(sortOrder))
            {
                query.OrderByDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
            }

            string highlightString = ctx.Request.ResourceParameters["h"];
            string highlightStringEnd = ctx.Request.ResourceParameters["h2"];

            if (!String.IsNullOrEmpty(highlightString))
            {
                // Set the end highlight string to the start highlight string if it is not set. 
                if (String.IsNullOrEmpty(highlightStringEnd))
                {
                    highlightStringEnd = highlightString;
                }

                query.Highlighter = new Highlighter(highlightString, highlightStringEnd);
            }

            return query;
        }

        private static IResponse ToCsvResponse(SelectResult result, string fileName)
        {
            const string outputMimeType = "text/csv; encoding=utf-8";

            var resp = new StreamWriterResponse(outputMimeType, async (s) =>
            {
                SerializationContext context = new SerializationContext(s);
                var items = result.Values;

                // ***Crazy Excel Business***
                // This is pretty ugly. If the first 2 chars in a CSV file as ID, then excel is  thinks the file is a SYLK 
                // file not a CSV File (!) and will alert the user. Excel does not care about output mime types. 
                // 
                // To work around this, and have a _nice_ experience for csv export, we'll modify 
                // the first column name to " ID" to trick Excel. It's not perfect, but it'll do.
                // 
                // As a mitigation for round-tripping, the CsvReader will trim column names. Sigh. 
                IList<string> columns = result.Query.Columns;

                if (String.Equals(columns[0], "ID", StringComparison.OrdinalIgnoreCase))
                {
                    columns = new List<string>(columns);
                    columns[0] = " ID";
                }

                CsvWriter writer = new CsvWriter(context, columns);

                for (int row = 0; row < items.RowCount; ++row)
                {
                    for (int col = 0; col < items.ColumnCount; ++col)
                    {
                        writer.AppendValue(items[row, col]);
                    }

                    writer.AppendRowSeparator();
                }

                context.Writer.Flush();
                await s.FlushAsync();
            });

            resp.AddHeader("Content-Disposition", String.Concat("attachment;filename=\"", fileName, "\";"));

            return resp;
        }

        private static IResponse ToRssResponse(SelectResult result, string rssUrl, string query, string itemUrlWithoutId)
        {
            DateTime utcNow = DateTime.UtcNow;

            const string outputMimeType = "application/rss+xml; encoding=utf-8";

            var resp = new StreamWriterResponse(outputMimeType, async (s) =>
            {
                SerializationContext context = new SerializationContext(s);
                RssWriter w = new RssWriter(context);

                ByteBlock queryBB = (ByteBlock)query;
                w.WriteRssHeader(queryBB, queryBB, rssUrl, utcNow, TimeSpan.FromHours(1));

                ByteBlock baseLink = itemUrlWithoutId;
                var items = result.Values;
                for (int row = 0; row < items.RowCount; ++row)
                {
                    ByteBlock id = ConvertToByteBlock(items[row, 0]);
                    w.WriteItem(id, id, id, baseLink, utcNow);
                }

                w.WriteRssFooter();

                context.Writer.Flush();
                await s.FlushAsync();
            });

            return resp;
        }

        private static ByteBlock ConvertToByteBlock(object value)
        {
            if (value == null) return ByteBlock.Zero;

            if (value is ByteBlock)
            {
                return (ByteBlock)value;
            }
            else if (value is string)
            {
                return (ByteBlock)value;
            }
            else
            {
                return (ByteBlock)(value.ToString());
            }
        }

        private async Task<IResponse> Query<T, U>(IRequestContext ctx, Route route) where T : IQuery<U>, new()
        {
            // TODO: Roll Aggregate and Distinct to use this. Need to make a nice pattern for reading from URL parameters first.
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to query.");
            }

            IQuery<U> query = new T();

            if (ctx.Request.Method == RequestVerb.Post && ctx.Request.HasBody)
            {
                query = await ctx.Request.ReadBodyAsync<T>();
            }

            query.TableName = tableName;

            using (ctx.Monitor(MonitorEventLevel.Verbose, "Correct", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run server correctors
                query.Correct(this.CurrentCorrectors(ctx));
            }

            using (ctx.Monitor(MonitorEventLevel.Information, "Query", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                Table t = this.Database[tableName];

                // Run the query
                U result = t.Query(query);
                return ArribaResponse.Ok(result);
            }
        }

        private async Task<IResponse> Aggregate(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to aggregate.");
            }

            AggregationQuery query = new AggregationQuery();

            if (ctx.Request.Method == RequestVerb.Post && ctx.Request.HasBody)
            {
                query = await ctx.Request.ReadBodyAsync<AggregationQuery>();
            }
            else
            {
                query = AggregateFromUrlParameters(ctx);
            }

            query.TableName = tableName;

            using (ctx.Monitor(MonitorEventLevel.Verbose, "Correct", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run server correctors
                query.Correct(this.CurrentCorrectors(ctx));
            }

            using (ctx.Monitor(MonitorEventLevel.Information, "Aggregate", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run the query
                AggregationResult result = this.Database[tableName].Query(query);
                return ArribaResponse.Ok(result);
            }
        }

        private static AggregationQuery AggregateFromUrlParameters(IRequestContext ctx)
        {
            string queryString = ctx.Request.ResourceParameters["q"];
            string aggregationFunction = ctx.Request.ResourceParameters["a"];
            string columnName = ctx.Request.ResourceParameters["col"];

            List<string> dimensions = new List<string>();
            int i = 1;
            while (ctx.Request.ResourceParameters.Contains("d" + i))
            {
                dimensions.Add(ctx.Request.ResourceParameters["d" + i]);
                ++i;
            }

            IExpression whereExpression = null;

            using (ctx.Monitor(MonitorEventLevel.Verbose, "Arriba.ParseQuery", String.IsNullOrEmpty(queryString) ? "<none>" : queryString))
            {
                whereExpression = String.IsNullOrEmpty(queryString) ? new AllExpression() : SelectQuery.ParseWhere(queryString);
            }

            AggregationQuery query = new AggregationQuery();
            query.Aggregator = AggregationQuery.BuildAggregator(aggregationFunction);
            query.AggregationColumns = new string[] { columnName };
            query.Where = whereExpression;

            foreach (string dimension in dimensions)
            {
                query.Dimensions.Add(new AggregationDimension("", dimension.Split(',')));
            }

            return query;
        }

        private async Task<IResponse> Distinct(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to query.");
            }

            DistinctQuery query = new DistinctQuery();

            if (ctx.Request.Method == RequestVerb.Post && ctx.Request.HasBody)
            {
                query = await ctx.Request.ReadBodyAsync<DistinctQuery>();
            }
            else
            {
                query = DistinctFromUrlParameters(ctx);
            }

            using (ctx.Monitor(MonitorEventLevel.Verbose, "Correct", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run server correctors
                query.Correct(this.CurrentCorrectors(ctx));
            }

            using (ctx.Monitor(MonitorEventLevel.Information, "Distinct", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run the query
                DistinctResult result = this.Database[tableName].Query(query);
                return ArribaResponse.Ok(result);
            }
        }

        private static DistinctQuery DistinctFromUrlParameters(IRequestContext ctx)
        {
            DistinctQuery query = new DistinctQuery();
            query.Column = ctx.Request.ResourceParameters["col"];

            string queryString = ctx.Request.ResourceParameters["q"];
            using (ctx.Monitor(MonitorEventLevel.Verbose, "Arriba.ParseQuery", String.IsNullOrEmpty(queryString) ? "<none>" : queryString))
            {
                query.Where = String.IsNullOrEmpty(queryString) ? new AllExpression() : SelectQuery.ParseWhere(queryString);
            }

            string take = ctx.Request.ResourceParameters["t"];
            if (!String.IsNullOrEmpty(take))
            {
                query.Count = UInt16.Parse(take);
            }

            return query;
        }

        private async Task<IResponse> Pivot(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to query.");
            }
            var table = this.Database[tableName];

            PivotQuery query = null;

            if (ctx.Request.Method == RequestVerb.Post && ctx.Request.HasBody)
            {
                query = await ctx.Request.ReadBodyAsync<PivotQuery>();
            }
            else
            {
                query = PivotQueryFromUrlParameters(ctx);
            }

            if (query.AggregationColumns == null || query.AggregationColumns.Length == 0)
            {
                // Fall back to PK
                query.AggregationColumns = new string[] { table.IDColumn.Name };
            }

            using (ctx.Monitor(MonitorEventLevel.Information, "Pivot", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                AggregationResult result = this.Database[tableName].Query(query);
                return ArribaResponse.Ok(result);
            }
        }

        private static PivotQuery PivotQueryFromUrlParameters(IRequestContext ctx)
        {
            var query = new PivotQuery
            {
                Where = SelectQuery.ParseWhere(ctx.Request.ResourceParameters["q"] ?? ""),
                Aggregator = AggregationQuery.BuildAggregator(ctx.Request.ResourceParameters["a"] ?? "COUNT"),
            };

            if (!String.IsNullOrEmpty(ctx.Request.ResourceParameters["col"]))
            {
                query.AggregationColumns = new string[] { ctx.Request.ResourceParameters["col"] };
            }


            int i = 1;
            while (ctx.Request.ResourceParameters.Contains("d" + i))
            {
                var dimensionUnparsed = ctx.Request.ResourceParameters["d" + i];
                ++i;

                PivotDimensionDescriptor descriptor;
                if (PivotDimensionDescriptor.TryParse(dimensionUnparsed, out descriptor))
                {
                    var dimension = GetPivotDimension(descriptor);

                    if (dimension != null)
                    {
                        query.Dimensions.Add(dimension);
                        continue;
                    }
                }

                // Parsing failed assume query.
                query.Dimensions.Add(new AggregationDimension("", dimensionUnparsed.Split(',')));
            }

            return query;
        }


        private static PivotDimension GetPivotDimension(PivotDimensionDescriptor pivot)
        {
            switch (pivot.Type)
            {
                case "col":
                    var distinct = new DistinctValuePivotDimension(pivot.Column);
                    distinct.MaximumValues = pivot.GetNullableArgument<ushort?>(0);
                    return distinct;

                case "date":
                    var dateHistogram = new DateHistogramPivotDimension(pivot.Column);
                    dateHistogram.From = pivot.GetNullableArgument<DateTime?>(0);
                    dateHistogram.To = pivot.GetNullableArgument<DateTime?>(1);
                    dateHistogram.Interval = pivot.GetNullableArgument<DateHistogramInterval?>(2);

                    if (dateHistogram.To == null)
                    {
                        dateHistogram.From = null;
                        dateHistogram.Interval = pivot.GetNullableArgument<DateHistogramInterval?>(0);
                    }

                    return dateHistogram;
                default:
                    return null;
            }
        }

        private class PivotDimensionDescriptor
        {
            private static readonly char[] s_columnEscapeChars = new[] { '[', ']' };
            public string Source { get; private set; }
            public string Type { get; private set; }
            public string Column { get; private set; }
            public string[] Arguments { get; private set; }

            public static bool TryParse(string raw, out PivotDimensionDescriptor dimension)
            {
                var parts = raw.Split(new[] { ';' });

                if (parts.Length < 2)
                {
                    dimension = null;
                    return false;
                }

                dimension = new PivotDimensionDescriptor();
                dimension.Source = raw;
                dimension.Type = parts[0].ToLowerInvariant();
                dimension.Column = parts[1].Trim(s_columnEscapeChars);

                if (parts.Length > 2)
                {
                    dimension.Arguments = parts.Skip(2).ToArray();
                }
                else
                {
                    dimension.Arguments = new string[0];
                }

                return true;
            }

            public T GetNullableArgument<T>(int index)
            {
                if (index >= this.Arguments.Length)
                {
                    return default(T);
                }

                var converter = new NullableConverter(typeof(T));

                try
                {
                    return (T)converter.ConvertFromInvariantString(this.Arguments[index]);
                }
                catch
                {
                    return default(T);
                }
            }

            public override string ToString()
            {
                if (this.Arguments.Length == 0)
                {
                    return String.Format("[{0} for {1}]", this.Type, this.Column);
                }

                return String.Format("[{0} for {1} with ({2})]", this.Type, this.Column, String.Join(", ", this.Arguments));
            }
        }
    }
}

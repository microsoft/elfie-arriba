﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Composition;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Model;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Monitoring;
using Arriba.Serialization;
using Arriba.Serialization.Csv;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using Arriba.Structures;
using Arriba.Model.Correctors;
using Arriba.Model.Column;

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

            this.Get(new RouteSpecification("/allCount"), this.AllCount);
        }

        private async Task<IResponse> Select(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to select from.");
            }

            string outputFormat = ctx.Request.ResourceParameters["fmt"];

            SelectQuery query = await SelectQueryFromRequest(this.Database, ctx);
            query.TableName = tableName;

            Table table = this.Database[tableName];
            SelectResult result = null;

            // If this is RSS, just get the ID column
            if(String.Equals(outputFormat, "rss", StringComparison.OrdinalIgnoreCase))
            {
                query.Columns = new string[] { table.IDColumn.Name };
            }

            // Read Joins, if passed
            IQuery<SelectResult> wrappedQuery = WrapInJoinQueryIfFound(query, this.Database, ctx);

            ICorrector correctors = this.CurrentCorrectors(ctx);
            using (ctx.Monitor(MonitorEventLevel.Verbose, "Correct", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run server correctors
                wrappedQuery.Correct(correctors);
            }

            using (ctx.Monitor(MonitorEventLevel.Information, "Select", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run the query
                result = this.Database.Query(wrappedQuery, (si) => this.IsInIdentity(ctx.Request.User, si));
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

        private async static Task<SelectQuery> SelectQueryFromRequest(Database db, IRequestContext ctx)
        {
            SelectQuery query = new SelectQuery();

            // Post with body - only SelectQuery supported
            if (ctx.Request.Method == RequestVerb.Post && ctx.Request.HasBody)
            {
                return await ctx.Request.ReadBodyAsync<SelectQuery>();
            }

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

        private static IQuery<T> WrapInJoinQueryIfFound<T>(IQuery<T> primaryQuery, Database db, IRequestContext ctx)
        {
            List<SelectQuery> joins = new List<SelectQuery>();

            for(int queryIndex = 1; ctx.Request.ResourceParameters.Contains("q" + queryIndex.ToString()); ++queryIndex)
            {
                string where = ctx.Request.ResourceParameters["q" + queryIndex.ToString()];
                string table = ctx.Request.ResourceParameters["t" + queryIndex.ToString()];
                joins.Add(new SelectQuery() { Where = SelectQuery.ParseWhere(where), TableName = table });
            }

            if(joins.Count == 0)
            {
                return primaryQuery;
            }
            else
            {
                return new JoinQuery<T>(db, primaryQuery, joins);
            }
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
                List<string> columns = new List<string>();

                foreach(ColumnDetails column in items.Columns)
                {
                    if(columns.Count == 0 && column.Name.Equals("ID", StringComparison.OrdinalIgnoreCase))
                    {
                        columns.Add(" ID");
                    }
                    else
                    {
                        columns.Add(column.Name);
                    }
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

        private IResponse AllCount(IRequestContext ctx, Route route)
        {
            List<CountResult> results = new List<CountResult>();

            // Build a Count query
            IQuery<AggregationResult> query = new AggregationQuery("count", new string[0], ctx.Request.ResourceParameters["q"] ?? "");

            // Wrap in Joins, if found
            query = WrapInJoinQueryIfFound(query, this.Database, ctx);

            // Run server correctors
            using (ctx.Monitor(MonitorEventLevel.Verbose, "Correct", type: "AllCount", detail: query.Where.ToString()))
            {
                query.Correct(this.CurrentCorrectors(ctx));
            }

            // Accumulate Results for each table
            IPrincipal user = ctx.Request.User;
            using (ctx.Monitor(MonitorEventLevel.Information, "AllCount", type: "AllCount", detail: query.Where.ToString()))
            {
                IExpression defaultWhere = query.Where;

                foreach (string tableName in this.Database.TableNames)
                {
                    if (this.HasTableAccess(tableName, user, PermissionScope.Reader))
                    {
                        query.TableName = tableName;
                        query.Where = defaultWhere;

                        AggregationResult tableCount = this.Database.Query(query, (si) => this.IsInIdentity(ctx.Request.User, si));

                        if (!tableCount.Details.Succeeded || tableCount.Values == null)
                        {
                            results.Add(new CountResult(tableName, 0, true, false));
                        }
                        else
                        {
                            results.Add(new CountResult(tableName, (ulong)tableCount.Values[0, 0], true, tableCount.Details.Succeeded));
                        }
                    }
                    else
                    {
                        results.Add(new CountResult(tableName, 0, false, false));
                    }
                }
            }

            return ArribaResponse.Ok(results.OrderByDescending((cr) => cr.Count));
        }

        private class CountResult
        {
            public string TableName { get; set; }
            public ulong Count { get; set; }
            public bool AllowedToRead { get; set; }
            public bool Succeeded { get; set; }

            public CountResult(string tableName, ulong count, bool allowedToRead, bool succeeded)
            {
                this.TableName = tableName;
                this.Count = count;
                this.AllowedToRead = allowedToRead;
                this.Succeeded = succeeded;
            }
        }

        private async Task<IResponse> Query<T>(IRequestContext ctx, Route route, IQuery<T> query)
        {
            IQuery<T> wrappedQuery = WrapInJoinQueryIfFound(query, this.Database, ctx);

            // Ensure the table exists and set it on the query
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to query.");
            }

            query.TableName = tableName;

            // Correct the query with default correctors
            using (ctx.Monitor(MonitorEventLevel.Verbose, "Correct", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                query.Correct(this.CurrentCorrectors(ctx));
            }

            // Execute and return results for the query
            using (ctx.Monitor(MonitorEventLevel.Information, query.GetType().Name, type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                T result = this.Database.Query(wrappedQuery, (si) => this.IsInIdentity(ctx.Request.User, si));
                return ArribaResponse.Ok(result);
            }
        }

        private async Task<IResponse> Aggregate(IRequestContext ctx, Route route)
        {
            IQuery<AggregationResult> query = await BuildAggregateFromContext(ctx);
            return await Query(ctx, route, query);
        }

        private static async Task<AggregationQuery> BuildAggregateFromContext(IRequestContext ctx)
        {
            if (ctx.Request.Method == RequestVerb.Post && ctx.Request.HasBody)
            {
                return await ctx.Request.ReadBodyAsync<AggregationQuery>();
            }

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
            IQuery<DistinctResult> query = await BuildDistinctFromContext(ctx);
            return await Query(ctx, route, query);
        }

        private async static Task<DistinctQuery> BuildDistinctFromContext(IRequestContext ctx)
        {
            if (ctx.Request.Method == RequestVerb.Post && ctx.Request.HasBody)
            {
                return await ctx.Request.ReadBodyAsync<DistinctQuery>();
            }

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
            PivotQuery query = await BuildPivotQueryFromContext(ctx);
            return await Query(ctx, route, query);
        }

        private async static Task<PivotQuery> BuildPivotQueryFromContext(IRequestContext ctx)
        {
            if (ctx.Request.Method == RequestVerb.Post && ctx.Request.HasBody)
            {
                return await ctx.Request.ReadBodyAsync<PivotQuery>();
            }

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

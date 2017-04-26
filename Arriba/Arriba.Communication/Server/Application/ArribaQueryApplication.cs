// Copyright (c) Microsoft. All rights reserved.
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

            this.Get(new RouteSpecification("/allCount"), this.AllCount);
            this.Get(new RouteSpecification("/suggest"), this.Suggest);
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

            // If no columns were requested or this is RSS, get only the ID column
            if(query.Columns == null || query.Columns.Count == 0 || String.Equals(outputFormat, "rss", StringComparison.OrdinalIgnoreCase))
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

            // Format the result in the return format
            if(String.IsNullOrEmpty(outputFormat))
            {
                // No problem - default return format
            }
            else if (String.Equals(outputFormat, "csv", StringComparison.OrdinalIgnoreCase))
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
            else
            {
                throw new ArgumentException($"OutputFormat [fmt] passed, '{outputFormat}', was invalid.");
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
            query.Columns = ReadParameterSet(ctx.Request, "c", "cols");

            string take = ctx.Request.ResourceParameters["t"];
            if (!String.IsNullOrEmpty(take)) query.Count = UInt16.Parse(take);

            string sortOrder = ctx.Request.ResourceParameters["so"];
            if (!String.IsNullOrEmpty(sortOrder))
            {
                query.OrderByDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
                if (!query.OrderByDescending && !sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"SortOrder [so] passed, '{sortOrder}' was not 'asc' or 'desc'.");
            }

            string highlightString = ctx.Request.ResourceParameters["h"];
            string highlightStringEnd = ctx.Request.ResourceParameters["h2"];

            if (!String.IsNullOrEmpty(highlightString))
            {
                // Set the end highlight string to the start highlight string if it is not set. 
                if (String.IsNullOrEmpty(highlightStringEnd)) highlightStringEnd = highlightString;
                query.Highlighter = new Highlighter(highlightString, highlightStringEnd);
            }

            return query;
        }

        /// <summary>
        ///  Read a set of parameters into a List (C1=X&C2=Y&C3=Z) => { "X", "Y", "Z" }.
        /// </summary>
        /// <param name="request">IRequest to read from</param>
        /// <param name="baseName">Parameter name before numbered suffix ('C' -> look for 'C1', 'C2', ...)</param>
        /// <returns>List&lt;string&gt; containing values for the parameter set, if any are found, otherwise an empty list.</returns>
        protected static List<string> ReadParameterSet(IRequest request, string baseName)
        {
            List<string> result = new List<string>();

            int i = 1;
            while(true)
            {
                string value = request.ResourceParameters[baseName + i.ToString()];
                if (String.IsNullOrEmpty(value)) break;

                result.Add(value);
                ++i;
            }

            return result;
        }

        /// <summary>
        ///  Read a set of parameters into a List, allowing a single comma-delimited fallback value.
        ///  (C1=X&C2=Y&C3=Z or Cols=X,Y,Z) => { "X", "Y", "Z" }
        /// </summary>
        /// <param name="request">IRequest to read from</param>
        /// <param name="nameIfSeparate">Parameter name prefix if parameters are passed separately ('C' -> look for 'C1', 'C2', ...)</param>
        /// <param name="nameIfDelimited">Parameter name if parameters are passed together comma delimited ('Cols')</param>
        /// <returns>List&lt;string&gt; containing values for the parameter set, if any are found, otherwise an empty list.</returns>
        protected static List<string> ReadParameterSet(IRequest request, string nameIfSeparate, string nameIfDelimited)
        {
            List<string> result = ReadParameterSet(request, nameIfSeparate);

            if (result.Count == 0)
            {
                string delimitedValue = request.ResourceParameters[nameIfDelimited];
                if(!String.IsNullOrEmpty(delimitedValue))
                {
                    result = new List<string>(delimitedValue.Split(','));
                }
            }

            return result;
        }

        private static IQuery<T> WrapInJoinQueryIfFound<T>(IQuery<T> primaryQuery, Database db, IRequestContext ctx)
        {
            List<SelectQuery> joins = new List<SelectQuery>();

            List<string> joinQueries = ReadParameterSet(ctx.Request, "q");
            List<string> joinTables = ReadParameterSet(ctx.Request, "t");

            for(int queryIndex = 0; queryIndex < Math.Min(joinQueries.Count, joinTables.Count); ++queryIndex)
            {
                joins.Add(new SelectQuery() { Where = SelectQuery.ParseWhere(joinQueries[queryIndex]), TableName = joinTables[queryIndex] });
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
            IQuery<AggregationResult> query = new AggregationQuery("count", null, ctx.Request.ResourceParameters["q"] ?? "");

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

            // Sort results so that succeeding tables are first and are subsorted by count [descending]
            results.Sort((left, right) =>
            {
                int result = right.Succeeded.CompareTo(left.Succeeded);
                if (result != 0) return result;

                return right.Count.CompareTo(left.Count);
            });

            return ArribaResponse.Ok(results);
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

        private IResponse Suggest(IRequestContext ctx, Route route)
        {
            string query = ctx.Request.ResourceParameters["q"];
            string selectedTable = ctx.Request.ResourceParameters["t"];
            IPrincipal user = ctx.Request.User;

            IntelliSenseResult result = null;

            using (ctx.Monitor(MonitorEventLevel.Verbose, "Suggest", type: "Suggest", detail: query))
            {
                // Get all available tables
                List<Table> tables = new List<Table>();
                foreach (string tableName in this.Database.TableNames)
                {
                    if (this.HasTableAccess(tableName, user, PermissionScope.Reader))
                    {
                        if(String.IsNullOrEmpty(selectedTable) || selectedTable.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                        {
                            tables.Add(this.Database[tableName]);
                        }
                    }
                }

                // Get IntelliSense results and return
                QueryIntelliSense qi = new QueryIntelliSense();
                result = qi.GetIntelliSenseItems(query, tables);
            }

            return ArribaResponse.Ok(result);
        }

        private IResponse Query<T>(IRequestContext ctx, Route route, IQuery<T> query)
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
            return Query(ctx, route, query);
        }

        private static async Task<AggregationQuery> BuildAggregateFromContext(IRequestContext ctx)
        {
            if (ctx.Request.Method == RequestVerb.Post && ctx.Request.HasBody)
            {
                return await ctx.Request.ReadBodyAsync<AggregationQuery>();
            }

            string aggregationFunction = ctx.Request.ResourceParameters["a"] ?? "count";
            string columnName = ctx.Request.ResourceParameters["col"];
            string queryString = ctx.Request.ResourceParameters["q"];

            AggregationQuery query = new AggregationQuery();
            query.Aggregator = AggregationQuery.BuildAggregator(aggregationFunction);
            query.AggregationColumns = String.IsNullOrEmpty(columnName) ? null : new string[] { columnName };

            using (ctx.Monitor(MonitorEventLevel.Verbose, "Arriba.ParseQuery", String.IsNullOrEmpty(queryString) ? "<none>" : queryString))
            {
                query.Where = String.IsNullOrEmpty(queryString) ? new AllExpression() : SelectQuery.ParseWhere(queryString);
            }

            for(char dimensionPrefix = 'd'; ctx.Request.ResourceParameters.Contains(dimensionPrefix.ToString() + "1"); ++dimensionPrefix)
            {
                List<string> dimensionParts = ReadParameterSet(ctx.Request, dimensionPrefix.ToString());

                if (dimensionParts.Count == 1 && dimensionParts[0].EndsWith(">"))
                {
                    query.Dimensions.Add(new DistinctValueDimension(dimensionParts[0].TrimEnd('>')));
                }
                else
                {
                    query.Dimensions.Add(new AggregationDimension("", dimensionParts));
                }
            }

            return query;
        }

        private async Task<IResponse> Distinct(IRequestContext ctx, Route route)
        {
            IQuery<DistinctResult> query = await BuildDistinctFromContext(ctx);
            return Query(ctx, route, query);
        }

        private async static Task<DistinctQuery> BuildDistinctFromContext(IRequestContext ctx)
        {
            if (ctx.Request.Method == RequestVerb.Post && ctx.Request.HasBody)
            {
                return await ctx.Request.ReadBodyAsync<DistinctQuery>();
            }

            DistinctQuery query = new DistinctQuery();
            query.Column = ctx.Request.ResourceParameters["col"];
            if (String.IsNullOrEmpty(query.Column)) throw new ArgumentException("Distinct Column [col] must be passed.");

            string queryString = ctx.Request.ResourceParameters["q"];
            using (ctx.Monitor(MonitorEventLevel.Verbose, "Arriba.ParseQuery", String.IsNullOrEmpty(queryString) ? "<none>" : queryString))
            {
                query.Where = String.IsNullOrEmpty(queryString) ? new AllExpression() : SelectQuery.ParseWhere(queryString);
            }

            string take = ctx.Request.ResourceParameters["t"];
            if (!String.IsNullOrEmpty(take)) query.Count = UInt16.Parse(take);

            return query;
        }
    }
}

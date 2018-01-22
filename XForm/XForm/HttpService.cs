// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;

namespace XForm
{
    public class HttpService
    {
        private XDatabaseContext _xDatabaseContext;
        private QuerySuggester _suggester;
        private static String8 s_delimiter = String8.Convert(";", new byte[1]);

        public HttpService(XDatabaseContext xDatabaseContext)
        {
            _xDatabaseContext = xDatabaseContext;
            _suggester = new QuerySuggester(_xDatabaseContext);
        }

        public void Run()
        {
            using (BackgroundWebServer server = new BackgroundWebServer("index.html", "Web"))
            {
                server.AddResponder("suggest", Suggest);
                server.AddResponder("run", Run);
                server.AddResponder("download", Download);
                server.AddResponder("count", CountWithinTimeout);
                server.AddResponder("save", Save);

                server.Start();
                Console.WriteLine("Http Server running; browse http://localhost:5073. Press enter to stop server.");
                Console.ReadLine();
                server.Stop();
            }
        }

        private void Suggest(HttpListenerContext context, HttpListenerResponse response)
        {
            using (ITabularWriter writer = WriterForFormat("json", response))
            {
                try
                {
                    string query = Require(context, "q");

                    DateTime asOfDate = ParseOrDefault(context.Request.QueryString["asof"], _xDatabaseContext.RequestedAsOfDateTime);
                    SuggestResult result = _suggester.Suggest(query);

                    // If the query is valid and there are no extra values valid next, just return valid
                    if (result.IsValid == true && result.Context == null)
                    {
                        writer.SetColumns(new string[] { "Valid" });
                        writer.Write(true);
                        writer.NextRow();
                    }
                    else
                    {
                        WriteException(result, writer);
                    }
                }
                catch (Exception ex)
                {
                    WriteException(ex, writer, false);
                }
            }
        }

        private void Run(HttpListenerContext context, HttpListenerResponse response)
        {
            try
            {
                Run(
                    Require(context, "q"),
                    context.Request.QueryString["fmt"] ?? "json",
                    ParseOrDefault(context.Request.QueryString["rowLimit"], 100),
                    ParseOrDefault(context.Request.QueryString["colLimit"], -1),
                    ParseOrDefault(context.Request.QueryString["asof"], _xDatabaseContext.RequestedAsOfDateTime),
                    response);
            }
            catch (Exception ex)
            {
                using (ITabularWriter writer = WriterForFormat("json", response))
                {
                    WriteException(ex, writer);
                }
            }
        }

        private void Download(HttpListenerContext context, HttpListenerResponse response)
        {
            try
            {
                Run(
                    Require(context, "q"),
                    Require(context, "fmt"),
                    ParseOrDefault(context.Request.QueryString["rowLimit"], -1),
                    ParseOrDefault(context.Request.QueryString["colLimit"], -1),
                    ParseOrDefault(context.Request.QueryString["asof"], _xDatabaseContext.RequestedAsOfDateTime),
                    response);
            }
            catch (Exception ex)
            {
                using (ITabularWriter writer = WriterForFormat("json", response))
                {
                    WriteException(ex, writer);
                }
            }
        }

        private void CountWithinTimeout(HttpListenerContext context, HttpListenerResponse response)
        {
            try
            {
                CountWithinTimeout(
                    Require(context, "q"),
                    TimeSpan.FromMilliseconds(ParseOrDefault(context.Request.QueryString["ms"], 5000)),
                    ParseOrDefault(context.Request.QueryString["asof"], _xDatabaseContext.RequestedAsOfDateTime),
                    response);
            }
            catch (Exception ex)
            {
                using (ITabularWriter writer = WriterForFormat("json", response))
                {
                    WriteException(ex, writer);
                }
            }
        }

        private void CountWithinTimeout(string query, TimeSpan timeout, DateTime asOfDate, HttpListenerResponse response)
        {
            IXTable pipeline = null;

            try
            {
                XDatabaseContext context = _xDatabaseContext;

                // Build for another moment in time if requested
                if (asOfDate != _xDatabaseContext.RequestedAsOfDateTime)
                {
                    context = new XDatabaseContext(_xDatabaseContext) { RequestedAsOfDateTime = asOfDate };
                }

                // Build a Pipeline for the Query
                pipeline = context.Query(query);

                // Try to get the count up to the timeout
                RunResult result = pipeline.RunUntilTimeout(timeout);

                using (ITabularWriter writer = WriterForFormat("json", response))
                {
                    writer.SetColumns(new string[] { "Count", "IsComplete", "RuntimeMs" });
                    writer.Write((int)result.RowCount);
                    writer.Write(result.IsComplete);
                    writer.Write((int)result.Elapsed.TotalMilliseconds);
                    writer.NextRow();
                }
            }
            finally
            {
                if (pipeline != null)
                {
                    pipeline.Dispose();
                    pipeline = null;
                }
            }
        }

        private void Run(string query, string format, int rowCountLimit, int colCountLimit, DateTime asOfDate, HttpListenerResponse response)
        {
            IXTable pipeline = null;

            try
            {
                XDatabaseContext context = _xDatabaseContext;

                // Build for another moment in time if requested
                if (asOfDate != _xDatabaseContext.RequestedAsOfDateTime)
                {
                    context = new XDatabaseContext(_xDatabaseContext) { RequestedAsOfDateTime = asOfDate };
                }

                // Build a Pipeline for the Query
                pipeline = context.Query(query);

                // Restrict the row and column count if requested
                if (rowCountLimit >= 0 || colCountLimit > 0)
                {
                    pipeline = new Verbs.Limit(pipeline, rowCountLimit, colCountLimit);
                }

                // Build a writer for the desired format
                using (ITabularWriter writer = WriterForFormat(format, response))
                {
                    // Run the query and return the output
                    pipeline = new TabularFileWriter(pipeline, writer);
                    pipeline.RunAndDispose();
                }
            }
            finally
            {
                if (pipeline != null)
                {
                    pipeline.Dispose();
                    pipeline = null;
                }
            }
        }

        private void Save(HttpListenerContext context, HttpListenerResponse response)
        {
            try
            {
                Save(
                    Require(context, "q"),
                    Require(context, "name"));

                // Success
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                using (ITabularWriter writer = WriterForFormat("json", response))
                {
                    WriteException(ex, writer);
                }
            }
        }

        private void Save(string query, string tableName)
        {
            _xDatabaseContext.Runner.Save(query, tableName);
        }

        private ITabularWriter WriterForFormat(string format, HttpListenerResponse response)
        {
            Stream toStream = response.OutputStream;
            toStream = new BufferedStream(toStream, 64 * 1024);

            switch (format.ToLowerInvariant())
            {
                case "json":
                    return new JsonTabularWriter(toStream);
                case "csv":
                    response.AddHeader("Content-Disposition", "attachment; filename=\"Result.csv\"");
                    return new CsvWriter(toStream);
                case "tsv":
                    response.AddHeader("Content-Disposition", "attachment; filename=\"Result.tsv\"");
                    return new TsvWriter(toStream);
                default:
                    throw new ArgumentException("fmt");
            }
        }

        private void WriteException(SuggestResult result, ITabularWriter writer)
        {
            WriteException(result.Context, result.IsValid, writer);
        }

        private void WriteException(Exception ex, ITabularWriter writer, bool isValid = false)
        {
            String8Block block = new String8Block();

            if (ex is UsageException)
            {
                UsageException ue = ex as UsageException;
                WriteException(ue.Context, isValid, writer);
            }
            else
            {
                writer.SetColumns(new string[] { "Valid", "Message", "Stack" });
                writer.Write(false);
                writer.Write(block.GetCopy(ex.Message));
                writer.Write(block.GetCopy(ex.StackTrace));
                writer.NextRow();
            }
        }

        private void WriteException(ErrorContext context, bool isValid, ITabularWriter writer)
        {
            String8Block block = new String8Block();

            writer.SetColumns(new string[] { "Valid", "Usage", "ItemCategory", "ErrorMessage", "Values", "InvalidToken", "InvalidTokenIndex" });
            writer.Write(isValid);
            writer.Write(block.GetCopy(context.Usage));
            writer.Write(block.GetCopy(context.InvalidValueCategory));
            writer.Write(block.GetCopy(context.ErrorMessage));

            String8 values = String8.Empty;
            if (context.ValidValues != null)
            {
                foreach (string value in context.ValidValues)
                {
                    values = block.Concatenate(values, s_delimiter, block.GetCopy(value));
                }
            }
            writer.Write(values);

            writer.Write(block.GetCopy(context.InvalidValue ?? ""));
            writer.Write(context.InvalidTokenIndex);

            writer.NextRow();
        }

        private static string Require(HttpListenerContext context, string parameterName)
        {
            string value = context.Request.QueryString[parameterName];
            if (value == null) throw new ArgumentException($"Request must include parameter \"{parameterName}\".");
            return value;
        }

        private static int ParseOrDefault(string value, int defaultValue)
        {
            if (String.IsNullOrEmpty(value)) return defaultValue;

            int result;
            if (!int.TryParse(value, out result)) return defaultValue;

            return result;
        }

        private static DateTime ParseOrDefault(string value, DateTime defaultValue)
        {
            if (String.IsNullOrEmpty(value)) return defaultValue;

            DateTime result;
            if (!DateTime.TryParse(value, out result)) return defaultValue;

            return result;
        }
    }
}

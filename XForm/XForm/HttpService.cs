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
        private WorkflowContext _workflowContext;
        private static String8 s_delimiter = String8.Convert(";", new byte[1]);

        public HttpService(WorkflowContext workflowContext)
        {
            _workflowContext = workflowContext;
        }

        public void Run()
        {
            using (BackgroundWebServer server = new BackgroundWebServer("index.html", "Web"))
            {
                server.AddResponder("suggest", Suggest);
                server.AddResponder("run", Run);
                server.AddResponder("download", Download);

                server.Start();
                Console.WriteLine("Http Server running; browse http://localhost:5073. Press enter to stop server.");
                Console.ReadLine();
                server.Stop();
            }
        }

        private void Suggest(HttpListenerContext context, HttpListenerResponse response)
        {
            using (JsonTabularWriter writer = new JsonTabularWriter(response.OutputStream))
            {
                try
                {
                    string query = Require(context, "q");

                    QuerySuggester suggester = new QuerySuggester(_workflowContext);
                    SuggestResult result = suggester.Suggest(query);

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
                    ParseOrDefault(context.Request.QueryString["c"], 100),
                    response);
            }
            catch (Exception ex)
            {
                using (ITabularWriter writer = new JsonTabularWriter(response.OutputStream))
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
                    -1,
                    response);
            }
            catch (Exception ex)
            {
                using (ITabularWriter writer = new JsonTabularWriter(response.OutputStream))
                {
                    WriteException(ex, writer);
                }
            }
        }

        private void Run(string query, string format, int rowCountLimit, HttpListenerResponse response)
        {
            IDataBatchEnumerator pipeline = null;

            try
            {
                // Build a Pipeline for the Query
                pipeline = XqlParser.Parse(query, null, _workflowContext);

                // Restrict the row count if requested
                if (rowCountLimit >= 0)
                {
                    pipeline = XqlParser.Parse($"limit {rowCountLimit}", pipeline, _workflowContext);
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
            String8Block block = new String8Block();

            writer.SetColumns(new string[] { "Valid", "Usage", "ItemCategory", "Values" });
            writer.Write(result.IsValid);
            writer.Write(block.GetCopy(result.Context.Usage));
            writer.Write(block.GetCopy(result.Context.InvalidValueCategory));

            String8 values = String8.Empty;
            if (result.Context.ValidValues != null)
            {
                foreach (string value in result.Context.ValidValues)
                {
                    values = block.Concatenate(values, s_delimiter, block.GetCopy(value));
                }
            }
            writer.Write(values);

            writer.NextRow();
        }

        private void WriteException(Exception ex, ITabularWriter writer, bool isValid = false)
        {
            String8Block block = new String8Block();

            if (ex is UsageException)
            {
                UsageException ue = ex as UsageException;

                writer.SetColumns(new string[] { "Valid", "Usage", "ItemCategory", "Values" });
                writer.Write(isValid);
                writer.Write(block.GetCopy(ue.Context.Usage));
                writer.Write(block.GetCopy(ue.Context.InvalidValueCategory));

                String8 values = String8.Empty;
                if (ue.Context.ValidValues != null)
                {
                    foreach (string value in ue.Context.ValidValues)
                    {
                        values = block.Concatenate(values, s_delimiter, block.GetCopy(value));
                    }
                }
                writer.Write(values);

                writer.NextRow();
            }
            else
            {
                writer.SetColumns(new string[] { "Valid", "Message" });
                writer.Write(false);
                writer.Write(block.GetCopy(ex.Message));
                writer.NextRow();
            }
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
    }
}

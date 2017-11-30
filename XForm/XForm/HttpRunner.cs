using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using System.IO;
using System.Net;
using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;

namespace XForm
{
    public class HttpRunner
    {
        private WorkflowRunner _innerRunner;
        private static String8 Delimiter = String8.Convert(";", new byte[1]);

        public HttpRunner(WorkflowRunner runner)
        {
            _innerRunner = runner;
        }

        public void Run()
        {
            using (BackgroundWebServer server = new BackgroundWebServer("", ""))
            {
                server.AddResponder("suggest", Suggest);
                server.AddResponder("run", Run);

                server.Start();
                Console.WriteLine("Http Server running; browse http://localhost:5073. Press enter to stop server.");
                Console.ReadLine();
                server.Stop();
            }
        }

        private void Suggest(HttpListenerContext context, HttpListenerResponse response)
        {
            String8Block block = new String8Block();
            using (JsonTabularWriter writer = new JsonTabularWriter(response.OutputStream))
            {
                try
                {
                    string query = context.Request.QueryString["q"];
                    if (String.IsNullOrEmpty(query)) throw new ArgumentException("q");
                
                    // Try building the query pipeline, using a *DeferredRunner* so dependencies aren't built right now
                    IDataBatchEnumerator pipeline = PipelineParser.BuildPipeline(query, null, new WorkflowContext() { Runner = new DeferredRunner(_innerRunner) });
                    writer.SetColumns(new string[] { "Valid" });
                    writer.Write(true);
                    writer.NextRow();
                }
                catch (Exception ex)
                {
                    WriteException(ex, writer);
                }
            }
        }

        private void Run(HttpListenerContext context, HttpListenerResponse response)
        {
            IDataBatchEnumerator pipeline = null;

            try
            {
                string query = context.Request.QueryString["q"];
                if (String.IsNullOrEmpty(query)) throw new ArgumentException("q");

                int rowCount = ParseOrDefault(context.Request.QueryString["c"], 100);

                // Build a Pipeline for the Query
                pipeline = PipelineParser.BuildPipeline(query, null, new WorkflowContext() { Runner = _innerRunner });

                // Restrict the row count if requested
                if (rowCount >= 0)
                {
                    pipeline = PipelineParser.BuildStage($"limit {rowCount}", pipeline);
                }

                // Build a writer for the desired format
                string format = context.Request.QueryString["fmt"] ?? "json";
                using (ITabularWriter writer = WriterForFormat(format, response))
                {
                    // Run the query and return the output
                    pipeline = new TabularFileWriter(pipeline, writer);
                    pipeline.RunAndDispose();
                }
            }
            catch(Exception ex)
            {
                using (JsonTabularWriter writer = new JsonTabularWriter(response.OutputStream))
                {
                    WriteException(ex, writer);
                }
            }
        }

        private ITabularWriter WriterForFormat(string format, HttpListenerResponse response)
        {
            switch(format.ToLowerInvariant())
            {
                case "json":
                    return new JsonTabularWriter(response.OutputStream);
                case "csv":
                    response.AddHeader("Content-Disposition", "attachment; filename=\"Result.csv\"");
                    return new CsvWriter(response.OutputStream);
                case "tsv":
                    response.AddHeader("Content-Disposition", "attachment; filename=\"Result.tsv\"");
                    return new TsvWriter(response.OutputStream);
                default:
                    throw new ArgumentException("fmt");
            }
        }

        private void WriteException(Exception ex, JsonTabularWriter writer)
        {
            String8Block block = new String8Block();
            
            if (ex is UsageException)
            {
                UsageException ue = ex as UsageException;

                writer.SetColumns(new string[] { "Valid", "Usage", "ItemCategory", "Values" });
                writer.Write(false);
                writer.Write(block.GetCopy(ue.Usage));
                writer.Write(block.GetCopy(ue.InvalidValueCategory));

                String8 values = String8.Empty;
                foreach (string value in ue.ValidValues)
                {
                    values = block.Concatenate(values, Delimiter, block.GetCopy(value));
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

        private static int ParseOrDefault(string value, int defaultValue)
        {
            if (String.IsNullOrEmpty(value)) return defaultValue;

            int result;
            if (!int.TryParse(value, out result)) return defaultValue;

            return result;
        }
    }
}

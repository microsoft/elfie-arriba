// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using Elfie.Serialization;
using Elfie.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class DataBatchEnumeratorTests
    {
        private static string s_outputRootFolderPath = @"C:\Download";

        public static string WebRequestSample = "WebRequestSample.5.1000.csv";
        public static string WebServerSample = "WebServerSample.csv";

        private static string s_sampleTableFileName = Path.Combine(s_outputRootFolderPath, "WebRequestSample.xform");
        private static string s_expectedOutputFileName = Path.Combine(s_outputRootFolderPath, "WebRequestSample.Expected.csv");
        private static string s_actualOutputFileName = Path.Combine(s_outputRootFolderPath, "WebRequestSample.Actual.csv");

        public static void WriteSamples()
        {
            if (!File.Exists(WebRequestSample)) Resource.SaveStreamTo($"XForm.Test.{WebRequestSample}", WebRequestSample);
            if (!File.Exists(WebServerSample)) Resource.SaveStreamTo($"XForm.Test.{WebServerSample}", WebServerSample);
        }

        [TestInitialize]
        public void EnsureSamplesExist()
        {
            WriteSamples();
        }

        [TestMethod]
        public void Scenario_EndToEnd()
        {
            PipelineParser.BuildPipeline($@"
                read {WebRequestSample}
                columns ID EventTime ServerName ServerPort HttpStatus ClientOs WasCachedResponse
                write {s_expectedOutputFileName}
                cast ID int32
                cast EventTime DateTime
                cast ServerPort int32
                cast HttpStatus int32           
                cast WasCachedResponse boolean
                write {s_sampleTableFileName}
            ").RunAndDispose();

            PipelineParser.BuildPipeline($@"
                read {s_sampleTableFileName}
                write {s_actualOutputFileName}
            ").RunAndDispose();

            Assert.AreEqual(File.ReadAllText(s_expectedOutputFileName), File.ReadAllText(s_actualOutputFileName));
        }

        [TestMethod]
        public void Join()
        {
            // Build binary format tables for the join
            PipelineParser.BuildPipeline($@"
                read {WebServerSample}
                write {WebServerSample}.xform
                where ServerRam >= 4096
                write {WebServerSample}.Big.xform
            ").RunAndDispose();

            // Join. Verify no exceptions. Verify all rows match.
            Assert.AreEqual(1000, PipelineParser.BuildPipeline($@"
                read {WebRequestSample}
                columns ID EventTime ServerName
                join ServerName {WebServerSample}.xform ServerName Server.
            ").RunAndDispose());

            // Join to partial set. Verify no exceptions. Verify only some rows match.
            Assert.AreEqual(613, PipelineParser.BuildPipeline($@"
                read {WebRequestSample}
                columns ID EventTime ServerName
                join ServerName {WebServerSample}.Big.xform ServerName Server.
            ").RunAndDispose());
        }

        private static IDataBatchEnumerator SampleReader()
        {
            return PipelineParser.BuildStage($"read {WebRequestSample}", null);
        }

        private static string[] SampleColumns()
        {
            using (IDataBatchEnumerator sample = SampleReader())
            {
                return sample.Columns.Select((cd) => cd.Name).ToArray();
            }
        }

        public static void DataSourceEnumerator_All(string configurationLine, int expectedRowCount, string[] requiredColumns = null)
        {
            int requiredColumnCount = (requiredColumns == null ? 0 : requiredColumns.Length);
            int actualRowCount;

            IDataBatchEnumerator pipeline = null;
            DataBatchEnumeratorContractValidator innerValidator = null;
            try
            {
                pipeline = SampleReader();
                innerValidator = new DataBatchEnumeratorContractValidator(pipeline);
                pipeline = PipelineParser.BuildStage(configurationLine, innerValidator);

                // Run without requesting any columns. Validate.
                Assert.AreEqual(requiredColumnCount, innerValidator.ColumnGettersRequested.Count);
                actualRowCount = pipeline.Run();
                Assert.AreEqual(expectedRowCount, actualRowCount, "DataSourceEnumerator should return correct count with no requested columns.");
                Assert.AreEqual(requiredColumnCount, innerValidator.ColumnGettersRequested.Count, "No extra columns requested after Run");

                // Reset; Request all columns. Validate.
                pipeline.Reset();
                pipeline = PipelineParser.BuildStage("write \"Sample.output.csv\"", pipeline);
                actualRowCount = pipeline.Run();
            }
            finally
            {
                if (pipeline != null)
                {
                    pipeline.Dispose();
                    pipeline = null;

                    if (innerValidator != null)
                    {
                        Assert.IsTrue(innerValidator.DisposeCalled, "Source must call Dispose on nested sources.");
                    }
                }
            }
        }

        [TestMethod]
        public void DataSourceEnumerator_EndToEnd()
        {
            DataSourceEnumerator_All("columns ID EventTime ServerPort HttpStatus ClientOs WasCachedResponse", 1000);
            DataSourceEnumerator_All("limit 10", 10);
            DataSourceEnumerator_All("count", 1);
            DataSourceEnumerator_All("where ServerPort = 80", 423, new string[] { "ServerPort" });
            DataSourceEnumerator_All("cast EventTime DateTime", 1000);
            DataSourceEnumerator_All("removecolumns EventTime", 1000);
            DataSourceEnumerator_All("write WebRequestSample.xform", 1000, SampleColumns());
        }

        [TestMethod]
        public void DataSourceEnumerator_Errors()
        {
            Verify.Exception<ArgumentException>(() => PipelineParser.BuildStage("read", null), "Usage: 'read' [tableNameOrFilePath]");
            Verify.Exception<FileNotFoundException>(() => PipelineParser.BuildStage("read NotFound.csv", null));
            Verify.Exception<ColumnNotFoundException>(() => PipelineParser.BuildStage("removeColumns NotFound", SampleReader()));

            // Verify casting a type to itself doesn't error
            PipelineParser.BuildPipeline(@"
                cast EventTime DateTime
                cast EventTime DateTime", SampleReader()).RunAndDispose();
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
            string fullSamplePath = Path.GetFullPath(WebRequestSample);
            if (!File.Exists(fullSamplePath)) Resource.SaveStreamTo($"XForm.Test.{WebRequestSample}", fullSamplePath);
            if (!Directory.Exists($"{WebRequestSample}.xform"))
            {
                PipelineParser.BuildPipeline($@"
                    read {WebRequestSample}
                    write {WebRequestSample}.xform").RunAndDispose();
            }

            if (!File.Exists(WebServerSample)) Resource.SaveStreamTo($"XForm.Test.{WebServerSample}", WebServerSample);
            if (!Directory.Exists($"{WebServerSample}.xform"))
            {
                PipelineParser.BuildPipeline($@"
                    read {WebServerSample}
                    write {WebServerSample}.xform").RunAndDispose();
            }
        }

        [TestInitialize]
        public void EnsureSamplesExist()
        {
            WriteSamples();
        }

        private static IDataBatchEnumerator s_cachedSample;

        private static IDataBatchEnumerator SampleReader()
        {
            if (s_cachedSample == null)
            {
                //s_cachedSample = PipelineParser.BuildPipeline($"read {WebRequestSample}");

                // Read the binary form and cache it in memory
                s_cachedSample = PipelineParser.BuildPipeline($@"
                    read {WebRequestSample}.xform
                    cache");

                // Force all columns to be cached so it can be Disposed safely
                // [The cacher can't get additional columns after the first Next call]
                for (int i = 0; i < s_cachedSample.Columns.Count; ++i)
                {
                    s_cachedSample.ColumnGetter(i);
                }
            }

            s_cachedSample.Reset();
            return s_cachedSample;
        }

        private static string[] SampleColumns()
        {
            using (IDataBatchEnumerator sample = SampleReader())
            {
                return sample.Columns.Select((cd) => cd.Name).ToArray();
            }
        }

        [TestMethod]
        public void Scenario_EndToEnd()
        {
            PipelineParser.BuildPipeline($@"
                read {WebRequestSample}
                columns ID EventTime ServerName ServerPort HttpStatus ClientOs WasCachedResponse
                renameColumns ServerPort PortNumber, HttpStatus HttpResponseStatus
                write {s_expectedOutputFileName}
                cast ID int32
                cast EventTime DateTime
                cast PortNumber int32
                cast HttpResponseStatus int32           
                cast WasCachedResponse boolean
                write {s_sampleTableFileName}
            ").RunAndDispose();

            PipelineParser.BuildPipeline($@"
                read {s_sampleTableFileName}
                write {s_actualOutputFileName}
            ").RunAndDispose();

            Verify.FilesEqual(s_expectedOutputFileName, s_actualOutputFileName);
        }

        [TestMethod]
        public void NullableHandling()
        {
            // Cast columns with nulls (RequestBytes, IsPremiumUser, DaysSinceJoined) and without (ID, EventTime)
            // Write the values pre-cast (nulls are empty string)
            // Write an XForm table (nulls should be marked by Vn.b8.bin)
            PipelineParser.BuildPipeline($@"
                read {WebRequestSample}
                columns ID EventTime UserName RequestBytes IsPremiumUser DaysSinceJoined
                write {Path.Combine(s_outputRootFolderPath, "WebRequest.Nullable.Expected.tsv")}
                cast RequestBytes int32
                cast IsPremiumUser boolean           
                cast DaysSinceJoined int32
                cast ID int32
                write {Path.Combine(s_outputRootFolderPath, "WebRequest.Nullable.Actual.xform")} 
                where UserName != """"
                assert none                    
                    where DaysSinceJoined = null
                    end
                assert none
                    where IsPremiumUser = null
                    end
            ").RunAndDispose();

            // Read the table with nulls back and confirm they're recognized and converted back to empty string
            PipelineParser.BuildPipeline($@"
                read {Path.Combine(s_outputRootFolderPath, "WebRequest.Nullable.Actual.xform")}
                write {Path.Combine(s_outputRootFolderPath, "WebRequest.Nullable.Actual.tsv")}
            ").RunAndDispose();

            Verify.FilesEqual(Path.Combine(s_outputRootFolderPath, "WebRequest.Nullable.Expected.tsv"), Path.Combine(s_outputRootFolderPath, "WebRequest.Nullable.Actual.tsv"));
        }

        [TestMethod]
        public void Join()
        {
            // Build binary format tables for the join
            PipelineParser.BuildPipeline($@"
                read {WebServerSample}.xform
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
            DataSourceEnumerator_All("renamecolumns ServerPort PortNumber, HttpStatus HttpResult", 1000);
        }

        [TestMethod]
        public void DataSourceEnumerator_Errors()
        {
            Verify.Exception<UsageException>(() => PipelineParser.BuildStage("read", null));
            Verify.Exception<FileNotFoundException>(() => PipelineParser.BuildStage("read NotFound.csv", null));
            Verify.Exception<UsageException>(() => PipelineParser.BuildStage("removeColumns NotFound", SampleReader()));

            // Verify casting a type to itself doesn't error
            PipelineParser.BuildPipeline(@"
                cast EventTime DateTime
                cast EventTime DateTime", SampleReader()).RunAndDispose();
        }
    }
}

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

namespace XForm.Test.Query
{
    [TestClass]
    public class DataBatchEnumeratorTests
    {
        private static string s_outputRootFolderPath = @"C:\Download";

        public static string SampleFileName = "WebRequestSample.5.1000.csv";
        private static string s_sampleTableFileName = Path.Combine(s_outputRootFolderPath, "WebRequestSample.xform");
        private static string s_expectedOutputFileName = Path.Combine(s_outputRootFolderPath, "WebRequestSample.Expected.csv");
        private static string s_actualOutputFileName = Path.Combine(s_outputRootFolderPath, "WebRequestSample.Actual.csv");

        public static void WriteSampleFile()
        {
            if (!File.Exists(SampleFileName))
            {
                Resource.SaveStreamTo($"XForm.Test.{SampleFileName}", SampleFileName);
            }
        }

        [TestInitialize]
        public void EnsureSampleFileExists()
        {
            WriteSampleFile();
        }

        [TestMethod]
        public void Scenario_EndToEnd()
        {
            PipelineFactory.BuildPipeline($@"
                read {SampleFileName}
                columns ID EventTime ServerPort HttpStatus ClientOs WasCachedResponse
                write {s_expectedOutputFileName}
                cast ID int32
                cast EventTime DateTime
                cast ServerPort int32
                cast HttpStatus int32           
                cast WasCachedResponse boolean
                write {s_sampleTableFileName}
            ").RunAndDispose();

            PipelineFactory.BuildPipeline($@"
                read {s_sampleTableFileName}
                write {s_actualOutputFileName}
            ").RunAndDispose();

            Assert.AreEqual(File.ReadAllText(s_expectedOutputFileName), File.ReadAllText(s_actualOutputFileName));
        }

        private static IDataBatchEnumerator SampleReader()
        {
            return PipelineFactory.BuildStage($"read {SampleFileName}", null);
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
                pipeline = PipelineFactory.BuildStage(configurationLine, innerValidator);

                // Run without requesting any columns. Validate.
                Assert.AreEqual(requiredColumnCount, innerValidator.ColumnGettersRequested.Count);
                actualRowCount = pipeline.Run();
                Assert.AreEqual(expectedRowCount, actualRowCount, "DataSourceEnumerator should return correct count with no requested columns.");
                Assert.AreEqual(requiredColumnCount, innerValidator.ColumnGettersRequested.Count, "No extra columns requested after Run");

                // Reset; Request all columns. Validate.
                pipeline.Reset();
                pipeline = PipelineFactory.BuildStage("write \"Sample.output.csv\"", pipeline);
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
            DataSourceEnumerator_All("convert EventTime DateTime", 1000);
            DataSourceEnumerator_All("removecolumns EventTime", 1000);
            DataSourceEnumerator_All("write WebRequestSample.xform", 1000, SampleColumns());
        }

        [TestMethod]
        public void DataSourceEnumerator_Errors()
        {
            Verify.Exception<ArgumentException>(() => PipelineFactory.BuildStage("read", null), "Usage: 'read' [tableNameOrFilePath]");
            Verify.Exception<FileNotFoundException>(() => PipelineFactory.BuildStage("read NotFound.csv", null));
            Verify.Exception<ColumnNotFoundException>(() => PipelineFactory.BuildStage("removeColumns NotFound", SampleReader()));

            // Verify casting a type to itself doesn't error
            PipelineFactory.BuildPipeline(@"
                cast EventTime DateTime
                cast EventTime DateTime", SampleReader()).RunAndDispose();
        }
    }
}

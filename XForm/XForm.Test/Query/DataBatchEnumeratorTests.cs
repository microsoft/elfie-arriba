//// Copyright (c) Microsoft. All rights reserved.
//// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//using System.IO;

//using Elfie.Test;

//using Microsoft.VisualStudio.TestTools.UnitTesting;

//using XForm.Data;
//using XForm.Extensions;
//using XForm.Query;

//namespace XForm.Test.Query
//{
//    [TestClass]
//    public class DataBatchEnumeratorTests
//    {
//        [TestMethod]
//        public void Scenario_EndToEnd()
//        {
//            PipelineParser.BuildPipeline($@"
//                read WebRequest
//                columns ID EventTime ServerName ServerPort HttpStatus ClientOs WasCachedResponse
//                renameColumns ServerPort PortNumber, HttpStatus HttpResponseStatus
//                write WebRequest.EndToEnd.Actual.csv
//                cast ID int32
//                cast EventTime DateTime
//                cast PortNumber int32
//                cast HttpResponseStatus int32           
//                cast WasCachedResponse boolean
//                write WebRequest.EndToEnd.Expected.xform
//            ", null, SampleDatabase.GetWorkflowContext()).RunAndDispose();

//            PipelineParser.BuildPipeline($@"
//                read {s_sampleTableFileName}
//                write {s_actualOutputFileName}
//            ").RunAndDispose();

//            Verify.FilesEqual(s_expectedOutputFileName, s_actualOutputFileName);
//        }

//        [TestMethod]
//        public void NullableHandling()
//        {
//            // Cast columns with nulls (RequestBytes, IsPremiumUser, DaysSinceJoined) and without (ID, EventTime)
//            // Write the values pre-cast (nulls are empty string)
//            // Write an XForm table (nulls should be marked by Vn.b8.bin)
//            PipelineParser.BuildPipeline($@"
//                read WebRequest
//                columns ID EventTime UserName RequestBytes IsPremiumUser DaysSinceJoined
//                write WebRequest.Nullable.Expected.tsv
//                cast RequestBytes int32
//                cast IsPremiumUser boolean           
//                cast DaysSinceJoined int32
//                cast ID int32
//                write WebRequest.Nullable.Actual.xform 
//                where UserName != """"
//                assert none                    
//                    where DaysSinceJoined = null
//                    end
//                assert none
//                    where IsPremiumUser = null
//                    end
//            ", null, SampleDatabase.GetWorkflowContext()).RunAndDispose();

//            // Read the table with nulls back and confirm they're recognized and converted back to empty string
//            PipelineParser.BuildPipeline($@"
//                read {Path.Combine(s_outputRootFolderPath, "WebRequest.Nullable.Actual.xform")}
//                write {Path.Combine(s_outputRootFolderPath, "WebRequest.Nullable.Actual.tsv")}
//            ", null, SampleDatabase.GetWorkflowContext()).RunAndDispose();

//            Verify.FilesEqual(Path.Combine(s_outputRootFolderPath, "WebRequest.Nullable.Expected.tsv"), Path.Combine(s_outputRootFolderPath, "WebRequest.Nullable.Actual.tsv"));
//        }

//        [TestMethod]
//        public void Join()
//        {
//            // Build binary format tables for the join
//            PipelineParser.BuildPipeline($@"
//                read {WebServerSample}.xform
//                where ServerRam >= 4096
//                write {WebServerSample}.Big.xform
//            ", null, SampleDatabase.GetWorkflowContext()).RunAndDispose();

//            // Join. Verify no exceptions. Verify all rows match.
//            Assert.AreEqual(1000, PipelineParser.BuildPipeline($@"
//                read {WebRequestSample}
//                columns ID EventTime ServerName
//                join ServerName {WebServerSample}.xform ServerName Server.
//            ", null, SampleDatabase.GetWorkflowContext()).RunAndDispose());

//            // Join to partial set. Verify no exceptions. Verify only some rows match.
//            Assert.AreEqual(613, PipelineParser.BuildPipeline($@"
//                read {WebRequestSample}
//                columns ID EventTime ServerName
//                join ServerName {WebServerSample}.Big.xform ServerName Server.
//            ", null, SampleDatabase.GetWorkflowContext()).RunAndDispose());
//        }

//        public static void DataSourceEnumerator_All(string configurationLine, int expectedRowCount, string[] requiredColumns = null)
//        {
//            int requiredColumnCount = (requiredColumns == null ? 0 : requiredColumns.Length);
//            int actualRowCount;

//            IDataBatchEnumerator pipeline = null;
//            DataBatchEnumeratorContractValidator innerValidator = null;
//            try
//            {
//                pipeline = SampleReader();
//                innerValidator = new DataBatchEnumeratorContractValidator(pipeline);
//                pipeline = PipelineParser.BuildStage(configurationLine, innerValidator, SampleDatabase.GetWorkflowContext());

//                // Run without requesting any columns. Validate.
//                Assert.AreEqual(requiredColumnCount, innerValidator.ColumnGettersRequested.Count);
//                actualRowCount = pipeline.Run();
//                Assert.AreEqual(expectedRowCount, actualRowCount, "DataSourceEnumerator should return correct count with no requested columns.");
//                Assert.AreEqual(requiredColumnCount, innerValidator.ColumnGettersRequested.Count, "No extra columns requested after Run");

//                // Reset; Request all columns. Validate.
//                pipeline.Reset();
//                pipeline = PipelineParser.BuildStage("write \"Sample.output.csv\"", pipeline, SampleDatabase.GetWorkflowContext());
//                actualRowCount = pipeline.Run();
//            }
//            finally
//            {
//                if (pipeline != null)
//                {
//                    pipeline.Dispose();
//                    pipeline = null;

//                    if (innerValidator != null)
//                    {
//                        Assert.IsTrue(innerValidator.DisposeCalled, "Source must call Dispose on nested sources.");
//                    }
//                }
//            }
//        }

//        [TestMethod]
//        public void DataSourceEnumerator_EndToEnd()
//        {
//            DataSourceEnumerator_All("columns ID EventTime ServerPort HttpStatus ClientOs WasCachedResponse", 1000);
//            DataSourceEnumerator_All("limit 10", 10);
//            DataSourceEnumerator_All("count", 1);
//            DataSourceEnumerator_All("where ServerPort = 80", 423, new string[] { "ServerPort" });
//            DataSourceEnumerator_All("cast EventTime DateTime", 1000);
//            DataSourceEnumerator_All("removecolumns EventTime", 1000);
//            DataSourceEnumerator_All("renamecolumns ServerPort PortNumber, HttpStatus HttpResult", 1000);
//        }

//        [TestMethod]
//        public void DataSourceEnumerator_Errors()
//        {
//            Verify.Exception<UsageException>(() => PipelineParser.BuildStage("read", null, SampleDatabase.GetWorkflowContext()));
//            Verify.Exception<FileNotFoundException>(() => PipelineParser.BuildStage("read NotFound.csv", null, SampleDatabase.GetWorkflowContext()));
//            Verify.Exception<UsageException>(() => PipelineParser.BuildPipeline(@"
//                read WebRequest
//                removeColumns NotFound", null, SampleDatabase.GetWorkflowContext()));

//            // Verify casting a type to itself doesn't error
//            PipelineParser.BuildPipeline(@"
//                read WebRequest
//                cast EventTime DateTime
//                cast EventTime DateTime", 
//                null, 
//                SampleDatabase.GetWorkflowContext()).RunAndDispose();
//        }
//    }
//}

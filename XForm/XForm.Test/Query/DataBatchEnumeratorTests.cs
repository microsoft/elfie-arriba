using Elfie.Serialization;
using Elfie.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using XForm.Data;
using XForm.Extensions;

namespace XForm.Test.Query
{
    [TestClass]
    public class DataBatchEnumeratorTests
    {
        [TestInitialize]
        public void WriteSampleFile()
        {
            Resource.SaveStreamTo("XForm.Test.WebRequestSample.5.1000.csv", "WebRequestSample.5.1000.csv");
        }

        [TestMethod]
        public void TabularFileReader_Errors()
        {
            Verify.Exception<ArgumentException>(() => PipelineFactory.BuildStage(null, "read"), "Usage: 'read' [filePath]");
            Verify.Exception<FileNotFoundException>(() => PipelineFactory.BuildStage(null, "read NotFound.csv"));
        }

        public static void DataSourceEnumerator_All(string configurationLine, int expectedRowCount, string[] requiredColumns = null)
        {
            int requiredColumnCount = (requiredColumns == null ? 0 : requiredColumns.Length);
            int actualRowCount;

            IDataBatchEnumerator pipeline = null;
            DataBatchEnumeratorContractValidator innerValidator = null;
            try
            {
                pipeline = PipelineFactory.BuildStage(null, "read \"WebRequestSample.5.1000.csv\"");
                innerValidator = new DataBatchEnumeratorContractValidator(pipeline);
                pipeline = PipelineFactory.BuildStage(innerValidator, configurationLine);

                // Run without requesting any columns. Validate.
                Assert.AreEqual(requiredColumnCount, innerValidator.ColumnGettersRequested.Count);
                actualRowCount = pipeline.Run();
                Assert.AreEqual(expectedRowCount, actualRowCount, "DataSourceEnumerator should return correct count with no requested columns.");
                Assert.AreEqual(requiredColumnCount, innerValidator.ColumnGettersRequested.Count, "No extra columns requested after Run");

                // Reset; Request all columns. Validate.
                pipeline.Reset();
                pipeline = PipelineFactory.BuildStage(pipeline, "write \"Sample.output.csv\"");
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
        }
    }
}

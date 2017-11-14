using Elfie.Serialization;
using Elfie.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XForm.Data;
using XForm.Extensions;

namespace XForm.Test.Query
{
    [TestClass]
    public class DataBatchEnumeratorTests
    {
        private const string SampleFileName = "WebRequestSample.5.1000.csv";

        [TestInitialize]
        public void WriteSampleFile()
        {
            Resource.SaveStreamTo($"XForm.Test.{SampleFileName}", SampleFileName);
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
            Verify.Exception<ArgumentException>(() => PipelineFactory.BuildStage("read", null), "Usage: 'read' [filePath]");
            Verify.Exception<FileNotFoundException>(() => PipelineFactory.BuildStage("read NotFound.csv", null));
            Verify.Exception<ColumnNotFoundException>(() => PipelineFactory.BuildStage("removeColumns NotFound", SampleReader()));
        }
    }
}

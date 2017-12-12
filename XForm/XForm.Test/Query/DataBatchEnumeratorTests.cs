// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Elfie.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class DataBatchEnumeratorTests
    {
        private static IDataBatchEnumerator s_SampleReader;
        private static IDataBatchEnumerator SampleReader()
        {
            if (s_SampleReader == null)
            {
                SampleDatabase.EnsureBuilt();
                s_SampleReader = XqlParser.Parse(@"
                read WebRequest
                cache all
                ", null, SampleDatabase.WorkflowContext);
            }

            s_SampleReader.Reset();
            return s_SampleReader;
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
                pipeline = XqlParser.Parse(configurationLine, innerValidator, SampleDatabase.WorkflowContext);

                // Run without requesting any columns. Validate.
                Assert.AreEqual(requiredColumnCount, innerValidator.ColumnGettersRequested.Count);
                actualRowCount = pipeline.Run();
                Assert.AreEqual(expectedRowCount, actualRowCount, "DataSourceEnumerator should return correct count with no requested columns.");
                Assert.AreEqual(requiredColumnCount, innerValidator.ColumnGettersRequested.Count, "No extra columns requested after Run");

                // Reset; Request all columns. Validate.
                pipeline.Reset();
                pipeline = XqlParser.Parse("write \"Sample.output.csv\"", pipeline, SampleDatabase.WorkflowContext);
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
            DataSourceEnumerator_All("columns [ID] [EventTime] [ServerPort] [HttpStatus] [ClientOs] [WasCachedResponse]", 1000);
            DataSourceEnumerator_All("limit 10", 10);
            DataSourceEnumerator_All("count", 1);
            DataSourceEnumerator_All("where [ServerPort] = 80", 423, new string[] { "ServerPort" });
            DataSourceEnumerator_All("cast [EventTime] DateTime", 1000);
            DataSourceEnumerator_All("removecolumns [EventTime]", 1000);
            DataSourceEnumerator_All("renamecolumns [ServerPort] [PortNumber], [HttpStatus] [HttpResult]", 1000);
        }

        [TestMethod]
        public void DataSourceEnumerator_Errors()
        {
            Verify.Exception<UsageException>(() => XqlParser.Parse("read", null, SampleDatabase.WorkflowContext));
            Verify.Exception<UsageException>(() => XqlParser.Parse("read NotFound.csv", null, SampleDatabase.WorkflowContext));
            Verify.Exception<UsageException>(() => XqlParser.Parse(@"
                read WebRequest
                removeColumns [NotFound]", null, SampleDatabase.WorkflowContext));

            // Column name not in braces
            Verify.Exception<UsageException>(() => XqlParser.Parse(@"
                read WebRequest
                where ""EventTime"" < ""2017-12-04""", null, SampleDatabase.WorkflowContext));

            // String value in braces
            Verify.Exception<UsageException>(() => XqlParser.Parse(@"read [WebRequest]", null, SampleDatabase.WorkflowContext));

            // Verify casting a type to itself doesn't error
            XqlParser.Parse(@"
                read WebRequest
                cast [EventTime] DateTime
                cast [EventTime] DateTime",
                null,
                SampleDatabase.WorkflowContext).RunAndDispose();
        }
    }
}

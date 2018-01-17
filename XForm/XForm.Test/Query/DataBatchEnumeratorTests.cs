// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public static void DataSourceEnumerator_All(string configurationLine, int expectedRowCount, string[] requiredColumns = null)
        {
            SampleDatabase.EnsureBuilt();

            int requiredColumnCount = (requiredColumns == null ? 0 : requiredColumns.Length);
            long actualRowCount;

            IDataBatchEnumerator pipeline = null;
            DataBatchEnumeratorContractValidator innerValidator = null;
            try
            {
                pipeline = SampleDatabase.XDatabaseContext.Load("WebRequest");
                innerValidator = new DataBatchEnumeratorContractValidator(pipeline);
                pipeline = SampleDatabase.XDatabaseContext.Query(configurationLine, innerValidator);

                // Run without requesting any columns. Validate.
                Assert.AreEqual(requiredColumnCount, innerValidator.ColumnGettersRequested.Count);
                actualRowCount = pipeline.RunWithoutDispose();
                Assert.AreEqual(expectedRowCount, actualRowCount, "DataSourceEnumerator should return correct count with no requested columns.");
                Assert.AreEqual(requiredColumnCount, innerValidator.ColumnGettersRequested.Count, "No extra columns requested after Run");

                // Reset; Request all columns. Validate.
                pipeline.Reset();
                pipeline = SampleDatabase.XDatabaseContext.Query("write \"Sample.output.csv\"", pipeline);
                actualRowCount = pipeline.RunWithoutDispose();
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
            DataSourceEnumerator_All("select [ID] [EventTime] [ServerPort] [HttpStatus] [ClientOs] [WasCachedResponse]", 1000);
            DataSourceEnumerator_All("limit 10", 10);
            DataSourceEnumerator_All("count", 1);
            DataSourceEnumerator_All("where [ServerPort] = \"80\"", 423, new string[] { "ServerPort" });
            DataSourceEnumerator_All("cast [EventTime] DateTime", 1000);
            DataSourceEnumerator_All("remove [EventTime]", 1000);
            DataSourceEnumerator_All("rename [ServerPort] [PortNumber], [HttpStatus] [HttpResult]", 1000);
        }

        [TestMethod]
        public void DataSourceEnumerator_Errors()
        {
            Verify.Exception<UsageException>(() => SampleDatabase.XDatabaseContext.Query("read"));
            Verify.Exception<UsageException>(() => SampleDatabase.XDatabaseContext.Query("read NotFound.csv"));
            Verify.Exception<UsageException>(() => SampleDatabase.XDatabaseContext.Query(@"
                read WebRequest
                remove [NotFound]"));

            // String value in braces
            Verify.Exception<UsageException>(() => SampleDatabase.XDatabaseContext.Query(@"read [WebRequest]"));

            // Verify casting a type to itself doesn't error
            SampleDatabase.XDatabaseContext.Query(@"
                read WebRequest
                select Cast(Cast([EventTime], DateTime), DateTime)").RunAndDispose();
        }
    }
}

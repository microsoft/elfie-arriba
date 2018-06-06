// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

using Elfie.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class XTableBasicTests
    {
        public static void XTable_All(string configurationLine, int expectedRowCount, string[] requiredColumns = null)
        {
            SampleDatabase.EnsureBuilt();

            int requiredColumnCount = (requiredColumns == null ? 0 : requiredColumns.Length);
            long actualRowCount;

            using (CancellationTokenSource source = new CancellationTokenSource())
            {
                IXTable pipeline = null;
                ValidatingTable innerValidator = null;
                CancellationToken token = source.Token;

                try
                {
                    pipeline = SampleDatabase.XDatabaseContext.Load("WebRequest");
                    innerValidator = new ValidatingTable(pipeline);
                    pipeline = SampleDatabase.XDatabaseContext.Query(configurationLine, innerValidator);

                    // Run without requesting any columns. Validate.
                    actualRowCount = pipeline.RunWithoutDispose(token).RowCount;
                    Assert.AreEqual(expectedRowCount, actualRowCount, "XTable should return correct count with no requested columns.");

                    // Reset; Request all columns. Validate.
                    pipeline.Reset();
                    pipeline = SampleDatabase.XDatabaseContext.Query("write \"Sample.output.csv\"", pipeline);
                    actualRowCount = pipeline.RunWithoutDispose(token).RowCount;
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
        }

        [TestMethod]
        public void XTable_EndToEnd()
        {
            // Testing everything except 'read' and 'write', which are covered indirectly through these.

            XTable_All("cast [EventTime] DateTime", 1000);
            XTable_All("choose Min [ID] [ServerPort]", 2);
            XTable_All("count", 1);
            XTable_All("groupBy [ServerPort] with Count()", 2);
            XTable_All("join [ServerName] WebServer [ServerName] Server.", 1000);
            XTable_All("limit 10", 10);
            XTable_All("peek [ClientBrowser]", 9);
            XTable_All("remove [EventTime]", 1000);
            XTable_All("rename [ServerPort] [PortNumber], [HttpStatus] [HttpResult]", 1000);
            XTable_All("schema", 22, new string[] { "Name", "Type" });
            XTable_All("select [ID] [EventTime] [ServerPort] [HttpStatus] [ClientOs] [WasCachedResponse]", 1000);
            XTable_All("set [ClientBrowser] ToUpper([ClientBrowser])", 1000);
            XTable_All("where [ServerPort] = \"80\"", 423, new string[] { "ServerPort" });
        }

        [TestMethod]
        public void XTable_Errors()
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

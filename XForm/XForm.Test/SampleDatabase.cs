using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;

namespace XForm.Test
{
    [TestClass]
    public class SampleDatabase
    {
        private static string s_RootPath;
        private static WorkflowContext s_WorkflowContext;

        public static WorkflowContext WorkflowContext
        {
            get
            {
                if (s_WorkflowContext != null) return s_WorkflowContext;
                EnsureBuilt();

                s_WorkflowContext = new WorkflowContext();
                s_WorkflowContext.StreamProvider = new LocalFileStreamProvider(s_RootPath);
                s_WorkflowContext.Runner = new WorkflowRunner(s_WorkflowContext);
                return s_WorkflowContext;
            }
        }

        public static void EnsureBuilt()
        {
            if (s_RootPath == null || !Directory.Exists(s_RootPath)) Build();
        }

        public static void Build()
        {
            if (s_RootPath == null) s_RootPath = Path.Combine(Environment.CurrentDirectory, "Database");
            DirectoryIO.DeleteAllContents(s_RootPath);

            // Unpack the sample database
            ZipFile.ExtractToDirectory("Database.zip", s_RootPath);

            // XForm add each source
            foreach (string filePath in Directory.GetFiles(Path.Combine(s_RootPath, "_Raw")))
            {
                Add(filePath);
            }
        }

        public static void Add(string filePath)
        {
            // Split the Table Name and AsOfDateTime from the sample data naming scheme (WebRequest.20171201....)
            string fileName = Path.GetFileName(filePath);
            string[] fileNameParts = fileName.Split('.');
            string tableName = fileNameParts[0];
            DateTime asOfDateTime = DateTime.ParseExact(fileNameParts[1], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            XForm($@"add ""{filePath}"" ""{tableName}"" Full ""{asOfDateTime}""");
            string expectedPath = Path.Combine(s_RootPath, "Source", tableName, "Full", asOfDateTime.ToString(StreamProviderExtensions.DateTimeFolderFormat), fileName);
            Assert.IsTrue(File.Exists(expectedPath), $"XForm add didn't add to expected location {expectedPath}");
        }

        public static void XForm(string xformCommand, int expectedExitCode = 0)
        {
            int result = Program.Run(new PipelineScanner(xformCommand).CurrentLineParts.ToArray(), s_RootPath, DateTime.UtcNow);
            Assert.AreEqual(expectedExitCode, result, $"Unexpected Exit Code for XForm {xformCommand}");
        }

        [TestMethod]
        public void Database_Build()
        {
            // Unpack the Database. Verify 'xform add' works for each source
            SampleDatabase.Build();
        }

        [TestMethod]
        public void Database_Sources()
        {
            SampleDatabase.EnsureBuilt();

            // Validate Database source list as returned by IWorkflowRunner.SourceNames. Don't validate the full list so that as test data is added this test isn't constantly failing.
            List<string> sources = new List<string>(SampleDatabase.WorkflowContext.Runner.SourceNames);
            Trace.Write(string.Join("\r\n", sources));
            Assert.IsTrue(sources.Contains("WebRequest"), "WebRequest table should exist");
            Assert.IsTrue(sources.Contains("WebRequest.Authenticated"), "WebRequest.Authenticated config should exist");
            Assert.IsTrue(sources.Contains("WebRequest.Typed"), "WebRequest.Typed config should exist");
            Assert.IsTrue(sources.Contains("WebRequest.BigServers"), "WebRequest.BigServers query should exist");
            Assert.IsTrue(sources.Contains("WebServer"), "WebServer table should exist");
        }

        [TestMethod]
        public void Database_RequestHistorical()
        {
            SampleDatabase.EnsureBuilt();

            // Ask for WebRequest as of 2017-12-02 just before noon. The 2017-12-02 version should be latest
            DateTime cutoff = new DateTime(2017, 12, 02, 11, 50, 00, DateTimeKind.Utc);
            XForm($"build WebRequest xform \"{cutoff:yyyy-MM-dd hh:mm:ssZ}");

            // Verify it has been created
            DateTime versionFound = SampleDatabase.WorkflowContext.StreamProvider.LatestBeforeCutoff(LocationType.Table, "WebRequest", cutoff).WhenModifiedUtc;
            Assert.AreEqual(new DateTime(2017, 12, 02, 00, 00, 00, DateTimeKind.Utc), versionFound);

            // Ask for WebRequest.Authenticated. Verify a 2017-12-02 version is also built for it
            XForm($"build WebRequest.Authenticated xform \"{cutoff:yyyy-MM-dd hh:mm:ssZ}");

            // Verify it has been created
            versionFound = SampleDatabase.WorkflowContext.StreamProvider.LatestBeforeCutoff(LocationType.Table, "WebRequest.Authenticated", cutoff).WhenModifiedUtc;
            Assert.AreEqual(new DateTime(2017, 12, 02, 00, 00, 00, DateTimeKind.Utc), versionFound);
        }

        [TestMethod]
        public void Database_RunAll()
        {
            SampleDatabase.EnsureBuilt();

            // XForm build each source
            foreach(string sourceName in SampleDatabase.WorkflowContext.Runner.SourceNames)
            {
                XForm($"build {PipelineScanner.Escape(sourceName)}", ExpectedResult(sourceName));
            }
        }

        private static int ExpectedResult(string sourceName)
        {
            if (sourceName.StartsWith("UsageError.")) return -2;
            if (sourceName.StartsWith("Error.")) return -1;
            return 0;
        }
    }
}

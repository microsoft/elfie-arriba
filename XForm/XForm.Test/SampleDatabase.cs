// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

using Elfie.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Extensions;
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Query;

namespace XForm.Test
{
    [TestClass]
    public class SampleDatabase
    {
        private static object s_locker = new object();
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
            lock (s_locker)
            {
                if (s_RootPath == null || !Directory.Exists(s_RootPath)) Build();
            }
        }

        public static void Build()
        {
            if (s_RootPath == null) s_RootPath = Path.Combine(Environment.CurrentDirectory, "Database");
            DirectoryIO.DeleteAllContents(s_RootPath);

            // Unpack the sample database
            ZipFile.ExtractToDirectory("SampleDatabase.zip", s_RootPath);

            // XForm add each source
            foreach (string filePath in Directory.GetFiles(Path.Combine(s_RootPath, "_Raw")))
            {
                Add(filePath);
            }

            foreach (string folderPath in Directory.GetDirectories(Path.Combine(s_RootPath, "_Raw")))
            {
                Add(folderPath);
            }

            // Add the sample configs and queries
            DirectoryIO.Copy(Path.Combine(Environment.CurrentDirectory, "SampleDatabase"), s_RootPath);
        }

        public static void Add(string filePath)
        {
            // Split the Table Name and AsOfDateTime from the sample data naming scheme (WebRequest.20171201....)
            string fileName = Path.GetFileName(filePath);
            string[] fileNameParts = fileName.Split('.');
            string tableName = fileNameParts[0];
            string sourceType = fileNameParts[1];
            DateTime asOfDateTime = DateTime.ParseExact(fileNameParts[2], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            XForm($@"add ""{filePath}"" ""{tableName}"" {sourceType} ""{asOfDateTime}""");
            string expectedPath = Path.Combine(s_RootPath, "Source", tableName, sourceType, asOfDateTime.ToString(StreamProviderExtensions.DateTimeFolderFormat));
            Assert.IsTrue(Directory.Exists(expectedPath), $"XForm add didn't add to expected location {expectedPath}");
        }

        public static void XForm(string xformCommand, int expectedExitCode = 0, WorkflowContext context = null)
        {
            if (context == null)
            {
                context = SampleDatabase.WorkflowContext;

                // Ensure the as-of DateTime is reset for each operation
                context.RequestedAsOfDateTime = DateTime.UtcNow;
            }

            List<string> args = new List<string>();
            XqlScanner scanner = new XqlScanner(xformCommand);
            while (scanner.Current.Type != TokenType.End)
            {
                if (scanner.Current.Type == TokenType.Newline) break;
                args.Add(scanner.Current.Value);
                scanner.Next();
            }

            int result = Program.Run(args.ToArray(), context);
            Assert.AreEqual(expectedExitCode, result, $"Unexpected Exit Code for XForm {xformCommand}");
        }

        [TestMethod]
        public void Database_Function()
        {
            SampleDatabase.EnsureBuilt();

            XqlParser.Parse(@"
                read WebRequest
                set [ClientOsUpper] Trim(ToUpper([ClientOs]))
                set [Sample] ToUpper(Trim(""  Sample  ""))
                assert none
                    where [Sample] != ""SAMPLE""", null, SampleDatabase.WorkflowContext).RunAndDispose();
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
            DateTime versionFound = SampleDatabase.WorkflowContext.StreamProvider.LatestBeforeCutoff(LocationType.Table, "WebRequest", CrawlType.Full, cutoff).WhenModifiedUtc;
            Assert.AreEqual(new DateTime(2017, 12, 02, 00, 00, 00, DateTimeKind.Utc), versionFound);

            // Ask for WebRequest.Authenticated. Verify a 2017-12-02 version is also built for it
            XForm($"build WebRequest.Authenticated xform \"{cutoff:yyyy-MM-dd hh:mm:ssZ}");

            // Verify it has been created
            versionFound = SampleDatabase.WorkflowContext.StreamProvider.LatestBeforeCutoff(LocationType.Table, "WebRequest.Authenticated", CrawlType.Full, cutoff).WhenModifiedUtc;
            Assert.AreEqual(new DateTime(2017, 12, 02, 00, 00, 00, DateTimeKind.Utc), versionFound);
        }

        [TestMethod]
        public void Database_ReadRange()
        {
            SampleDatabase.EnsureBuilt();

            // Asking for 2d from 2017-12-04 should get 2017-12-03 and 2017-12-02 crawls
            WorkflowContext historicalContext = new WorkflowContext(SampleDatabase.WorkflowContext) { RequestedAsOfDateTime = new DateTime(2017, 12, 04, 00, 00, 00, DateTimeKind.Utc) };
            Assert.AreEqual(2000, XqlParser.Parse("readRange 2d WebRequest", null, historicalContext).RunAndDispose());

            // Asking for 3d should get all three crawls
            Assert.AreEqual(3000, XqlParser.Parse("readRange 3d WebRequest", null, historicalContext).RunAndDispose());

            // Asking for 4d should error (no version for the range start)
            Verify.Exception<UsageException>(() => XqlParser.Parse("readRange 4d WebRequest", null, historicalContext).RunAndDispose());
        }

        [TestMethod]
        public void Database_Report()
        {
            SampleDatabase.EnsureBuilt();
            StreamAttributes latestReport;

            // Build WebRequest.tsv. Verify it's a 2017-12-03 version. Verify the TSV is found
            XForm($"build WebRequest tsv");
            latestReport = SampleDatabase.WorkflowContext.StreamProvider.LatestBeforeCutoff(LocationType.Report, "WebRequest", CrawlType.Full, DateTime.UtcNow);
            Assert.AreEqual(new DateTime(2017, 12, 03, 00, 00, 00, DateTimeKind.Utc), latestReport.WhenModifiedUtc);
            Assert.IsTrue(SampleDatabase.WorkflowContext.StreamProvider.Attributes(Path.Combine(latestReport.Path, "Report.tsv")).Exists);

            // Ask for a 2017-12-02 report. Verify 2017-12-02 version is created
            DateTime cutoff = new DateTime(2017, 12, 02, 11, 50, 00, DateTimeKind.Utc);
            XForm($"build WebRequest tsv \"{cutoff:yyyy-MM-dd hh:mm:ssZ}");
            latestReport = SampleDatabase.WorkflowContext.StreamProvider.LatestBeforeCutoff(LocationType.Report, "WebRequest", CrawlType.Full, cutoff);
            Assert.AreEqual(new DateTime(2017, 12, 02, 00, 00, 00, DateTimeKind.Utc), latestReport.WhenModifiedUtc);
            Assert.IsTrue(SampleDatabase.WorkflowContext.StreamProvider.Attributes(Path.Combine(latestReport.Path, "Report.tsv")).Exists);
        }

        [TestMethod]
        public void Database_BranchedScenario()
        {
            SampleDatabase.EnsureBuilt();

            // Make a branch of the database in "Database.Branched"
            string branchedFolder = Path.Combine(s_RootPath, @"..\Database.Branched");
            IStreamProvider branchedStreamProvider = new LocalFileStreamProvider(branchedFolder);
            branchedStreamProvider.Delete(".");

            IStreamProvider mainStreamProvider = SampleDatabase.WorkflowContext.StreamProvider;

            WorkflowContext branchedContext = new WorkflowContext(SampleDatabase.WorkflowContext);
            branchedContext.StreamProvider = new MultipleSourceStreamProvider(branchedStreamProvider, branchedContext.StreamProvider, MultipleSourceStreamConfiguration.LocalBranch);
            branchedContext.Runner = new WorkflowRunner(branchedContext);

            // Ask for WebRequest in the main database; verify built
            XForm("build WebRequest", 0);
            Assert.IsTrue(mainStreamProvider.Attributes("Table\\WebRequest").Exists);

            // Ask for WebRequest in the branch. Verify the main one is loaded where it is
            XForm("build WebRequest", 0, branchedContext);
            Assert.IsFalse(branchedStreamProvider.Attributes("Table\\WebRequest").Exists);

            // Ask for WebRequest.Authenticated in the main database; verify built
            XForm("build WebRequest.Authenticated", 0);
            Assert.IsTrue(mainStreamProvider.Attributes("Table\\WebRequest.Authenticated").Exists);
            Assert.IsFalse(branchedStreamProvider.Attributes("Table\\WebRequest.Authenticated").Exists);

            // Make a custom query in the branch. Verify the branched source has a copy with the new query, but it isn't published back
            string webRequestAuthenticatedConfigNew = @"
                read WebRequest

                # Slightly different query
                where [UserName] != ""
                where [UserName] != null";

            branchedStreamProvider.WriteAllText("Config\\WebRequest.Authenticated.xql", webRequestAuthenticatedConfigNew);
            XForm("build WebRequest.Authenticated", 0, branchedContext);
            Assert.IsTrue(branchedStreamProvider.Attributes("Table\\WebRequest.Authenticated").Exists);
            Assert.AreEqual(webRequestAuthenticatedConfigNew, ((BinaryTableReader)branchedContext.Runner.Build("WebRequest.Authenticated", branchedContext)).Query);
            Assert.AreNotEqual(webRequestAuthenticatedConfigNew, ((BinaryTableReader)SampleDatabase.WorkflowContext.Runner.Build("WebRequest.Authenticated", SampleDatabase.WorkflowContext)).Query);
        }

        [TestMethod]
        public void Database_IncrementalSources()
        {
            SampleDatabase.EnsureBuilt();

            WorkflowContext reportContext = new WorkflowContext(SampleDatabase.WorkflowContext);

            // Build WebServer as of 2017-11-25; should have 86 original rows, verify built copy cached
            reportContext.NewestDependency = DateTime.MinValue;
            reportContext.RequestedAsOfDateTime = new DateTime(2017, 11, 25, 00, 00, 00, DateTimeKind.Utc);
            Assert.AreEqual(86, SampleDatabase.WorkflowContext.Runner.Build("WebServer", reportContext).RunAndDispose());
            Assert.AreEqual(new DateTime(2017, 11, 25, 00, 00, 00, DateTimeKind.Utc), SampleDatabase.WorkflowContext.StreamProvider.LatestBeforeCutoff(LocationType.Table, "WebServer", CrawlType.Full, DateTime.UtcNow).WhenModifiedUtc);

            // Build WebServer as of 2017-11-27; should have 91 rows (one incremental added)
            reportContext.NewestDependency = DateTime.MinValue;
            reportContext.RequestedAsOfDateTime = new DateTime(2017, 11, 27, 00, 00, 00, DateTimeKind.Utc);
            Assert.AreEqual(91, SampleDatabase.WorkflowContext.Runner.Build("WebServer", reportContext).RunAndDispose());
            Assert.AreEqual(new DateTime(2017, 11, 27, 00, 00, 00, DateTimeKind.Utc), SampleDatabase.WorkflowContext.StreamProvider.LatestBeforeCutoff(LocationType.Table, "WebServer", CrawlType.Full, DateTime.UtcNow).WhenModifiedUtc);

            // Build WebServer as of 2017-11-29; should have 96 rows (two incrementals added)
            reportContext.NewestDependency = DateTime.MinValue;
            reportContext.RequestedAsOfDateTime = new DateTime(2017, 11, 29, 00, 00, 00, DateTimeKind.Utc);
            Assert.AreEqual(96, SampleDatabase.WorkflowContext.Runner.Build("WebServer", reportContext).RunAndDispose());
            Assert.AreEqual(new DateTime(2017, 11, 29, 00, 00, 00, DateTimeKind.Utc), SampleDatabase.WorkflowContext.StreamProvider.LatestBeforeCutoff(LocationType.Table, "WebServer", CrawlType.Full, DateTime.UtcNow).WhenModifiedUtc);
        }

        [TestMethod]
        public void Database_RunAll()
        {
            SampleDatabase.EnsureBuilt();

            // XForm build each source
            foreach (string sourceName in SampleDatabase.WorkflowContext.Runner.SourceNames)
            {
                XForm($"build {XqlScanner.Escape(sourceName, TokenType.Value)}", ExpectedResult(sourceName));
            }

            // When one fails, put it by itself in the test below to debug
        }

        [TestMethod]
        public void Database_TryOne()
        {
            SampleDatabase.EnsureBuilt();

            // To debug Main() error handling or argument parsing, run like this:
            //XForm("build WebRequest.NullableHandling");

            // To debug engine execution, run like this:
            XqlParser.Parse("read WebRequest.NullableHandling", null, SampleDatabase.WorkflowContext).RunAndDispose();
        }

        private static int ExpectedResult(string sourceName)
        {
            if (sourceName.StartsWith("UsageError.")) return -2;
            if (sourceName.StartsWith("Error.")) return -1;
            return 0;
        }
    }
}

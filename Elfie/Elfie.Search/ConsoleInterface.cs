// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Indexer;
using Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.PDB;
using Microsoft.CodeAnalysis.Elfie.Search.VisualStudio;

namespace Microsoft.CodeAnalysis.Elfie.Search
{
    public class ConsoleInterface : IDisposable
    {
        private const string TitleFormatString = "ELFIE {0}, Copyright Microsoft 2016";

        private string Title { get; set; }
        private string LoadStatus { get; set; }
        private string RebuildStatus { get; set; }
        private string QueryStatus { get; set; }

        private SearchOptions Options { get; set; }
        private string Query { get; set; }
        private string Command { get; set; }

        private object _queryLock;
        private object _drawLock;
        private Position Start { get; set; }
        private Position End { get; set; }
        private Position CommandEnd { get; set; }

        private IMemberDatabase Database { get; set; }
        private MemberQuery ParsedQuery { get; set; }
        private PartialArray<Symbol> _lastResult;

        private IList<string> TemporaryFiles { get; set; }

        // TEMP: F12 demo
        private bool IsF12Demo { get; set; }
        private RoslynReferencesWrapper References { get; set; }

        public ConsoleInterface(SearchOptions options)
        {
            this.Options = options;

            // TEMP: F12 Demo
            string databaseFolderName = Path.GetFileNameWithoutExtension(Options.DatabasePath);
            this.IsF12Demo = databaseFolderName.Equals("Debug", StringComparison.OrdinalIgnoreCase) || databaseFolderName.Equals("Release", StringComparison.OrdinalIgnoreCase);

            TemporaryFiles = new List<string>();

            Title = String.Format(TitleFormatString, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3));

            _queryLock = new object();
            _drawLock = new object();
            Start = new Position();
            End = new Position();
            CommandEnd = new Position();

            ParsedQuery = new MemberQuery("", false, false);
            _lastResult = new PartialArray<Symbol>(options.ResultCount);
        }

        public int Run()
        {
            Start.Save();

            if (!String.IsNullOrEmpty(Options.Query))
            {
                RunCommandLineSearch();
                return _lastResult.Count;
            }

            // Capture Trace.WriteLine calls to show as status
            ConsoleProgressTraceListener listener = new ConsoleProgressTraceListener(this);
            Trace.Listeners.Add(listener);

            // Start the index loading
            Thread IndexThread = new Thread(LoadOrIndex);
            IndexThread.SetApartmentState(ApartmentState.STA);
            IndexThread.IsBackground = true;
            IndexThread.Start();

            // Start reading keystrokes
            while (Read())
            {
                RunQuery();
                Draw();
            }

            // Skip to the end of previous output on quit
            End.Restore();
            if (CommandEnd.CompareTo(End) > 0) CommandEnd.Restore();

            Console.WriteLine();
            return _lastResult.Count;
        }

        public void RunCommandLineSearch()
        {
            LoadOrIndex();
            Query = Options.Query;
            RunQuery();
            DrawCommandLineQueryResults();
        }

        private void LoadOrIndex()
        {
            LoadReferences();

            if (File.Exists(Options.DatabasePath))
            {
                Load();
            }

            if (this.Database != null)
            {
                if (Options.DatabasePath.StartsWith(Environment.CurrentDirectory))
                {
                    RebuildStatus = "Checking for Changes";
                    Draw();

                    if (JustMyCodeBinaryFinder.AreJustMyCodeBinariesNewerThan(Environment.CurrentDirectory, File.GetLastWriteTimeUtc(Options.DatabasePath)))
                    {
                        RebuildStatus = "Out of Date";
                        Index();
                        RebuildStatus = "Up to Date";
                        Load();
                    }
                    else
                    {
                        RebuildStatus = "Up to Date";
                        Draw();
                    }
                }
            }
            else
            {
                if (!File.Exists(Options.DatabasePath)) RebuildStatus = "No Index";

                Index();
                RebuildStatus = "Up to Date";
                Load();
            }
        }

        private void LoadReferences()
        {
            // F12 Demo. Need a nicer story for identifying when to load references.
            if (this.IsF12Demo)
            {
                LoadStatus = "Building Go To Definition Compilation...";
                Draw();

                string referencesDirectory = Path.GetFullPath(Path.Combine(Options.DatabasePath, "..\\.."));

                List<string> referencePaths = new List<string>();
                referencePaths.Add(typeof(object).Assembly.Location);
                referencePaths.AddRange(Directory.GetFiles(referencesDirectory, "*.dll"));
                referencePaths.AddRange(Directory.GetFiles(referencesDirectory, "*.exe"));

                this.References = new RoslynReferencesWrapper(referencePaths);
            }
        }

        private void Load()
        {
            LoadStatus = "Loading Index...";
            Draw();

            try
            {
                IMemberDatabase db = null;
                MeasureDiagnostics diagnostics = Memory.Measure(() =>
                {
                    db = MemberDatabase.Load(Options.DatabasePath);
                    return db;
                });

                lock (_queryLock)
                {
                    Database = db;
                }

                LoadStatus = String.Format("Loaded {0:n0} members in {1} in {2}.", db.Count, diagnostics.MemoryUsedBytes.SizeString(), diagnostics.LoadTime.ToFriendlyString());
            }
            catch (IOException ex)
            {
                LoadStatus = "";
                RebuildStatus = String.Format("Error loading: {0}.", ex.Message);
                return;
            }

            RunQuery();
            Draw();
        }

        private void Index()
        {
            DateTime previousDatabaseWriteTimeUtc = default(DateTime);
            if (this.Database != null) previousDatabaseWriteTimeUtc = File.GetLastWriteTimeUtc(Options.DatabasePath);

            IndexOptions indexerOptions = new IndexOptions() { PathToIndex = Environment.CurrentDirectory, IsFull = true, IncludeSymbolCacheIndices = this.IsF12Demo, PreviousDatabase = this.Database, PreviousDatabaseWriteTimeUtc = previousDatabaseWriteTimeUtc };
            PackageDatabase db = Elfie.Indexer.IndexCommand.Index(indexerOptions);
            db.FileWrite(Options.DatabasePath);
        }

        public bool Read()
        {
            ConsoleKeyInfo ki = Console.ReadKey(true);

            switch (ki.Key)
            {
                case ConsoleKey.Q:
                    {
                        if ((ConsoleModifiers.Control & ki.Modifiers) == ConsoleModifiers.Control)
                        {
                            return false;
                        }
                        Query += ki.KeyChar;
                        break;
                    }

                case ConsoleKey.Delete:
                    {
                        Query = String.Empty;
                        break;
                    }
                case ConsoleKey.Backspace:
                    {
                        if (Query.Length > 0) Query = Query.Substring(0, Query.Length - 1);
                        break;
                    }
                case ConsoleKey.OemPeriod:
                case ConsoleKey.Tab:
                    {
                        AutoCompleteCurrentSuffix(ki.KeyChar);
                        break;
                    }
                case ConsoleKey.Enter:
                    {
                        End.Restore();

                        OpenFirstMatch();
                        break;
                    }

                case ConsoleKey.F12:
                    {
                        End.Restore();

                        GoToDefinition();
                        break;
                    }
                case ConsoleKey.Escape:
                    {
                        return false;
                    }
                default:
                    {
                        if (char.IsLetterOrDigit(ki.KeyChar))
                        {
                            Query += ki.KeyChar;
                        }
                        break;
                    }
            }

            return true;
        }

        private void AutoCompleteCurrentSuffix(char character)
        {
            if (_lastResult.Count > 0)
            {
                int indexOfLastDot = Query.LastIndexOf('.');
                if (indexOfLastDot >= 0)
                {
                    Query = Query.Substring(0, indexOfLastDot + 1);
                }
                else
                {
                    Query = "";
                }

                Query = Query + _lastResult[0].Name.ToString() + ".";
            }
            else
            {
                Query = Query + character;
            }
        }

        private void OpenFirstMatch()
        {
            if (!String.IsNullOrEmpty(Query) && _lastResult.Count > 0)
            {
                OpenSymbol(_lastResult[0]);
            }
        }

        private void OpenSymbol(Symbol matchToOpen)
        {
            string localPath = SourceFileMap.GetLocalPath(matchToOpen);

            if (String.IsNullOrEmpty(localPath))
            {
                Console.WriteLine("ERROR: File Path unknown for '{0}'.", matchToOpen.Name.ToString());
            }
            else
            {
                Console.WriteLine("OPEN: {0}({1})", localPath, matchToOpen.Line);
                VS.OpenFileToLine(localPath, matchToOpen.Line, Options.DatabasePath);
            }
        }

        private void GoToDefinition()
        {
            Position current = new Position();
            current.Save();
            current.ClearUpTo(CommandEnd);

            Console.WriteLine();

            LocationWithinFile p = VS.GetCurrentLocation(Options.DatabasePath);
            if (p == null)
            {
                Console.WriteLine("Unable to find current cursor position.");
                return;
            }
            else
            {
                Console.WriteLine("Finding Symbol at {0}({1}, {2})", p.FilePath, p.Line, p.CharInLine);
            }

            RoslynDefinitionFinder finder = new RoslynDefinitionFinder(p.FilePath, this.References);
            MemberQuery query = finder.BuildQueryForMemberUsedAt(p.Line, p.CharInLine);
            query.IgnoreCase = Options.IgnoreCase;

            if (query == null)
            {
                Console.WriteLine("Unable to identify symbol.");
                return;
            }

            Console.Write("Roslyn identified as {0}", query.SymbolName);
            if (query.Parameters.Length > 0) Console.Write("({0})", query.Parameters);
            Console.WriteLine();

            PartialArray<Symbol> results = new PartialArray<Symbol>(1);
            if (query.TryFindMembers(this.Database, ref results))
            {
                OpenSymbol(results[0]);
            }
            else
            {
                Console.WriteLine("NOT FOUND");
            }

            CommandEnd.Save();
        }

        private void RunQuery()
        {
            lock (_queryLock)
            {
                if (Database == null) return;
                if (String.IsNullOrEmpty(Query) && _lastResult.Count == 0) return;

                Stopwatch w = Stopwatch.StartNew();
                ParsedQuery.SymbolName = Query;
                ParsedQuery.IgnoreCase = Options.IgnoreCase;
                ParsedQuery.TryFindMembers(Database, ref _lastResult);
                w.Stop();

                QueryStatus = String.Format("{0:n0} matches in {1} for \"{2}\"", _lastResult.Count, w.Elapsed.ToFriendlyString(), Query);
            }
        }

        public void Draw()
        {
            // Suppress progress output for command line queries
            if (Options.CommandLineQuery) return;

            lock (_drawLock)
            {
                Position queryEnd = new Position();
                Start.ClearUpTo(End);

                StringBuilder heading = new StringBuilder();
                heading.Append(Title);
                heading.Append("  [");
                heading.Append(RebuildStatus);
                if (!String.IsNullOrEmpty(RebuildStatus)) heading.Append("; ");
                heading.Append(LoadStatus);
                heading.AppendLine("] ");
                heading.Append(" > ");
                heading.Append(Query);
                Console.Write(heading.ToString());

                queryEnd.Save();

                DrawResults();

                End.Save();
                queryEnd.Restore();
            }
        }

        public void DrawResults()
        {
            // Don't write anything when database is still loading
            if (_lastResult.Count == 0 || Database == null) return;

            ConsoleHighlighter.WriteWithHighlight(Environment.NewLine + "--- " + QueryStatus + " ---" + Environment.NewLine, ParsedQuery.SymbolName);

            // Write to StringBuilder and then copy to Console in one call (much faster perceived performance)
            StringBuilder output = new StringBuilder();
            using (StringWriter writer = new StringWriter(output))
            {
                ResultFormatter.WriteMatchesInFlatFormat(writer, _lastResult);
                //ResultFormatter.WriteMatchesInTreeFormat(writer, _lastResult, Database);
                //ResultFormatter.WriteMatchesIndentedUnderContainers(writer, _lastResult);
            }

            ResultIndenter indenter = new ResultIndenter(output.ToString());
            ConsoleHighlighter.WriteWithHighlight(indenter.WriteAligned(), ParsedQuery.SymbolName);
        }

        public void DrawCommandLineQueryResults()
        {
            // Don't write anything when database is still loading
            if (_lastResult.Count == 0 || Database == null) return;

            ConsoleHighlighter.WriteWithHighlight("--- " + QueryStatus + " in " + Options.DatabasePath + " ---" + Environment.NewLine, ParsedQuery.SymbolName);

            // Write to StringBuilder and then copy to Console in one call (much faster perceived performance)
            StringBuilder output = new StringBuilder();
            using (StringWriter writer = new StringWriter(output))
            {
                ResultFormatter.WriteMatchesForToolUse(writer, _lastResult);
            }

            Console.Write(output.ToString());
        }

        public void Dispose()
        {
            if (this.TemporaryFiles != null)
            {
                foreach (string fileName in TemporaryFiles)
                {
                    if (File.Exists(fileName))
                    {
                        try
                        {
                            File.Delete(fileName);
                        }
                        catch (IOException) { }
                        catch (SecurityException) { }
                        catch (UnauthorizedAccessException) { }
                    }
                }
            }
        }

        internal class ConsoleProgressTraceListener : TraceListener
        {
            private ConsoleInterface _interface;

            public ConsoleProgressTraceListener(ConsoleInterface i)
            {
                _interface = i;
                Trace.Listeners.Add(this);
            }

            public override void Write(string message)
            {
                // Partial Line logging (like progress) isn't captured
            }

            public override void WriteLine(string message)
            {
                _interface.LoadStatus = message.Trim();
                _interface.Draw();
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Extensions;

using XForm.Context;
using XForm.Data;
using XForm.Extensions;
using XForm.IO.StreamProvider;
using XForm.Query;

namespace XForm
{
    public class InteractiveRunner : IWorkflowRunner
    {
        private static string s_commandCachePath;
        private XDatabaseContext _xDatabaseContext;
        private IXTable _pipeline;
        private List<IXTable> _stages;
        private List<string> _commands;

        public IEnumerable<string> SourceNames => _xDatabaseContext.Runner.SourceNames;

        public IXTable Build(string sourceName, XDatabaseContext context)
        {
            return _xDatabaseContext.Runner.Build(sourceName, context);
        }

        public void Save(string query, string saveToPath)
        {
            _xDatabaseContext.Runner.Save(query, saveToPath);
        }

        public InteractiveRunner(XDatabaseContext context)
        {
            s_commandCachePath = Environment.ExpandEnvironmentVariables(@"%TEMP%\XForm.Last.xql");
            _xDatabaseContext = context;

            _pipeline = null;
            _stages = new List<IXTable>();
            _commands = new List<string>();
        }

        public long Run()
        {
            long lastCount = 0;

            try
            {
                while (true)
                {
                    Console.Write("> ");

                    // Read the next query line
                    string nextLine = Console.ReadLine();

                    Stopwatch w = Stopwatch.StartNew();
                    try
                    {
                        if (String.IsNullOrEmpty(nextLine)) return lastCount;

                        string[] parts = nextLine.Split(' ');
                        string command = parts[0].ToLowerInvariant();
                        switch (command)
                        {
                            case "quit":
                            case "exit":
                                // Stop on empty, "quit", or "exit"
                                return lastCount;


                            case "back":
                            case "undo":
                                // Unwrap on "back" or "undo"
                                IXTable last = _stages.LastOrDefault();
                                if (last != null)
                                {
                                    _pipeline = last;
                                    _stages.RemoveAt(_stages.Count - 1);
                                    _commands.RemoveAt(_commands.Count - 1);
                                }

                                break;
                            case "save":
                                string tableName = parts[1];
                                string queryPath = _xDatabaseContext.StreamProvider.Path(LocationType.Query, tableName, ".xql");
                                _xDatabaseContext.StreamProvider.WriteAllText(queryPath, String.Join(Environment.NewLine, _commands));
                                Console.WriteLine($"Query saved to \"{tableName}\".");


                                _commands.Clear();
                                _commands.Add($"read \"{tableName}\"");
                                _pipeline = null;
                                _pipeline = AddStage(_commands[0]);

                                break;
                            case "run":
                                LoadScript(parts[1]);
                                break;
                            case "rerun":
                                LoadScript(s_commandCachePath);
                                break;
                            default:
                                try
                                {
                                    _pipeline = AddStage(nextLine);
                                    break;
                                }
                                catch (Exception ex) when (!Debugger.IsAttached)
                                {
                                    Console.WriteLine($"Error: {ex.Message}");
                                    continue;
                                }
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }

                    SaveScript(s_commandCachePath);

                    // Get the first 10 results and 10 columns
                    IXTable firstTenWrapper = _pipeline;
                    firstTenWrapper = _xDatabaseContext.Query("limit 10 10", firstTenWrapper);
                    firstTenWrapper = _xDatabaseContext.Query("write cout", firstTenWrapper);
                    lastCount = firstTenWrapper.Count();

                    // Get the count
                    RunResult result = _pipeline.RunUntilTimeout(TimeSpan.FromSeconds(3));
                    lastCount += result.RowCount;
                    firstTenWrapper.Reset();

                    Console.WriteLine();
                    Console.WriteLine($"{lastCount:n0} rows in {w.Elapsed.ToFriendlyString()}. {(result.IsComplete ? "" : "[incomplete]")}");
                    Console.WriteLine();
                }
            }
            finally
            {
                if (_pipeline != null) _pipeline.Dispose();
            }
        }

        private void LoadScript(string path)
        {
            foreach (string line in File.ReadAllLines(path))
            {
                Console.WriteLine(line);
                _pipeline = AddStage(line);
            }
        }

        private void SaveScript(string path)
        {
            string folder = Path.GetDirectoryName(path);
            if (!String.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            File.WriteAllLines(path, _commands);
        }

        private IXTable AddStage(string nextLine)
        {
            // Save stages before the last one
            _stages.Add(_pipeline);

            // Build the new stage
            _pipeline = _xDatabaseContext.Query(nextLine, _pipeline);

            // Save the current command set
            _commands.Add(nextLine);
            return _pipeline;
        }
    }
}

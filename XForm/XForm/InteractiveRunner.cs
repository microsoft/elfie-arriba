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
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Query;

namespace XForm
{
    public class InteractiveRunner : IWorkflowRunner
    {
        private static string s_commandCachePath;
        private WorkflowContext _workflowContext;
        private IDataBatchEnumerator _pipeline;
        private List<IDataBatchEnumerator> _stages;
        private List<string> _commands;

        public IEnumerable<string> SourceNames => _workflowContext.Runner.SourceNames;

        public IDataBatchEnumerator Build(string sourceName, WorkflowContext context)
        {
            return _workflowContext.Runner.Build(sourceName, context);
        }

        public InteractiveRunner(WorkflowContext context)
        {
            s_commandCachePath = Environment.ExpandEnvironmentVariables(@"%TEMP%\XForm.Last.xql");
            _workflowContext = context;

            _pipeline = null;
            _stages = new List<IDataBatchEnumerator>();
            _commands = new List<string>();
        }

        public int Run()
        {
            int lastCount = 0;

            try
            {
                while (true)
                {
                    Console.Write("> ");

                    // Read the next query line
                    string nextLine = Console.ReadLine();
                    XqlParser parser = null;
                    try
                    {
                        parser = new XqlParser(nextLine, _workflowContext);
                        if (!parser.HasAnotherPart) return lastCount;

                        string command = parser.NextString().ToLowerInvariant();
                        switch (command)
                        {
                            case "quit":
                            case "exit":
                                // Stop on empty, "quit", or "exit"
                                return lastCount;


                            case "back":
                            case "undo":
                                // Unwrap on "back" or "undo"
                                IDataBatchEnumerator last = _stages.LastOrDefault();
                                if (last != null)
                                {
                                    _pipeline = last;
                                    _stages.RemoveAt(_stages.Count - 1);
                                    _commands.RemoveAt(_commands.Count - 1);
                                }

                                break;
                            case "save":
                                string tableName = parser.NextOutputTableName();
                                string queryPath = _workflowContext.StreamProvider.Path(LocationType.Query, tableName, ".xql");
                                _workflowContext.StreamProvider.WriteAllText(queryPath, String.Join(Environment.NewLine, _commands));
                                Console.WriteLine($"Query saved to \"{tableName}\".");


                                _commands.Clear();
                                _commands.Add($"read \"{tableName}\"");
                                _pipeline = null;
                                _pipeline = AddStage(_commands[0]);

                                break;
                            case "run":
                                LoadScript(parser.NextString());
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
                    Stopwatch w = Stopwatch.StartNew();

                    // Get the first 10 results
                    IDataBatchEnumerator firstTenWrapper = _pipeline;
                    firstTenWrapper = XqlParser.Parse("limit 10", firstTenWrapper, _workflowContext);
                    firstTenWrapper = XqlParser.Parse("write cout", firstTenWrapper, _workflowContext);
                    lastCount = firstTenWrapper.Run();

                    // Get the count
                    lastCount += _pipeline.Run();
                    firstTenWrapper.Reset();

                    Console.WriteLine();
                    Console.WriteLine($"{lastCount:n0} rows in {w.Elapsed.ToFriendlyString()}.");
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

        private IDataBatchEnumerator AddStage(string nextLine)
        {
            // Save stages before the last one
            _stages.Add(_pipeline);

            // Build the new stage
            _pipeline = XqlParser.Parse(nextLine, _pipeline, _workflowContext);

            // Save the current command set
            _commands.Add(nextLine);
            return _pipeline;
        }
    }
}

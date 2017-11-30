// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Extensions;

using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;

namespace XForm
{
    public class InteractiveRunner : IWorkflowRunner
    {
        private static string s_commandCachePath;
        private IDataBatchEnumerator _pipeline;
        private List<IDataBatchEnumerator> _stages;
        private List<string> _commands;
        private WorkflowRunner _workflowRunner;

        public IEnumerable<string> SourceNames => _workflowRunner.SourceNames;

        public InteractiveRunner(WorkflowRunner workflowRunner)
        {
            s_commandCachePath = Environment.ExpandEnvironmentVariables(@"%TEMP%\XForm.Last.xql");

            _pipeline = null;
            _stages = new List<IDataBatchEnumerator>();
            _commands = new List<string>();
            _workflowRunner = workflowRunner;
        }

        public IDataBatchEnumerator Build(string sourceName, WorkflowContext context)
        {
            if (File.Exists(sourceName))
            {
                if (sourceName.EndsWith("xform") || Directory.Exists(sourceName))
                {
                    return new BinaryTableReader(sourceName);
                }
                else
                {
                    return new TabularFileReader(sourceName);
                }
            }
            else
            {
                return _workflowRunner.Build(sourceName, context);
            }
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
                    PipelineParser parser = null;
                    try
                    {
                        parser = new PipelineParser(nextLine, new WorkflowContext(this));


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
                                _workflowRunner.SaveXql(LocationType.Query, tableName, String.Join(Environment.NewLine, _commands));
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
                    firstTenWrapper = PipelineParser.BuildStage("limit 10", firstTenWrapper);
                    firstTenWrapper = PipelineParser.BuildStage("write cout", firstTenWrapper);
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
            _pipeline = PipelineParser.BuildStage(nextLine, _pipeline, new WorkflowContext(this));

            // Save the current command set
            _commands.Add(nextLine);
            return _pipeline;
        }
    }
}

using Microsoft.CodeAnalysis.Elfie.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using XForm.Data;
using XForm.Extensions;

namespace XForm
{
    public class InteractiveRunner
    {
        private static string commandCachePath;
        private IDataBatchEnumerator pipeline;
        private List<IDataBatchEnumerator> stages;
        private List<string> commands;

        public InteractiveRunner()
        {
            commandCachePath = Environment.ExpandEnvironmentVariables(@"%TEMP%\XForm.Last.xql");

            pipeline = null;
            stages = new List<IDataBatchEnumerator>();
            commands = new List<string>();
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
                    List<string> arguments = PipelineFactory.SplitConfigurationLine(nextLine);
                    if (arguments.Count == 0) return lastCount;

                    string command = arguments[0].ToLowerInvariant();
                    switch (command)
                    {
                        case "quit":
                        case "exit":
                            // Stop on empty, "quit", or "exit"
                            return lastCount;


                        case "back":
                        case "undo":
                            // Unwrap on "back" or "undo"
                            IDataBatchEnumerator last = stages.LastOrDefault();
                            if (last != null)
                            {
                                pipeline = last;
                                stages.RemoveAt(stages.Count - 1);
                                commands.RemoveAt(commands.Count - 1);
                            }

                            break;
                        case "save":
                            if (arguments.Count != 2)
                            {
                                Console.WriteLine($"Usage: 'save' [scriptFilePath]");
                                continue;
                            }
                            else
                            {
                                SaveScript(arguments[1]);
                                Console.WriteLine($"Script saved to \"{arguments[1]}\".");
                                continue;
                            }
                        case "run":
                            if (arguments.Count != 2)
                            {
                                Console.WriteLine($"Usage: 'run' [scriptFilePath]");
                                continue;
                            }
                            else
                            {
                                LoadScript(arguments[1]);
                                break;
                            }
                        case "rerun":
                            LoadScript(commandCachePath);
                            break;
                        default:
                            try
                            {
                                pipeline = AddStage(nextLine);
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error: {ex.Message}");
                                continue;
                            }
                    }

                    SaveScript(commandCachePath);
                    Stopwatch w = Stopwatch.StartNew();

                    // Get the first 10 results
                    IDataBatchEnumerator firstTenWrapper = pipeline;
                    firstTenWrapper = PipelineFactory.BuildStage("limit 10", firstTenWrapper);
                    firstTenWrapper = PipelineFactory.BuildStage("write cout", firstTenWrapper);
                    lastCount = firstTenWrapper.Run();

                    // Get the count
                    lastCount += pipeline.Run();
                    firstTenWrapper.Reset();

                    Console.WriteLine();
                    Console.WriteLine($"{lastCount:n0} rows in {w.Elapsed.ToFriendlyString()}.");
                    Console.WriteLine();
                }
            }
            finally
            {
                if (pipeline != null) pipeline.Dispose();
            }
        }

        private void LoadScript(string path)
        {
            foreach (string line in File.ReadAllLines(path))
            {
                Console.WriteLine(line);
                pipeline = AddStage(line);
            }
        }

        private void SaveScript(string path)
        {
            string folder = Path.GetDirectoryName(path);
            if (!String.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            File.WriteAllLines(path, commands);
        }

        private IDataBatchEnumerator AddStage(string nextLine)
        {
            // Save stages before the last one
            stages.Add(pipeline);

            // Build the new stage
            pipeline = PipelineFactory.BuildStage(nextLine, pipeline);

            // Save the current command set
            commands.Add(nextLine);
            return pipeline;
        }
    }
}

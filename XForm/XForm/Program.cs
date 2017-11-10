using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using XForm.Aggregators;
using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;
using XForm.Transforms;

namespace XForm
{
    class Program
    {
        static int Main(string[] args)
        {
            //TimingComparisons();
            //return 0;

            try
            {
                if(args.Length > 0)
                {
                    return RunFileQuery(args[0]);
                }
                else
                {
                    return RunInteractive();
                }
            }
            catch (ArgumentException ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine($"Usage: {ex.ToString()}");
                return -2;
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine($"Error: {ex.ToString()}");
                return -1;
            }
        }

        private static int RunFileQuery(string queryFilePath)
        {
            string query = File.ReadAllText(queryFilePath);

            int rowsWritten = 0;
            using (new TraceWatch(query))
            {
                using (IDataBatchEnumerator source = PipelineFactory.BuildPipeline(query))
                {
                    rowsWritten = source.Run();
                }
            }

            Console.WriteLine($"Done. {rowsWritten:n0} rows written.");
            return rowsWritten;
        }

        private static int RunInteractive()
        {
            int lastCount = 0;
            IDataBatchEnumerator pipeline = null;
            List<IDataBatchEnumerator> stages = new List<IDataBatchEnumerator>();
            List<string> commands = new List<string>();

            try
            {
                while (true)
                {
                    Console.Write("> ");

                    // Read the next query line
                    string nextLine = Console.ReadLine();
                    List<string> arguments = PipelineFactory.SplitConfigurationLine(nextLine);

                    string command = arguments[0].ToLowerInvariant();
                    switch(command)
                    {
                        case "":
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
                            if(arguments.Count != 2)
                            {
                                Console.WriteLine($"Error: Usage: save [scriptFilePath]");
                                continue;
                            }
                            else
                            {
                                File.WriteAllLines(arguments[1], commands);
                                Console.WriteLine($"Script saved to \"{arguments[1]}\".");
                                continue;
                            }
                        default:
                            try
                            {
                                // Save stages before the last one
                                stages.Add(pipeline);

                                // Build the new stage
                                pipeline = PipelineFactory.BuildStage(pipeline, nextLine);

                                // Save the current command set
                                commands.Add(nextLine);

                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error: {ex.Message}");
                                continue;
                            }
                    }

                    // Get the first 10 results
                    IDataBatchEnumerator resultToRun = pipeline;
                    resultToRun = PipelineFactory.BuildStage(resultToRun, "limit 10");
                    resultToRun = PipelineFactory.BuildStage(resultToRun, "write cout");
                    resultToRun.Run();
                    resultToRun.Reset();

                    // Get the count
                    resultToRun = pipeline;
                    lastCount = resultToRun.Run();
                    resultToRun.Reset();

                    Console.WriteLine();
                    Console.WriteLine($"{lastCount:n0} rows.");
                    Console.WriteLine();
                }
            }
            finally
            {
                if (pipeline != null) pipeline.Dispose();
            }
        }

        static void TimingComparisons()
        {
            int[] sample = new int[16 * 1024 * 1024];
            Random r = new Random();
            for (int i = 0; i < sample.Length; ++i)
            {
                sample[i] = r.Next(1000);
            }

            TimingComparisons(sample, 500);
        }
            
        static void TimingComparisons(int[] array, int value)
        {
            using (new TraceWatch($"For Loop [==]"))
            {
                int count = 0;
                for (int i = 0; i < array.Length; ++i)
                {
                    if (array[i] == value) count++;
                }

                Console.WriteLine($"Done. {count:n0} found.");
            }

            using (new TraceWatch($"For Loop [.CompareTo]"))
            {
                int count = 0;
                for (int i = 0; i < array.Length; ++i)
                {
                    if (value.CompareTo(array[i]) == 0) count++;
                }

                Console.WriteLine($"Done. {count:n0} found.");
            }

            using (new TraceWatch($"Linq Count [==]"))
            {
                int count = array.Where((i) => i == value).Count();
                Console.WriteLine($"Done. {count:n0} found.");
            }

            using (new TraceWatch($"Linq Count [.CompareTo]"))
            {
                int count = array.Where((i) => i.CompareTo(value) == 0).Count();
                Console.WriteLine($"Done. {count:n0} found.");
            }

            using (new TraceWatch($"XForm Count"))
            {
                ArrayReader table = new ArrayReader();
                table.AddColumn(new ColumnDetails("ID", typeof(int), false), DataBatch.All(array, array.Length));

                IDataBatchEnumerator source = table;
                source = new WhereFilter(source, "ID", CompareOperator.Equals, value);
                source = new CountAggregator(source);

                source.Next(10240);
                int count = (int)source.ColumnGetter(0)().Array.GetValue(0);
                Console.WriteLine($"Done. {count:n0} found.");
            }
        }
    }
}

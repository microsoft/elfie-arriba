// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Diagnostics;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using XForm.Aggregators;
using XForm.Commands;
using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;

namespace XForm
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            //TimingComparisons();
            //return 0;

            try
            {
                if (args.Length > 0)
                {
                    return RunFileQuery(args[0]);
                }
                else
                {
                    InteractiveRunner runner = new InteractiveRunner();
                    return runner.Run();
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



        private static void TimingComparisons()
        {
            int[] sample = new int[16 * 1024 * 1024];
            Random r = new Random();
            for (int i = 0; i < sample.Length; ++i)
            {
                sample[i] = r.Next(1000);
            }

            TimingComparisons(sample, 500);
        }

        private static void TimingComparisons(int[] array, int value)
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
                ArrayEnumerator table = new ArrayEnumerator();
                table.AddColumn(new ColumnDetails("ID", typeof(int), false), DataBatch.All(array, array.Length));

                IDataBatchEnumerator source = table;
                source = new Where(source, "ID", CompareOperator.Equals, value);
                source = new CountAggregator(source);

                source.Next(10240);
                int count = (int)source.ColumnGetter(0)().Array.GetValue(0);
                Console.WriteLine($"Done. {count:n0} found.");
            }
        }
    }
}

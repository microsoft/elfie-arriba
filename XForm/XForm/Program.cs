using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using System;
using System.IO;
using System.Linq;
using XForm.Aggregators;
using XForm.Data;
using XForm.Query;
using XForm.Readers;
using XForm.Transforms;

namespace XForm
{
    class Program
    {
        static void Main(string[] args)
        {
            //TimingComparisons();
            string query = File.ReadAllText(args[0]);
            int rowsWritten = 0;
            using (new TraceWatch(query))
            {
                using (IDataBatchSource source = PipelineFactory.BuildPipeline(query))
                {
                    while (true)
                    {
                        int batchCount = source.Next(10240);
                        if (batchCount == 0) break;
                        rowsWritten += batchCount;
                    }
                }
            }

            Console.WriteLine($"Done. {rowsWritten:n0} rows written.");
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

                IDataBatchSource source = table;
                source = new WhereFilter(source, "ID", CompareOperator.Equals, value);
                source = new CountAggregator(source);

                source.Next(10240);
                int count = (int)source.ColumnGetter(0)().Array.GetValue(0);
                Console.WriteLine($"Done. {count:n0} found.");
            }
        }
    }
}

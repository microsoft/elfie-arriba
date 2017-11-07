using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using System.Linq;
using XForm.Aggregators;
using XForm.Data;
using XForm.Readers;
using XForm.Transforms;
using XForm.Writers;

namespace XForm
{
    class Program
    {
        static void Main(string[] args)
        {
            int[] sample = new int[16 * 1024 * 1024];
            Random r = new Random(5);
            for(int i = 0; i < sample.Length; ++i)
            {
                sample[i] = r.Next(1000);
            }

            TimingComparisons(sample, 500);
            

            ArrayReader table = new ArrayReader();
            table.AddColumn(new ColumnDetails("ID", typeof(int), false), DataBatch.All(sample, sample.Length));

            String8Block block = new String8Block();

            IDataBatchSource source;
            source = table;
            //source = new TabularFileReader(TabularFactory.BuildReader(args[0]));
            //source = new WhereFilter(source, "ID", CompareOperator.Equals, 500);
            //source = new WhereFilter(source, "State", CompareOperator.NotEquals, block.GetCopy("Active"));
            //source = new WhereFilter(source, "Assigned To", CompareOperator.Equals, block.GetCopy("Barry Markey"));
            source = new WhereFilter(source, "ID", CompareOperator.Equals, 500);
            //source = new RowLimiter(source, 10000000);
            source = new CountAggregator(source);
            source = new TypeConverter(source, "Count", typeof(String8));

            using (new TraceWatch($"Copying from \"{args[0]}\" to \"{args[1]}\"..."))
            {
                using (TabularFileWriter writer = new TabularFileWriter(source, TabularFactory.BuildWriter(args[1])))
                {
                    writer.Copy(10240);
                    Console.WriteLine($"{writer.RowCountWritten:n0} rows, {writer.BytesWritten.SizeString()} writen.");
                }
            }
        }

        static void TimingComparisons(int[] array, int value)
        {
            using (new TraceWatch($"For Loop [==]"))
            {
                int count = 0;
                for (int i = 0; i < array.Length; ++i)
                {
                    if (array[i] == 500) count++;
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
        }
    }
}

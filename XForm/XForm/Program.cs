using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using XForm.Aggregators;
using XForm.Data;
using XForm.Readers;
using XForm.Sources;
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

            ArrayReader table = new ArrayReader();
            table.AddColumn(new ColumnDetails("ID", typeof(int), false), sample, sample.Length);

            String8Block block = new String8Block();

            IDataBatchSource source;
            source = table;
            //source = new TabularFileReader(TabularFactory.BuildReader(args[0]));
            //source = new WhereEqualsFilter<int>(source, "ID", 500);
            //source = new WhereEqualsFilter<String8>(source, "State", block.GetCopy("Active"));
            //source = new WhereEqualsFilter<String8>(source, "Assigned To", block.GetCopy("Barry Markey"));
            source = new WhereEqualsFilter<int>(source, "ID", 500);
            source = new CountAggregator(source);
            source = new TypeConverter(source, "Count", typeof(String8));

            using (new TraceWatch($"Copying from \"{args[0]}\" to \"{args[1]}\"..."))
            {
                using (TabularFileWriter writer = new TabularFileWriter(source, TabularFactory.BuildWriter(args[1])))
                {
                    writer.Copy();
                    Console.WriteLine($"{writer.RowCountWritten:n0} rows, {writer.BytesWritten.SizeString()} writen.");
                }
            }
        }
    }
}

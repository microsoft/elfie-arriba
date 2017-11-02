using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using XForm.Data;
using XForm.Sources;
using XForm.Transforms;
using XForm.Writers;

namespace XForm
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get this to work. What's up with Elfie BadImageFormatException?
            // Copy from TSV to CSV
            String8Block block = new String8Block();
            using (new TraceWatch($"Copying from \"{args[0]}\" to \"{args[1]}\"..."))
            {
                IDataBatchSource source = new TabularFileReader(TabularFactory.BuildReader(args[0]));
                source = new WhereEqualsFilter<String8>(source, "OU", block.GetCopy("UserAccounts"));
                //source = new WhereEqualsFilter<String8>(source, "Assigned To", block.GetCopy("Barry Markey"));
                using (TabularFileWriter writer = new TabularFileWriter(source, TabularFactory.BuildWriter(args[1])))
                {
                    writer.Copy();

                    Console.WriteLine($"{writer.RowCountWritten:n0} rows, {writer.BytesWritten.SizeString()} writen.");
                }
            }
        }
    }
}

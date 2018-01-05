using System;
using System.IO;

namespace XForm.Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            string xformProjectPath = Path.GetFullPath(Path.Combine(@"..\..\..\XForm"));

            string comparerPath = Path.Combine(xformProjectPath, @"Types\Comparers");
            Console.WriteLine($"Building Comparers to [{comparerPath}]...");
            ComparerGenerator.GenerateAll(comparerPath);

            string corePath = Path.Combine(xformProjectPath, @"Core");
            string dataBatchHasherGeneratorPath = Path.Combine(corePath, "DataBatchHasher.cs");
            Console.WriteLine($"Building {dataBatchHasherGeneratorPath}...");
            DataBatchHasherGenerator.Generate(dataBatchHasherGeneratorPath);

            Console.WriteLine("Done.");
        }
    }
}

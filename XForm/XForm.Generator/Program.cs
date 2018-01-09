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

            Console.WriteLine("Done.");
        }
    }
}

using Cinco.Extensions;
using System;
using System.IO;

namespace Cinco.ConsoleTest
{
    public static class Extensions
    {
        public static string ToSizeString(this long lengthBytes)
        {
            return $"{(lengthBytes / 1024.0 / 1024.0):n3} MB";
        }
    }

    /// <summary>
    ///  Cinco TODO:
    ///    - Unit Test project won't properly wait for build of Cinco.Native via Cinco reference.
    /// </summary>
    class Program
    {
        public enum Scenario
        {
            IndexOf,
            IndexOfOrdinal,
            IndexOfN
        }

        static int MatchCount(Scenario scenario, string text, string value, int fromIndex = 0)
        {
            int matchCount = 0;
            int index = -1;

            while(true)
            {
                switch (scenario)
                {
                    case Scenario.IndexOf:
                        index = text.IndexOf(value, fromIndex);
                        break;
                    case Scenario.IndexOfOrdinal:
                        index = text.IndexOf(value, fromIndex, StringComparison.Ordinal);
                        break;
                    case Scenario.IndexOfN:
                        index = text.IndexOfN(value, fromIndex);
                        break;
                }

                if (index == -1) break;

                fromIndex = index + 1;
                matchCount++;
            }

            return matchCount;
        }

        static void Main(string[] args)
        {
            string sampleFilePath = "Elfie.Arriba.All.cs";
            SampleBuilder.ConcatenateCode(@"C:\Code\XForm", sampleFilePath);

            string text = File.ReadAllText(sampleFilePath);
            long lengthBytes = (long)new FileInfo(sampleFilePath).Length;
            string value = "IndexOf";

            Console.WriteLine($"Searching for '{value}' in '{sampleFilePath}' ({lengthBytes.ToSizeString()})...");
            using (Benchmarker b = new Benchmarker("IndexOf", 2000))
            {
                b.Measure("String.IndexOf", (int)lengthBytes, () => MatchCount(Scenario.IndexOf, text, value));
                b.Measure("String.IndexOfOrdinal", (int)lengthBytes, () => MatchCount(Scenario.IndexOfOrdinal, text, value));
                b.Measure("String.IndexOfN", (int)lengthBytes, () => MatchCount(Scenario.IndexOfN, text, value));
            }
        }
    }
}
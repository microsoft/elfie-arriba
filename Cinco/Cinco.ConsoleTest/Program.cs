using Cinco.Extensions;
using System;
using System.Diagnostics;
using System.IO;

namespace Cinco.ConsoleTest
{
    public static class Extensions
    {
        public static string ToSizeString(this long lengthBytes)
        {
            return $"{(lengthBytes / 1024.0 / 1024.0):n3} MB";
        }

        public static string ToGigabytesPerSec(this double seconds, long lengthBytes, long iterations)
        {
            double bytesPerSecond = (lengthBytes * iterations) / seconds;
            double gbPerSecond = bytesPerSecond / (1024 * 1024 * 1024);
            return $"{gbPerSecond:n3} GB/s";
        }
    }

    /// <summary>
    ///  Cinco TODO:
    ///    - How do I get Cinco reference to copy in Cinco.Native?
    ///    - Unit Tests
    ///    - Easy performance comparison run
    ///    - Add IndexOfAll inside.
    /// </summary>
    class Program
    {
        static int IndexOfAll(string text, string value, int[] matches, int fromIndex = 0)
        {
            int matchCount = 0;

            while(matchCount < matches.Length)
            {
                int index = text.IndexOf(value, fromIndex);
                if (index == -1) break;

                matches[matchCount++] = index;
                fromIndex = index + 1;
            }

            return matchCount;
        }

        static void Main(string[] args)
        {
            string sampleFilePath = "Elfie.Arriba.All.cs";
            SampleBuilder.ConcatenateCode(@"C:\Code\XForm", sampleFilePath);

            string text = File.ReadAllText(sampleFilePath);
            long lengthBytes = new FileInfo(sampleFilePath).Length;
            string value = "IndexOf";
            int iterations = 100;

            Console.WriteLine($"Searching {iterations:n0} times for '{value}' in '{sampleFilePath}' ({lengthBytes.ToSizeString()})...");
            Stopwatch w = Stopwatch.StartNew();

            int[] matches = new int[100];
            int totalCount = 0;

            for (int i = 0; i < iterations; ++i)
            {
                int fromIndex = 0;

                while (true)
                {
                    int matchCount = IndexOfAll(text, value, matches, fromIndex);
                    totalCount += matchCount;

                    if (matchCount < matches.Length) break;
                    fromIndex = matches[matchCount - 1] + 1;
                }
            }

            w.Stop();
            Console.WriteLine($"Found {totalCount:n0} total matches in {w.ElapsedMilliseconds:n0} ms; {w.Elapsed.TotalSeconds.ToGigabytesPerSec(lengthBytes, iterations)}");
        }
    }
}
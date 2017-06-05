using System;
using System.Diagnostics;

namespace V5.ConsoleTest
{
    public static class Benchmark
    {
        public static void Compare(string description, int iterations, long bytes, string[] variantNames, params Action[] variants)
        {
            Console.WriteLine("{0}: {1:n0} iterations of {2:n0} bytes", description, iterations, bytes);

            double benchmarkMilliseconds = 0.0;

            for(int i = 0; i < variants.Length; ++i)
            {
                Action variant = variants[i];

                Stopwatch w = Stopwatch.StartNew();
                for (int iteration = 0; iteration < iterations; ++iteration)
                {
                    variant();
                }
                w.Stop();

                if (i == 0) benchmarkMilliseconds = w.ElapsedMilliseconds;

                double totalBytes = bytes * (long)iterations;
                double gigabytesPerSecond = ((double)totalBytes / (double)(1024 * 1024 * 1024)) / ((double)w.ElapsedMilliseconds / (double)1000);
                double relativePerformance = benchmarkMilliseconds / (double)w.ElapsedMilliseconds;
                Console.WriteLine(" - {0}: {1:n0}ms, {2:n2} GB/s [{3:n1}x]", variantNames[i], w.ElapsedMilliseconds, gigabytesPerSecond, relativePerformance);
            }

            Console.WriteLine();
        }
    }
}

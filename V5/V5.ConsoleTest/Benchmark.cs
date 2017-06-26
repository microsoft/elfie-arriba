using System;
using System.Diagnostics;

namespace V5.ConsoleTest
{
    public static class Benchmark
    {
        public static void Compare(string description, int iterations, long itemCount, string[] variantNames, params Func<object>[] variants)
        {
            Console.WriteLine("{0}: {1:n0} iterations of {2:n0} rows", description, iterations, itemCount);

            double benchmarkMilliseconds = 0.0;

            for(int i = 0; i < variants.Length; ++i)
            {
                Func<object> variant = variants[i];
                object result = variant() ?? "<null>";

                Stopwatch w = Stopwatch.StartNew();
                for (int iteration = 0; iteration < iterations; ++iteration)
                {
                    variant();
                }
                w.Stop();

                if (i == 0) benchmarkMilliseconds = w.ElapsedMilliseconds;

                double totalItems = itemCount * (long)iterations;
                double billionPerSecond = ((double)totalItems / (double)(1000 * 1000 * 1000)) / ((double)w.ElapsedMilliseconds / (double)1000);
                double relativePerformance = benchmarkMilliseconds / (double)w.ElapsedMilliseconds;
                Console.WriteLine(" - {0} -> {1}: {2:n0}ms, {3:n2} B/s [{4:n1}x]", variantNames[i], result, w.ElapsedMilliseconds, billionPerSecond, relativePerformance);
            }

            Console.WriteLine();
        }
    }
}

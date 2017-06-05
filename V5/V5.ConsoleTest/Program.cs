using System;

namespace V5.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int length = 8000000;
            byte[] test = new byte[length];

            Random r = new Random();
            r.NextBytes(test);

            byte rangeStart = 0;
            byte rangeEnd = 4;

            int rm = CountManaged(test, rangeStart, rangeEnd);
            int rn = String8Native.CountInRange(test, rangeStart, rangeEnd);
            Console.WriteLine($"Managed: {rm:n0}, Native: {rn:n0}");

            Benchmark.Compare("Count bytes in range", 100, length, new string[] { "Managed", "Native" },
                () => CountManaged(test, rangeStart, rangeEnd),
                () => String8Native.CountInRange(test, rangeStart, rangeEnd)
            );
        }

        private static int CountManaged(byte[] test, byte rangeStart, byte rangeEnd)
        {
            int count = 0;

            for (int i = 0; i < test.Length; ++i)
            {
                byte value = test[i];
                if (value >= rangeStart && value <= rangeEnd) count++;
            }

            return count;
        }
    }
}

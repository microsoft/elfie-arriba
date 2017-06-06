using System;

namespace V5.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int length = 0x1 << 23; // 8M
            byte[] test = new byte[length];

            Random r = new Random(5);
            r.NextBytes(test);

            //byte rangeStart = 0;
            //byte rangeEnd = 4;

            //int rm = CountManaged(test, rangeStart, rangeEnd);
            //int rn = String8Native.CountInRange(test, rangeStart, rangeEnd);
            //Console.WriteLine($"Managed: {rm:n0}, Native: {rn:n0}");

            //Benchmark.Compare("Count bytes in range", 100, length, new string[] { "Managed", "Native" },
            //    () => CountManaged(test, rangeStart, rangeEnd),
            //    () => String8Native.CountInRange(test, rangeStart, rangeEnd)
            //);

            uint[] managedVector = new uint[length >> 5];
            uint[] nativeVector = new uint[length >> 5];
            sbyte edge = 125;

            Array.Clear(managedVector, 0, managedVector.Length);
            WhereGreaterThan(test, edge, managedVector);
            int managed = Count(managedVector);

            Array.Clear(nativeVector, 0, nativeVector.Length);
            ArraySearch.WhereGreaterThan(test, edge, nativeVector);
            int native = Count(nativeVector);

            if(!AreEqual(managedVector, nativeVector))
            {
                Console.WriteLine("ERROR");
            }

            Console.WriteLine($"Managed: {managed}, Native: {native}");

            Benchmark.Compare("Find Items in Range", 100, length, new string[] { "Managed", "Native" },
                () => WhereGreaterThan(test, edge, managedVector),
                () => ArraySearch.WhereGreaterThan(test, edge, nativeVector)
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

        private static void WhereGreaterThan(byte[] test, sbyte value, uint[] resultVector)
        {
            for (int i = 0; i < test.Length; ++i)
            {
                if ((sbyte)test[i] > value)
                {
                    resultVector[i >> 5] |= (0x1U << (i & 31));
                }
            }
        }

        private static int Count(uint[] resultVector)
        {
            int count = 0;

            for(int i = 0; i < resultVector.Length; ++i)
            {
                uint segment = resultVector[i];

                for(int j = 0; j < 32; ++j)
                {
                    if ((segment & (0x1U << j)) != 0) count++;
                }
            }

            return count;
        }

        private static bool AreEqual(uint[] left, uint[] right)
        {
            if (left.Length != right.Length) return false;

            for(int i = 0; i < left.Length; ++i)
            {
                if (left[i] != right[i]) return false;
            }

            return true;
        }
    }
}

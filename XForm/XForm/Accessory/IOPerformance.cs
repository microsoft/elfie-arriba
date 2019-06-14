using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace XForm
{
    class IOPerformance
    {
        public static void Test()
        {
            string tablePath = @"C:\Download\XFormProduction\Table\HugeSample\Full\2018.06.28 00.00.00Z\";
            string fileSubpath = @"Segment\V.u16.bin";

            // 350MB/s, 4GB/s RAM cached
            //DirectRead(@"C:\Download\XFormProduction\Table\HugeSample\Full\2018.06.07 00.00.00Z\0\Segment\V.u16.bin", 20480 * 2);

            // ~380MB/s, 4GB/s RAM cached
            //ReadSet(@"C:\Download\XFormProduction\Table\HugeSample\Full\2018.06.07 00.00.00Z", @"Segment\V.u16.bin", 20480 * 2);

            // ~450MB/s, 4GB/s RAM cached
            //ReadSet(@"C:\Download\XFormProduction\Table\HugeSample\Full\2018.06.07 00.00.00Z", @"WasEncrypted\VR.u8.bin", 10 * 1024 * 1024);

            // CrystalDiskMark: 1.5GB/s sequential Q32T1

            ReadSetParallel(tablePath, fileSubpath, 4096 * 512);

            //Stopwatch w = Stopwatch.StartNew();
            //long bytesRead = DirectRead(Path.Combine(tablePath, "0", fileSubpath), 4096 * 4096);
            //double mbPerSecond = bytesRead / (1024 * 1024 * w.Elapsed.TotalSeconds);
            //Trace.WriteLine($"Done. Read {bytesRead.SizeString()}; {mbPerSecond:n2} MB/s.");
        }

        private static void ReadSetParallel(string tablePath, string filePathPerPartition, int bytesPerRead)
        {
            long bytesRead = 0;

            Stopwatch w = Stopwatch.StartNew();
            using (new TraceWatch($@"Reading {tablePath}\*\{filePathPerPartition}..."))
            {
                Parallel.ForEach(Directory.GetDirectories(tablePath), (partition) =>
                {
                    long setRead = DirectRead(Path.Combine(partition, filePathPerPartition), bytesPerRead);
                    Interlocked.Add(ref bytesRead, setRead);
                });
            }
            w.Stop();

            double mbPerSecond = bytesRead / (1024 * 1024 * w.Elapsed.TotalSeconds);
            Trace.WriteLine($"Done. Read {bytesRead.SizeString()}; {mbPerSecond:n2} MB/s.");
        }

        private static long DirectRead(string filePath, int bytesPerRead)
        {
            const ulong FILE_FLAG_NO_BUFFERING = 0x20000000;

            byte[] buffer = new byte[bytesPerRead];
            long totalRead = 0;

            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bytesPerRead / 4, (FileOptions)FILE_FLAG_NO_BUFFERING))
            {
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, bytesPerRead);
                    totalRead += bytesRead;
                    if (bytesRead < bytesPerRead) break;
                }
            }

            return totalRead;
        }
    }
}

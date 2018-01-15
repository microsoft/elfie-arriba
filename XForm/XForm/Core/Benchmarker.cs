// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace XForm
{
    /// <summary>
    ///  Benchmarker provides a simple interface for logging performance results quickly.
    ///  It automatically chooses an iteration count to get a reasonable measurement.
    ///  It logs via Trace.WriteLine and to a TSV file to keep historical data.
    ///  MeasureParallel can be used to test multi-threaded work for methods which take an index and length to share work in an array.
    ///
    ///  To validate the work, make the method return an object. ToString will be called on it.
    ///  Make sure that verification is done *in* ToString to avoid measuring the verification work.
    ///  
    ///  Usage:
    ///  using(Benchmarker b = new Benchmarker("Set Scenarios"))
    ///  {
    ///      b.Measure("Clear", () => set.Clear);
    ///      b.MeasureParallel("Count", (index, length) => set.Count(index, length));
    ///  }
    /// </summary>
    public class Benchmarker : IDisposable
    {
        public static string BenchmarkTsvName = "%ComputerName%.Benchmarks.tsv";
        private int _measureForMilliseconds;
        private StreamWriter _writer;
        private List<BenchmarkResult> _results;

        public Benchmarker(string groupName, int measureForMilliseconds = 500)
        {
            _measureForMilliseconds = measureForMilliseconds;
            _results = new List<BenchmarkResult>();

            // Get to the nearest folder which isn't a build output
            DirectoryInfo folder = new DirectoryInfo(Environment.CurrentDirectory);
            while (folder.Name.Equals("Release") || folder.Name.Equals("Debug") || folder.Name.Equals("bin") || folder.Name.Equals("x64"))
            {
                folder = folder.Parent;
            }

            _writer = File.AppendText(Path.Combine(folder.FullName, Environment.ExpandEnvironmentVariables(BenchmarkTsvName)));
            if (_writer.BaseStream.Length == 0) _writer.WriteLine("Name\tOutput\tX1\tX2\tX4");

            WriteHeading($"{groupName} @{DateTime.UtcNow:u}");
        }

        private void WriteHeading(string message)
        {
            _writer.WriteLine(message);
            Trace.WriteLine(message);
        }

        private static string Pad(string value, int length)
        {
            if (value == null) return new string(' ', length);
            if (value.Length >= length) return value;
            return value + new string(' ', length - value.Length);
        }

        private void WriteEntry(params BenchmarkResult[] results)
        {
            object output = results[0].Output ?? "<null>";
            string outputString = (output is int || output is long ? string.Format("{0:n0}", output) : output.ToString());

            _writer.Write($"{results[0].Name}\t{outputString}");
            Trace.Write($" - {Pad(results[0].Name, 40)} -> {Pad(outputString, 15)}");

            foreach (BenchmarkResult r in results)
            {
                _results.Add(r);
                _writer.Write($"\t{r.ToResultCount()}");
                Trace.Write($"\t{r.ToResultCount()}");

                if (r.Iterations == 1)
                {
                    _writer.Write($" ({r.Elapsed.TotalMilliseconds:n0}ms)");
                    Trace.Write($" ({r.Elapsed.TotalMilliseconds:n0}ms)");
                }
            }

            _writer.WriteLine();
            Trace.WriteLine("");
        }

        public void Measure(string name, int itemCount, Func<object> method)
        {
            WriteEntry(BenchmarkResult.Measure(name, itemCount, method, _measureForMilliseconds));
        }

        public void MeasureParallel(string name, int itemCount, Func<int, int, object> method)
        {
            WriteEntry(
                BenchmarkResult.Measure(name, itemCount, () => method(0, itemCount), _measureForMilliseconds),
                BenchmarkResult.MeasureParallel(name, itemCount, method, 2, _measureForMilliseconds),
                BenchmarkResult.MeasureParallel(name, itemCount, method, 4, _measureForMilliseconds)
            );
        }

        public void AssertResultsEqual()
        {
            object expected = _results[0].Output;
            for (int i = 1; i < _results.Count; ++i)
            {
                if (!expected.Equals(_results[i].Output))
                {
                    Trace.WriteLine($"{_results[i].Name} result {_results[i].Output} did not equal expected result {expected} from {_results[0].Name}.");
                }
            }
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.WriteLine();
                Trace.WriteLine("");

                _writer.Dispose();
                _writer = null;
            }
        }
    }

    internal class BenchmarkResult
    {
        public string Name { get; set; }
        public object Output { get; set; }
        public int ItemCount { get; set; }
        public int Iterations { get; set; }
        public TimeSpan Elapsed { get; set; }

        private BenchmarkResult()
        { }

        public string ToResultCount()
        {
            double itemsPerSecond = ((double)this.ItemCount * (double)this.Iterations) / this.Elapsed.TotalSeconds;

            if (itemsPerSecond > (1000 * 1000 * 1000))
            {
                return string.Format("{0:n1} B", itemsPerSecond / (double)(1000 * 1000 * 1000));
            }
            else if (itemsPerSecond > (1000 * 1000))
            {
                return string.Format("{0:n1} M", itemsPerSecond / (double)(1000 * 1000));
            }
            else if (itemsPerSecond > 1000)
            {
                return string.Format("{0:n1} K", itemsPerSecond / (double)(1000));
            }
            else
            {
                return string.Format("{0:n1} / s", itemsPerSecond);
            }
        }

        public static BenchmarkResult Measure(string name, int itemCount, Func<object> method, int forMilliseconds)
        {
            Stopwatch w = Stopwatch.StartNew();
            object output = method();
            w.Stop();

            if (w.Elapsed.TotalMilliseconds > forMilliseconds)
            {
                // If over 500ms, one iteration will do
                return new BenchmarkResult() { Name = name, Output = output, ItemCount = itemCount, Iterations = 1, Elapsed = w.Elapsed };
            }

            // Estimate iterations from one run
            int iterations = (w.Elapsed.TotalMilliseconds < 1 ? 2 * forMilliseconds : (int)(forMilliseconds / w.Elapsed.TotalMilliseconds));
            TimeSpan elapsed;

            w.Restart();

            for (int iteration = 0; iteration < iterations; ++iteration)
            {
                output = method();
            }

            w.Stop();
            elapsed = w.Elapsed;

            // If we used less than half the time, re-estimate and run again
            if (elapsed.TotalMilliseconds < forMilliseconds / 2)
            {
                int remainingMs = forMilliseconds - (int)elapsed.TotalMilliseconds;
                double iterationsPerMs = iterations / elapsed.TotalMilliseconds;
                int additionalIterations = (int)(remainingMs * iterationsPerMs);

                w.Restart();
                for (int iteration = 0; iteration < additionalIterations; ++iteration)
                {
                    output = method();
                }
                w.Stop();

                iterations += additionalIterations;
                elapsed += w.Elapsed;
            }

            return new BenchmarkResult() { Name = name, Output = output, ItemCount = itemCount, Iterations = iterations, Elapsed = w.Elapsed };
        }

        public static BenchmarkResult MeasureParallel(string name, int itemCount, Func<int, int, object> method, int parallelCount, int forMilliseconds)
        {
            return Measure(name, itemCount, () => RunParallel(itemCount, method, parallelCount), forMilliseconds);
        }

        private static object RunParallel(int itemCount, Func<int, int, object> method, int parallelCount)
        {
            object output = null;
            int segmentLength = ParallelLengthPart(itemCount, parallelCount);

            Parallel.For(0, parallelCount, (i) =>
            {
                int offset = i * segmentLength;
                int length = (i == parallelCount - 1 ? itemCount - offset : segmentLength);

                output = method(offset, length);
            });

            return output;
        }

        public static int ParallelLengthPart(int totalCount, int parallelCount)
        {
            int portionLength = totalCount / parallelCount;
            if ((portionLength & 63) != 0) portionLength = 64 + portionLength & ~63;
            return portionLength;
        }
    }

    internal class BenchmarkLogger
    {
        public const string BenchmarkTsvPath = @"..\..\..\XForm.Benchmarks.tsv";
        private StreamWriter _writer;

        public BenchmarkLogger()
        {
            _writer = File.AppendText(BenchmarkTsvPath);
            LogSessionStart();
        }

        private void WriteLine(string message = "")
        {
            _writer.WriteLine(message);
            Trace.WriteLine(" - " + message);
        }

        private void LogSessionStart()
        {
            // Write column headings if new file
            if (_writer.BaseStream.Length == 0)
            {
                _writer.WriteLine("Name\tOutput\tX1\tX2\tX4");
            }

            // Get the current git commit
            //ProcessStartInfo psi = new ProcessStartInfo();
            //psi.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
            //psi.Arguments = "/K git rev-parse --short HEAD";
            //psi.RedirectStandardOutput = true;
            //psi.UseShellExecute = false;
            //psi.WorkingDirectory = Environment.CurrentDirectory;

            //Process git = Process.Start(psi);
            //string nearestGitCommit = git.StandardOutput.ReadToEnd();
            //git.WaitForExit(1000);

            // Log the session
            WriteLine();
            WriteLine($"{DateTime.UtcNow:u}\t{Environment.MachineName}");//\t{nearestGitCommit}");
        }

        public void LogGroupStart(string description)
        {
            WriteLine();
            WriteLine(description);
        }

        internal void LogResult(BenchmarkResult result)
        {
            WriteLine($"{result.Name}\t{(result.Output ?? "<null>").ToString()}\t{result.ToResultCount()}\t{result.Elapsed.TotalMilliseconds:n0}ms\t{result.Iterations:n0}");
        }

        internal void LogResult(BenchmarkResult x1, BenchmarkResult x2, BenchmarkResult x4)
        {
            WriteLine($"{x1.Name}\t{(x1.Output ?? "<null>").ToString()}\t{x1.ToResultCount()}\t{x2.ToResultCount()}\t{x4.ToResultCount()}");
        }
    }
}

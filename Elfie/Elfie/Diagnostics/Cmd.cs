// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Microsoft.CodeAnalysis.Elfie.Diagnostics
{
    /// <summary>
    ///  Cmd is a small utility class which will run a requested command, wait for it to complete,
    ///  and capture output and the exit code from it. Cmd is used to incorporate batch script
    ///  segments easily into managed code (but with debuggability).
    /// </summary>
    public class Cmd
    {
        private object _locker;

        private Process Process { get; set; }
        private bool ShouldEcho { get; set; }
        private StreamWriter OutputWriter { get; set; }
        public string Command { get; private set; }
        public bool WasKilled { get; private set; }

        /// <summary>
        ///  Create a new command given an executable, arguments, and whether to echo output.
        /// </summary>
        /// <param name="executable">Full Path of executable to run</param>
        /// <param name="arguments">Arguments to pass to executable, or String.Empty</param>
        /// <param name="shouldEcho">True to write command output to Trace log, false not to</param>
        /// <param name="outputFilePath">File to redirect output to, or String.Empty</param>
        private Cmd(string executable, string arguments, bool shouldEcho, string outputFilePath)
        {
            _locker = new object();

            this.ShouldEcho = shouldEcho;
            this.Command = executable + " " + (arguments ?? String.Empty);

            this.Process = new Process();
            this.Process.StartInfo.FileName = executable;
            this.Process.StartInfo.Arguments = arguments;
            this.Process.StartInfo.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            this.Process.StartInfo.CreateNoWindow = true;
            this.Process.StartInfo.UseShellExecute = false;
            this.Process.StartInfo.RedirectStandardError = true;
            this.Process.StartInfo.RedirectStandardOutput = true;

            this.Process.OutputDataReceived += Process_OutputDataReceived;
            this.Process.ErrorDataReceived += Process_ErrorDataReceived;

            if (!String.IsNullOrEmpty(outputFilePath))
            {
                this.OutputWriter = new StreamWriter(outputFilePath, false);
            }
        }

        /// <summary>
        ///  Create a new command given a command line command and whether to echo output.
        /// </summary>
        /// <param name="command">A command line command to run (net use ...)</param>
        /// <param name="shouldEcho">True to write command output to Trace log, false not to</param>
        /// <param name="outputFilePath">File to redirect output to, or String.Empty</param>
        private Cmd(string command, bool shouldEcho, string outputFilePath)
            : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), "/C " + command, shouldEcho, outputFilePath)
        {
            this.Command = command;
        }

        /// <summary>
        ///  Run a given executable with the given arguments, allowing up to the timeout for it to exit.
        ///  Will wait until timeout for command to complete, but returns afterward either way. You need
        ///  to check Cmd.HasExited to verify the command completed.
        /// </summary>        
        /// <param name="executable">Full Path of executable to run</param>
        /// <param name="arguments">Arguments to pass to executable</param>
        /// <param name="timeout">Timeout to wait for command to complete</param>
        /// <param name="memoryLimitBytes">Limit of memory use to allow, in bytes, -1 for no limit</param>
        /// <param name="outputFilePath">File to redirect output to, or String.Empty</param>
        /// <returns>Cmd instance to check final state of launched executable.</returns>
        public static Cmd Echo(string executable, string arguments, TimeSpan timeout, long memoryLimitBytes = -1, string outputFilePath = null)
        {
            Cmd cmd = new Cmd(executable, arguments, true, outputFilePath);
            cmd.Wait(timeout, memoryLimitBytes);
            return cmd;
        }

        /// <summary>
        ///  Run a given executable with the given arguments, allowing up to the timeout for it to exit.
        ///  Will wait until timeout for command to complete, but returns afterward either way. You need
        ///  to check Cmd.HasExited to verify the command completed.
        /// </summary>        
        /// <param name="executable">Full Path of executable to run</param>
        /// <param name="arguments">Arguments to pass to executable</param>
        /// <param name="timeout">Timeout to wait for command to complete</param>
        /// <param name="memoryLimitBytes">Limit of memory use to allow, in bytes, -1 for no limit</param>
        /// <param name="outputFilePath">File to redirect output to, or String.Empty</param>
        /// <returns>Cmd instance to check final state of launched executable.</returns>
        public static Cmd Quiet(string executable, string arguments, TimeSpan timeout, long memoryLimitBytes = -1, string outputFilePath = null)
        {
            Cmd cmd = new Cmd(executable, arguments, false, outputFilePath);
            cmd.Wait(timeout, memoryLimitBytes);
            return cmd;
        }

        /// <summary>
        ///  Run a given command line, allowing up to the timeout for it to exit. Output is echoed to
        ///  the trace log.
        ///  
        ///  Will wait until timeout for command to complete, but returns afterward either way. You need
        ///  to check Cmd.HasExited to verify the command completed.
        /// </summary>
        /// <param name="command">Command line command to run</param>
        /// <param name="timeout">Timeout to wait for command to complete</param>
        /// <returns>Cmd instance to check final state of launched command</returns>
        public static Cmd Echo(string command, TimeSpan timeout, string outputFilePath = null)
        {
            Cmd cmd = new Cmd(command, true, outputFilePath);
            cmd.Wait(timeout);
            return cmd;
        }

        /// <summary>
        ///  Run a given command line, allowing up to the timeout for it to exit.
        ///  Will wait until timeout for command to complete, but returns afterward either way. You need
        ///  to check Cmd.HasExited to verify the command completed.
        /// </summary>        
        /// <param name="command">Command line command to run</param>
        /// <param name="timeout">Timeout to wait for command to complete</param>
        /// <returns>Cmd instance to check final state of launched command</returns>
        public static Cmd Quiet(string command, TimeSpan timeout, string outputFilePath = null)
        {
            Cmd cmd = new Cmd(command, false, outputFilePath);
            cmd.Wait(timeout);
            return cmd;
        }

        /// <summary>
        ///  Wait up to timeout for this command instance to exit. Check return value
        ///  to determine if command HasExited. If the memory use exceeds the limit,
        ///  the process will be killed.
        /// </summary>
        /// <param name="timeout">Timeout to wait for command to exit.</param>
        /// <param name="memoryLimitBytes">Memory use limit for process, -1 for no limit.</param>
        /// <returns>True if it exited, False otherwise</returns>
        public bool Wait(TimeSpan timeout, long memoryLimitBytes = -1)
        {
            if (this.ShouldEcho) Trace.WriteLine(this.Command);

            this.Process.Start();
            this.Process.BeginOutputReadLine();
            this.Process.BeginErrorReadLine();

            bool exited = false;

            long totalBytes = 0;
            Stopwatch runtime = Stopwatch.StartNew();

            // Wait for the process to exit or exceed timeout or memory limit
            while (runtime.Elapsed < timeout)
            {
                exited = this.Process.WaitForExit(1000);
                if (exited) break;

                if (memoryLimitBytes != -1)
                {
                    try
                    {
                        totalBytes = this.Process.WorkingSet64;
                        if (totalBytes > memoryLimitBytes) break;
                    }
                    catch (InvalidOperationException)
                    {
                        // [happens when the process exited between the WaitForExit and the WorkingSet64 get]
                    }
                }
            }

            // If it didn't exit, kill it
            if (!exited)
            {
                Trace.TraceError("Cmd: Killing command {0}, running {2:n0}s, using {3:n0} MB. (limit {4:n0}s, {5} MB)\r\n{1}", this.Process.Id, this.Command, runtime.Elapsed.TotalSeconds, totalBytes / 1024 / 1024, timeout.TotalSeconds, memoryLimitBytes / 1024 / 1024);
                this.WasKilled = true;
                this.Kill();
            }

            // Close output file, if there was one
            this.Dispose();

            return exited;
        }

        /// <summary>
        ///  Kill the command process.
        /// </summary>
        public void Kill()
        {
            this.Process.Kill();

            while (!this.Process.HasExited)
            {
                Thread.Sleep(5000);
            }

            this.Dispose();
        }

        /// <summary>
        ///  Returns whether the command exited.
        /// </summary>
        public bool HasExited
        {
            get { return this.WasKilled || this.Process.HasExited; }
        }

        /// <summary>
        ///  Returns the exit code from the command. You must check HasExited before attempting to read this.
        /// </summary>
        public int ExitCode
        {
            get { return this.Process.ExitCode; }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            if (this.ShouldEcho) Trace.WriteLine(e.Data);

            lock (_locker)
            {
                if (this.OutputWriter != null) this.OutputWriter.WriteLine(e.Data);
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            if (this.ShouldEcho) Trace.WriteLine(e.Data);

            lock (_locker)
            {
                if (this.OutputWriter != null) this.OutputWriter.WriteLine(e.Data);
            }
        }

        public void Dispose()
        {
            if (this.OutputWriter != null)
            {
                lock (_locker)
                {
                    if (this.OutputWriter != null)
                    {
                        this.OutputWriter.Dispose();
                        this.OutputWriter = null;
                    }
                }
            }
        }
    }
}

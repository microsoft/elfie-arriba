// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.CodeAnalysis.Elfie.Diagnostics
{
    /// <summary>
    ///  Logger writes the command lines run and unhandled errors to two log files in %LocalAppData%.
    /// </summary>
    public class Logger
    {
        public const string ErrorEntryFormat = @"
================================================================================
{0}
================================================================================
{1}

";

        private string LogPath { get; set; }
        private string ErrorLogPath { get; set; }
        private string UseLogPath { get; set; }

        public Logger(string logName = null)
        {
            if (String.IsNullOrEmpty(logName)) logName = "Elfie";
            this.LogPath = Path.Combine(Environment.ExpandEnvironmentVariables(@"%LocalAppData%"), logName);
            Directory.CreateDirectory(LogPath);

            this.ErrorLogPath = Path.Combine(this.LogPath, "Errors.log");
            this.UseLogPath = Path.Combine(this.LogPath, "Use.log");
        }

        public void LogUse()
        {
            File.AppendAllText(UseLogPath, String.Format("{0:MM/dd hh:mm:sst}: {1}\r\n", DateTime.Now, Environment.CommandLine));
        }

        public void LogException(Exception ex)
        {
            if (ex is AggregateException) ex = ((AggregateException)ex).InnerException;
            Console.WriteLine("ERROR: {0}: {1}\r\nDetail written to '{2}'.", ex.GetType().Name, ex.Message, ErrorLogPath);
            TryLog(ErrorLogPath, String.Format(ErrorEntryFormat, Environment.CommandLine, ex));
        }

        private void TryLog(string logPath, string message)
        {
            Exception lastException = null;

            for (int iteration = 0; iteration < 5; ++iteration)
            {
                // Try to append to the file. Track the last exception seen
                try
                {
                    File.AppendAllText(logPath, message);
                    return;
                }
                catch (IOException ex)
                {
                    lastException = ex;
                }

                // For failures, wait randomly up to 100ms to retry
                Random r = new Random();
                Thread.Sleep(r.Next(100));
            }

            Trace.WriteLine(String.Format("Logger Error: Unable to log to '{0}' after five tries.\r\nMessage: {1}\r\nLast Error: {2}", logPath, message, lastException.ToString()));
        }
    }
}

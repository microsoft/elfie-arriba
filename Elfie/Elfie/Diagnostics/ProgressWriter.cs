// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.CodeAnalysis.Elfie.Diagnostics
{
    /// <summary>
    ///  Small utility class to write progress of a slow operation to Trace output.
    ///  It will ensure the total work is exactly 48 dots and show a progress guide.
    ///  
    ///  Usage:
    ///  ProgressWriter p = new ProgressWriter(items.Count);
    ///  for( int i = 0; i &lt; items.Count; ++i )
    ///  {
    ///     // ... slow per item work
    ///     p.SetProgress(i + 1);
    ///  }
    /// </summary>
    public class ProgressWriter
    {
        /// <summary>
        ///  The number of dots which indicate completion (100% progress)
        /// </summary>
        public const int TotalDotsToWrite = 48;
        public const string Guide = "|          25          50          75          |";

        internal TextWriter Destination { get; set; }
        private int TotalItems { get; set; }
        private int LastCountDone { get; set; }

        /// <summary>
        ///  Create a new ProgressWriter to track progress for the given number of items,
        ///  outputting to the Tracing infrastructure.
        /// </summary>
        /// <param name="totalItems">Number of items to be processed</param>
        public ProgressWriter(int totalItems)
            : this(totalItems, null)
        { }

        /// <summary>
        ///  Create a new ProgressWriter to track progress for the given number of items.
        /// </summary>
        /// <param name="totalItems">Number of items to be processed</param>
        /// <param name="destination">TextWriter to which to write progress output</param>
        public ProgressWriter(int totalItems, TextWriter destination)
        {
            if (totalItems < 0) throw new ArgumentOutOfRangeException("totalItems");

            this.Destination = destination;
            this.TotalItems = Math.Max(1, totalItems);
            this.LastCountDone = 0;

            // Write rather than WriteLine to coexist nicely with ConsoleInterface.ConsoleProgressTraceListener
            Write(Guide + "\r\n");
        }

        /// <summary>
        ///  Set the current progress - output will be written to communicate the current
        ///  progress.
        /// </summary>
        /// <param name="countDone">Number of items processed so far</param>
        public void SetProgress(int countDone)
        {
            double lastPercentage = ((double)LastCountDone / (double)TotalItems);
            int lastDots = (int)(TotalDotsToWrite * lastPercentage);

            if (countDone > TotalItems) countDone = TotalItems;

            double percentage = (double)countDone / (double)TotalItems;
            int currentDots = (int)(TotalDotsToWrite * percentage);

            // Write additional progress dots if more should be shown
            if (currentDots > lastDots)
            {
                Write(new string('.', currentDots - lastDots));
            }

            // Write a newline if done
            if (countDone >= TotalItems)
            {
                Write("\r\n");
            }

            // Track highest previously seen count (don't track backward progress)
            if (countDone > LastCountDone) LastCountDone = countDone;
        }

        /// <summary>
        ///  Increment the current progress. Indicate one more item has been processed.
        /// </summary>
        public void IncrementProgress()
        {
            SetProgress(LastCountDone + 1);
        }

        private void Write(string value)
        {
            if (Destination == null)
            {
                Trace.Write(value);
            }
            else
            {
                Destination.Write(value);
            }
        }
    }
}

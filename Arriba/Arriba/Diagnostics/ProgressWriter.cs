// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Arriba.Diagnostics
{
    /// <summary>
    ///  Small utility class to write progress of a slow operation to the console.
    ///  It will ensure the total work is exactly one line of dots.
    ///  
    ///  Usage:
    ///  ProgressWriter p = new ProgressWriter(items.Count);
    ///  for( int i = 0; i &lt; items.Count; ++i )
    ///  {
    ///     // ... slow per item work
    ///     p.SetProgress(i + 1);
    ///  }
    ///  
    /// </summary>
    public class ProgressWriter
    {
        /// <summary>
        ///  The number of dots which indicate completion (100% progress)
        /// </summary>
        public static int TotalDotsToWrite
        {
            get { return 80; }
        }

        internal TextWriter Destination { get; set; }
        private int TotalItems { get; set; }
        private int LastCountDone { get; set; }

        /// <summary>
        ///  Create a new ProgressWriter to track progress for the given number of items,
        ///  outputting to the Trace infrastructure.
        /// </summary>
        /// <param name="totalItems">Number of items to be processed</param>
        public ProgressWriter(int totalItems)
            : this(totalItems, new TraceWriter())
        {
        }

        /// <summary>
        ///  Create a new ProgressWriter to track progress for the given number of items.
        /// </summary>
        /// <param name="totalItems">Number of items to be processed</param>
        /// <param name="destination">TextWriter to which to write progress output</param>
        public ProgressWriter(int totalItems, TextWriter destination)
        {
            if (totalItems < 0) throw new ArgumentOutOfRangeException("totalItems", "totalItems must not be negative.");

            this.Destination = destination;
            this.TotalItems = Math.Max(1, totalItems);
            this.LastCountDone = 0;
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

            // Cap progress at 100%
            if (countDone > TotalItems)
                countDone = TotalItems;

            double percentage = (double)countDone / (double)TotalItems;
            int currentDots = (int)(TotalDotsToWrite * percentage);

            // Write additional progress dots if more should be shown
            if (currentDots > lastDots)
            {
                //Destination.Write(new string('.', currentDots - lastDots));
                Destination.WriteLine("Percent Complete:" + percentage);
            }

            // Track highest previously seen count (don't track backward progress)
            if (countDone > LastCountDone)
                LastCountDone = countDone;
        }

        /// <summary>
        ///  Increment the current progress. Indicate one more item has been processed.
        /// </summary>
        public void IncrementProgress()
        {
            SetProgress(LastCountDone + 1);
        }
    }
}

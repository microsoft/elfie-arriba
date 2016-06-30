// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using Arriba.Extensions;

namespace Arriba.Diagnostics
{
    public class Memory
    {
        /// <summary>
        ///  Measure the managed memory use of a given object. Used to analyze
        ///  the memory use of different objects to tune use.
        /// </summary>
        /// <param name="creator">Function which creates object to measure</param>
        /// <returns>Size in bytes of memory used by new object(s).</returns>
        public static long MeasureObjectSize(Func<object> creator)
        {
            // Measure the memory used by the object creation, keeping it alive until the end measurement
            long beforeMemory = GC.GetTotalMemory(true);
            object o = creator();
            long afterMemory = GC.GetTotalMemory(true);
            GC.KeepAlive(o);

            // Return the new size used by the specific object (and any dependencies)
            long size = (afterMemory - beforeMemory);
            return size;
        }

        /// <summary>
        ///  Convert a desired number of gigabytes into a long for comparison to
        ///  file lengths and process allocation, for example.
        /// </summary>
        /// <param name="value">Number of Gigabytes to convert</param>
        /// <returns>Number of bytes corresponding to the argument in gigabytes</returns>
        public static long FromGigabytes(double value)
        {
            return (long)(value * 1024 * 1024 * 1024);
        }
    }
}

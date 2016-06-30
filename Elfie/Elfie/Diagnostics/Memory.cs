// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Elfie.Diagnostics
{
    public class MeasureDiagnostics
    {
        public TimeSpan LoadTime;
        public long MemoryUsedBytes;
    }

    /// <summary>
    ///  Memory is used to measure the total memory allocated by a method
    ///  which constructs an object. It's used to compare actual memory use
    ///  to estimates from IStatistics.
    /// </summary>
    public static class Memory
    {
        /// <summary>
        ///  Measure the managed memory use of a given object. Used to analyze
        ///  the memory use of different objects to tune use.
        /// </summary>
        /// <param name="creator">Function which creates object to measure</param>
        /// <returns>Size in bytes of memory used by new object(s).</returns>
        public static MeasureDiagnostics Measure(Func<object> creator)
        {
            Stopwatch w = Stopwatch.StartNew();

            // Measure the memory used by the object creation, keeping it alive until the end measurement
            long beforeMemory = GC.GetTotalMemory(true);
            object o = creator();
            long afterMemory = GC.GetTotalMemory(true);
            GC.KeepAlive(o);

            // Return the new size used by the specific object (and any dependencies)
            long size = (afterMemory - beforeMemory);
            return new MeasureDiagnostics() { MemoryUsedBytes = size, LoadTime = w.Elapsed };
        }
    }
}

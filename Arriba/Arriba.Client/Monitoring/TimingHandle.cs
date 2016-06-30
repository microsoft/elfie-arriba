// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Arriba.Monitoring
{
    /// <summary>
    /// Provides a mechanism to time a scope of execution in fractional milliseconds. 
    /// </summary>
    internal class TimingHandle : IDisposable
    {
        private static long s_qpcFrequency = 0;

        private class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

            [DllImport("kernel32.dll")]
            public static extern bool QueryPerformanceFrequency(out long lpFrequency);
        }

        private long _start = 0L;
        private long _stop = 0L;
        private double _accumulated = 0D;

        internal TimingHandle()
        {
            if (s_qpcFrequency == 0)
            {
                // NOTE: This is static for the lifetime of a machine 
                NativeMethods.QueryPerformanceFrequency(out s_qpcFrequency);
            }

            NativeMethods.QueryPerformanceCounter(out _start);
        }

        internal double ElapsedMiliseconds
        {
            get
            {
                double running = 0;

                if (_start > 0)
                {
                    long current;
                    NativeMethods.QueryPerformanceCounter(out current);
                    running = QpcRangeToMilliseconds(_start, current);
                }

                return running + _accumulated;
            }
        }

        private static double QpcRangeToMilliseconds(long start, long stop)
        {
            return (double)(stop - start) * 1000.0 / (double)s_qpcFrequency;
        }

        internal void Start()
        {
            if (_start == 0)
            {
                NativeMethods.QueryPerformanceCounter(out _start);
                _stop = 0;
            }
        }

        public void Dispose()
        {
            NativeMethods.QueryPerformanceCounter(out _stop);
            _accumulated = QpcRangeToMilliseconds(_start, _stop);
            _start = 0;
            _stop = 0;
        }
    }
}

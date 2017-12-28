// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using XForm.Data;
using XForm.Extensions;
using XForm.Test;

namespace XForm
{
    public class PerformanceComparisons
    {
        private int _rowCount;
        private ushort[] _values;
        private ushort[] _thresholds;
        private WorkflowContext _context;

        public PerformanceComparisons()
        {
            _rowCount = 10 * 1000 * 1000;
            _values = new ushort[_rowCount];
            _thresholds = new ushort[_rowCount];

            Random r = new Random();
            for (int i = 0; i < _values.Length; ++i)
            {
                _values[i] = (ushort)r.Next(1000);
                _thresholds[i] = 50;
            }

            _context = new WorkflowContext();
        }

        public void Run()
        {
            NativeAccelerator.Enable();
            DoubleWhere();
            //WhereIntUnderConstant();
            //WhereIntEqualsInt();
        }

        public void WhereIntUnderConstant()
        {
            using (Benchmarker b = new Benchmarker($"int[{_rowCount:n0}] | where [Value] < 50 | count", 3000))
            {
                b.Measure("For Count", _values.Length, () =>
                {
                    int count = 0;
                    for (int i = 0; i < _values.Length; ++i)
                    {
                        if (_values[i] < 50) count++;
                    }
                    return count;
                });

                b.Measure("Linq Count", _values.Length, () =>
                {
                    return _values.Where((i) => i < 50).Count();
                });

                b.Measure("XForm Count", _values.Length, () =>
                {
                    return (int)XFormTable.FromArrays(_values.Length)
                    .WithColumn("Value", _values)
                    .Query("where [Value] < 50", _context)
                    .Count();
                });

                b.AssertResultsEqual();
            }
        }

        public void WhereIntEqualsInt()
        {
            using (Benchmarker b = new Benchmarker($"int[{_rowCount:n0}] | where [Value] = [Threshold] | count"))
            {
                b.Measure("For Count", _values.Length, () =>
                {
                    int count = 0;
                    for (int i = 0; i < _values.Length; ++i)
                    {
                        if (_values[i] == _thresholds[i]) count++;
                    }
                    return count;
                });

                b.Measure("XForm Count", _values.Length, () =>
                {
                    return (int)XFormTable.FromArrays(_values.Length)
                    .WithColumn("Value", _values)
                    .WithColumn("Threshold", _thresholds)
                    .Query("where [Value] = [Threshold]", _context)
                    .Count();
                });

                b.AssertResultsEqual();
            }
        }

        public void DoubleWhere()
        {
            using (Benchmarker b = new Benchmarker($"int[{_rowCount:n0}] | where [Value] < 50 | where [Value] = 25 | count", 3000))
            {
                b.Measure("For Count", _values.Length, () =>
                {
                    int count = 0;
                    for (int i = 0; i < _values.Length; ++i)
                    {
                        if (_values[i] < 50)
                        {
                            if (_values[i] == 25) count++;
                        }
                    }
                    return count;
                });

                b.Measure("XForm Count", _values.Length, () =>
                {
                    return (int)XFormTable.FromArrays(_values.Length)
                    .WithColumn("Value", _values)
                    .Query("where [Value] < 50", _context)
                    .Query("where [Value] = 25", _context)
                    .Count();
                });

                b.AssertResultsEqual();
            }
        }
    }
}

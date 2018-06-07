// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Columns;
using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.IO.StreamProvider;

namespace XForm
{
    internal class HugeSampleGenerator
    {
        /// <summary>
        ///  Generate builds a sample table with a huge number of tiny rows for scale testing.
        /// </summary>
        /// <remarks>
        ///     [Segment]      - ushort looping from 0-65,535
        ///     [Status]       - one of five string values (1b Enum Column)
        ///     [WasEncrypted] - true 33%, false 67%
        ///     
        ///     Each row is 4b in binary format and ~20b as a CSV row.
        ///     5B rows is ~20GB in binary format and a ~100GB CSV.
        /// </remarks>
        /// <param name="rowCount">Number of rows to generate</param>
        /// <param name="databaseRoot">Database Root folder in which to generate</param>
        public static void Generate(long rowCount, XDatabaseContext context)
        {
            BinaryTableWriter.ColumnFileSizeLimit = 1 * 1024 * 1024 * 1024;

            String8Block block = new String8Block();
            String8[] statuses = new [] { "New", "Active", "Completed", "In Progress", "Blocked" }.Select((value) => block.GetCopy(value)).ToArray();

            string tablePath = context.StreamProvider.Path(LocationType.Table, "HugeSample", CrawlType.Full, DateTime.UtcNow.Date);

            using (new TraceWatch($"Generating HugeSample \r\n  with {rowCount:n0} rows\r\n  to {context.StreamProvider.Description}\\{tablePath}..."))
            {
                IXTable table = new RepeatingArrayTable(rowCount, ushort.MaxValue)
                    .WithColumn("Segment", Enumerable.Range(0, ushort.MaxValue).Select((i) => (ushort)i).ToArray())
                    .WithColumn("Status", Enumerable.Range(0, ushort.MaxValue).Select((i) => statuses[i % statuses.Length]).ToArray())
                    .WithColumn("WasEncrypted", Enumerable.Range(0, ushort.MaxValue).Select((i) => (i % 3) == 1).ToArray())
                    .Query($@"write ""{tablePath}""", context);

                table.RunAndDispose();
            }
        }
    }

    /// <summary>
    ///  Return the same array rows over and over for the desired count.
    /// </summary>
    internal class RepeatingArrayTable : IXTable
    {
        private List<ArrayColumn> _columns;
        private long _rowCount;
        private int _arrayRowCount;

        private long _countEnumerated;
        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public RepeatingArrayTable(long rowCount, int arrayRowCount)
        {
            _columns = new List<ArrayColumn>();
            _rowCount = rowCount;
            _arrayRowCount = arrayRowCount;
            Reset();
        }

        public RepeatingArrayTable WithColumn(ColumnDetails details, XArray fullColumn)
        {
            if (fullColumn.Count != _arrayRowCount) throw new ArgumentException($"All columns passed to ArrayReader must have the configured row count. The configured row count is {_rowCount:n0}; this column has {fullColumn.Count:n0} rows.");

            for (int i = 0; i < _columns.Count; ++i)
            {
                if (_columns[i].ColumnDetails.Name.Equals(details.Name, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"Can't add duplicate column. ArrayReader already has a column {details.Name}.");
            }

            _columns.Add(new ArrayColumn(fullColumn, details));
            return this;
        }

        public RepeatingArrayTable WithColumn(string columnName, Array array)
        {
            return WithColumn(new ColumnDetails(columnName, array.GetType().GetElementType()), XArray.All(array, _arrayRowCount));
        }

        public int CurrentRowCount { get; private set; }
        public IReadOnlyList<IXColumn> Columns => _columns;

        public void Reset()
        {
            _countEnumerated = 0;
            _currentEnumerateSelector = ArraySelector.All(_arrayRowCount).Slice(0, 0);

            for (int i = 0; i < _columns.Count; ++i)
            {
                _columns[i].SetSelector(_currentEnumerateSelector);
            }
        }

        public int Next(int desiredCount, CancellationToken cancellationToken)
        {
            long countToReturn = Math.Min(_rowCount - _countEnumerated, desiredCount);
            if (countToReturn == 0) return 0;

            _currentEnumerateSelector = _currentEnumerateSelector.NextPage(_arrayRowCount, (int)countToReturn);

            if (_currentEnumerateSelector.Count == 0)
            {
                _currentEnumerateSelector = ArraySelector.All(_arrayRowCount).Slice(0, 0);
                _currentEnumerateSelector = _currentEnumerateSelector.NextPage(_arrayRowCount, (int)countToReturn);
            }

            _currentSelector = _currentEnumerateSelector;

            for (int i = 0; i < _columns.Count; ++i)
            {
                _columns[i].SetSelector(_currentEnumerateSelector);
            }

            CurrentRowCount = _currentEnumerateSelector.Count;
            _countEnumerated += _currentEnumerateSelector.Count;
            return CurrentRowCount;
        }

        public void Dispose()
        { }
    }
}

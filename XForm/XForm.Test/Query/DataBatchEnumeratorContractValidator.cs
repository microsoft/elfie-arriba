// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;

namespace XForm.Test.Query
{
    public class DataBatchEnumeratorContractValidator : IDataBatchEnumerator
    {
        private IDataBatchEnumerator _inner;

        public bool ColumnSetRequested;
        public List<string> ColumnGettersRequested;
        public bool NextCalled;
        public bool DisposeCalled;
        public int CurrentBatchRowCount { get; private set; }

        public DataBatchEnumeratorContractValidator(IDataBatchEnumerator inner)
        {
            _inner = inner;
            this.ColumnGettersRequested = new List<string>();
        }

        public IReadOnlyList<ColumnDetails> Columns
        {
            get
            {
                ColumnSetRequested = true;
                return _inner.Columns;
            }
        }

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            if (!ColumnSetRequested) throw new AssertFailedException("Columns must be retrieved before asking for specific column getters.");
            if (NextCalled) throw new AssertFailedException("Column Getters must all be requested before the first Next() call (so callees know what to retrieve).");

            string columnName = _inner.Columns[columnIndex].Name;
            ColumnGettersRequested.Add(columnName);

            Func<DataBatch> innerGetter = _inner.ColumnGetter(columnIndex);
            return () =>
            {
                if (!NextCalled) throw new AssertFailedException($"ColumnGetter for {columnName} called before Next() was called.");

                DataBatch batch = innerGetter();
                if (batch.Count != CurrentBatchRowCount) throw new AssertFailedException($"Column {columnName} getter returned {batch.Count:n0} rows, but Next returned {CurrentBatchRowCount:n0} rows.");
                return batch;
            };
        }

        public void Dispose()
        {
            DisposeCalled = true;

            if (_inner != null)
            {
                _inner.Dispose();
                _inner = null;
            }
        }

        public int Next(int desiredCount)
        {
            NextCalled = true;

            CurrentBatchRowCount = _inner.Next(desiredCount);
            Assert.AreEqual(CurrentBatchRowCount, _inner.CurrentBatchRowCount, $"Enumerator must return the same row count from Next {CurrentBatchRowCount:n0} that it saves in CurrentRowBatchCount {_inner.CurrentBatchRowCount:n0}.");
            return CurrentBatchRowCount;
        }

        public void Reset()
        {
            NextCalled = false;

            _inner.Reset();
        }
    }
}

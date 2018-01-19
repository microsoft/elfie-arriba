// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;

namespace XForm.Test.Query
{
    public class XArrayEnumeratorContractValidator : IXTable
    {
        private IXTable _inner;

        public bool ColumnSetRequested;
        public List<string> ColumnGettersRequested;
        public bool NextCalled;
        public bool DisposeCalled;
        public int CurrentRowCount { get; private set; }

        public XArrayEnumeratorContractValidator(IXTable inner)
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

        public Func<XArray> ColumnGetter(int columnIndex)
        {
            if (!ColumnSetRequested) throw new AssertFailedException("Columns must be retrieved before asking for specific column getters.");
            if (NextCalled) throw new AssertFailedException("Column Getters must all be requested before the first Next() call (so callees know what to retrieve).");

            string columnName = _inner.Columns[columnIndex].Name;
            ColumnGettersRequested.Add(columnName);

            Func<XArray> innerGetter = _inner.ColumnGetter(columnIndex);
            return () =>
            {
                if (!NextCalled) throw new AssertFailedException($"ColumnGetter for {columnName} called before Next() was called.");

                XArray xarray = innerGetter();
                if (xarray.Count != CurrentRowCount) throw new AssertFailedException($"Column {columnName} getter returned {xarray.Count:n0} rows, but Next returned {CurrentRowCount:n0} rows.");
                return xarray;
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

            CurrentRowCount = _inner.Next(desiredCount);
            Assert.AreEqual(CurrentRowCount, _inner.CurrentRowCount, $"Enumerator must return the same row count from Next {CurrentRowCount:n0} that it saves in CurrentRowbatchCount {_inner.CurrentRowCount:n0}.");
            return CurrentRowCount;
        }

        public void Reset()
        {
            NextCalled = false;

            _inner.Reset();
        }
    }
}

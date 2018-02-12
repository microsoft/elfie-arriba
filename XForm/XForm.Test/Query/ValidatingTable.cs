// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;

namespace XForm.Test.Query
{
    internal class ValidatingColumn : IXColumn
    {
        private ValidatingTable _table;
        private IXColumn _column;

        public ValidatingColumn(ValidatingTable table, IXColumn column)
        {
            _table = table;
            _column = column;
        }

        public ColumnDetails ColumnDetails => _column.ColumnDetails;
        public Type IndicesType => _column.IndicesType;

        public Func<XArray> CurrentGetter()
        {
            if (_table.NextCalled) throw new AssertFailedException("Column Getters must all be requested before the first Next() call (so callees know what to retrieve).");
            Func<XArray> getter = _column.CurrentGetter();
            return () =>
            {
                XArray result = getter();
                Assert.AreEqual(_table.CurrentRowCount, result.Count, "Getter must return count matching Table.Next() and Table.CurrentRowCount.");
                return result;
            };
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            if (_table.NextCalled) throw new AssertFailedException("Column Getters must all be requested before the first Next() call (so callees know what to retrieve).");
            Func<ArraySelector, XArray> getter = _column.SeekGetter();
            return (selector) =>
            {
                XArray result = getter(selector);
                Assert.AreEqual(selector.Count, result.Count, "Seek getters must return count matching requested ArraySelector count.");
                return result;
            };
        }

        public Func<XArray> ValuesGetter()
        {
            return _column.ValuesGetter();
        }

        public Func<XArray> IndicesCurrentGetter()
        {
            if (_table.NextCalled) throw new AssertFailedException("Column Getters must all be requested before the first Next() call (so callees know what to retrieve).");
            Func<XArray> getter = _column.IndicesCurrentGetter();
            return () =>
            {
                XArray result = getter();
                Assert.AreEqual(_table.CurrentRowCount, result.Count, "Getter must return count matching Table.Next() and Table.CurrentRowCount.");
                return result;
            };
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            if (_table.NextCalled) throw new AssertFailedException("Column Getters must all be requested before the first Next() call (so callees know what to retrieve).");
            Func<ArraySelector, XArray> getter = _column.IndicesSeekGetter();
            return (selector) =>
            {
                XArray result = getter(selector);
                Assert.AreEqual(selector.Count, result.Count, "Seek getters must return count matching requested ArraySelector count.");
                return result;
            };
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }
    }

    public class ValidatingTable : IXTable
    {
        private IXTable _inner;
        private ValidatingColumn[] _columns;

        public bool NextCalled;
        public bool DisposeCalled;
        public int CurrentRowCount { get; private set; }

        public ValidatingTable(IXTable inner)
        {
            _inner = inner;
            _columns = inner.Columns.Select((col) => new ValidatingColumn(this, col)).ToArray();
        }

        public IReadOnlyList<IXColumn> Columns => _columns;

        public void Dispose()
        {
            DisposeCalled = true;

            if (_inner != null)
            {
                _inner.Dispose();
                _inner = null;
            }
        }

        public int Next(int desiredCount, CancellationToken cancellationToken)
        {
            if (cancellationToken == default(CancellationToken)) Assert.Fail("CancellationToken must be passed through the table pipeline.");

            NextCalled = true;

            CurrentRowCount = _inner.Next(desiredCount, cancellationToken);
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

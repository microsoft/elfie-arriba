// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace XForm.Data
{
    public class DataBatchEnumeratorWrapper : IDataBatchEnumerator
    {
        protected IDataBatchEnumerator _source;

        public DataBatchEnumeratorWrapper(IDataBatchEnumerator source)
        {
            _source = source;
        }

        public virtual IReadOnlyList<ColumnDetails> Columns => _source.Columns;

        public int CurrentBatchRowCount { get; private set; }

        public virtual Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return _source.ColumnGetter(columnIndex);
        }

        public virtual int Next(int desiredCount)
        {
            CurrentBatchRowCount = _source.Next(desiredCount);
            return CurrentBatchRowCount;
        }

        public virtual void Reset()
        {
            _source.Reset();
        }

        public virtual void Dispose()
        {
            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }
        }
    }
}

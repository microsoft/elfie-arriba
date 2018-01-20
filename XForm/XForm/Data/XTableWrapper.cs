// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace XForm.Data
{
    public class XTableWrapper : IXTable
    {
        protected IXTable _source;

        public XTableWrapper(IXTable source)
        {
            _source = source;
        }

        public int CurrentRowCount { get; private set; }
        public ArraySelector CurrentSelector => _source.CurrentSelector;
        public virtual IReadOnlyList<IXColumn> Columns => _source.Columns;

        public virtual int Next(int desiredCount)
        {
            CurrentRowCount = _source.Next(desiredCount);
            return CurrentRowCount;
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

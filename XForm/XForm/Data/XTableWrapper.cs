// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace XForm.Data
{
    public class XTableWrapper : IXTable
    {
        protected IXTable _source;

        public XTableWrapper(IXTable source)
        {
            if (source == null) throw new ArgumentNullException("source");
            _source = source;
        }

        public virtual int CurrentRowCount => _source.CurrentRowCount;
        public virtual IReadOnlyList<IXColumn> Columns => _source.Columns;

        public virtual int Next(int desiredCount, CancellationToken cancellationToken)
        {
            return _source.Next(desiredCount, cancellationToken);
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

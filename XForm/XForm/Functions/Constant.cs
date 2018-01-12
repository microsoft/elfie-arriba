// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Query;

namespace XForm.Functions
{
    public class Constant : IDataBatchColumn
    {
        public bool IsNull { get; private set; }
        public bool WasUnwrappedLiteral { get; private set; }
        public ColumnDetails ColumnDetails { get; private set; }

        private Array _valueArray;
        private IDataBatchEnumerator Source { get; set; }

        public Constant(IDataBatchEnumerator source, object value, Type type, bool wasUnwrappedLiteral = false)
        {
            Source = source;

            _valueArray = null;
            Allocator.AllocateToSize(ref _valueArray, 1, type);
            _valueArray.SetValue(value, 0);

            IsNull = (value == null || value.Equals("null"));
            WasUnwrappedLiteral = wasUnwrappedLiteral;
            ColumnDetails = new ColumnDetails(string.Empty, type, false);
        }

        public object Value => _valueArray.GetValue(0);

        public Func<DataBatch> Getter()
        {
            return () =>
            {
                if (IsNull) return DataBatch.Null(_valueArray, Source.CurrentBatchRowCount);
                return DataBatch.Single(_valueArray, Source.CurrentBatchRowCount);
            };
        }

        public override string ToString()
        {
            return XqlScanner.Escape(Value.ToString(), TokenType.Value, WasUnwrappedLiteral);
        }
    }
}

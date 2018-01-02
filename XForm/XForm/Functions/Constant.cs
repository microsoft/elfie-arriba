// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Query;

namespace XForm.Functions
{
    public class Constant : IDataBatchColumn
    {
        private IDataBatchEnumerator Source { get; set; }
        private Array ValueArray { get; set; }
        public bool IsNull { get; private set; }
        public ColumnDetails ColumnDetails { get; private set; }

        public Constant(IDataBatchEnumerator source, object value, Type type)
        {
            Source = source;
            ValueArray = Allocator.AllocateArray(type, 1);
            ValueArray.SetValue(value, 0);
            IsNull = (value == null);
            ColumnDetails = new ColumnDetails(string.Empty, type, false);
        }

        public object Value => ValueArray.GetValue(0);

        public Func<DataBatch> Getter()
        {
            return () =>
            {
                if (IsNull) return DataBatch.Null(ValueArray, Source.CurrentBatchRowCount);
                return DataBatch.Single(ValueArray, Source.CurrentBatchRowCount);
            };
        }

        public override string ToString()
        {
            return XqlScanner.Escape(Value.ToString(), TokenType.Value);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Transforms
{
    public interface IDataBatchComparer
    {
        Type Type { get; }

        void SetValue(object value);

        void WhereEquals(DataBatch source, RowRemapper result);
        void WhereNotEquals(DataBatch source, RowRemapper result);
        void WhereLessThan(DataBatch source, RowRemapper result);
        void WhereLessThanOrEquals(DataBatch source, RowRemapper result);
        void WhereGreaterThan(DataBatch source, RowRemapper result);
        void WhereGreaterThanOrEquals(DataBatch source, RowRemapper result);
    }
}

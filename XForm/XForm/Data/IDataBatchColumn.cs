// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace XForm.Data
{
    public interface IDataBatchColumn
    {
        ColumnDetails ColumnDetails { get; }
        Func<DataBatch> Getter();
    }

    public interface IDataBatchEnumColumn : IDataBatchColumn
    {
        DataBatch Values();
        Func<DataBatch> Indices();
    }
}

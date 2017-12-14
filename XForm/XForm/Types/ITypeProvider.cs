// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Query;
using XForm.Transforms;

namespace XForm.Types
{
    public interface IColumnReader : IDisposable
    {
        int Count { get; }
        DataBatch Read(ArraySelector selector);
    }

    public interface IColumnWriter : IDisposable
    {
        void Append(DataBatch batch);
    }

    public interface ITypeProvider
    {
        string Name { get; }
        Type Type { get; }

        IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath);
        IColumnWriter BinaryWriter(IStreamProvider streamProvider, string columnPath);

        Func<DataBatch, DataBatch> TryGetConverter(Type sourceType, Type targetType, object defaultValue, bool strict);

        Action<DataBatch, DataBatch, RowRemapper> TryGetComparer(CompareOperator op);
    }
}

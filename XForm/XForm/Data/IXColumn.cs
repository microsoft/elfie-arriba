// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace XForm.Data
{
    public interface IXColumn
    {
        ColumnDetails ColumnDetails { get; }
        Func<XArray> Getter();
    }

    public interface IXEnumColumn : IXColumn
    {
        Type IndicesType { get;  }
        XArray Values();
        Func<XArray> Indices();
    }
}

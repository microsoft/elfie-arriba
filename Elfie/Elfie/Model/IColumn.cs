// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    public interface IColumn : IBinarySerializable
    {
        void Clear();
        void Add();
        void ConvertToImmutable();
    }
}

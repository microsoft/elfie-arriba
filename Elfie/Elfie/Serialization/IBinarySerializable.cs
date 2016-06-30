// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    public interface IBinarySerializable
    {
        void WriteBinary(BinaryWriter w);
        void ReadBinary(BinaryReader r);
    }
}

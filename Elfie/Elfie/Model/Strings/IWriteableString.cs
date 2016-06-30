// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Model.Strings
{
    public interface IWriteableString
    {
        int Length { get; }
        int WriteTo(byte[] buffer, int index);
        int WriteTo(Stream stream);
        int WriteTo(TextWriter w);
    }
}

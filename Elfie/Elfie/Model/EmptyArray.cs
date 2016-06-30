// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    internal static class EmptyArray<T>
    {
        public static readonly T[] Instance = new T[0];
    }
}

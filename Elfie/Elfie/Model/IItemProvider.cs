// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    public interface IItemProvider<T>
    {
        T this[int index] { get; }
    }
}

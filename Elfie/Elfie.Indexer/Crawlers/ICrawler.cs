// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers
{
    public interface ICrawler
    {
        void Walk(string filePath, MutableSymbol parent);
    }
}

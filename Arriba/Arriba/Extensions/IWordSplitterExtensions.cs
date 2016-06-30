// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Indexing;
using Arriba.Structures;

namespace Arriba.Extensions
{
    public static class IWordSplitterExtensions
    {
        public static RangeSet Split(this IWordSplitter splitter, ByteBlock text)
        {
            RangeSet words = new RangeSet();
            splitter.Split(text.Array, new Range(text.Index, text.Length), words);
            return words;
        }
    }
}

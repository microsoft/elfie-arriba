// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Extensions;
using Arriba.Indexing;
using Arriba.Structures;

namespace Arriba.Test.Indexing
{
    public class WordSplitterTestBase
    {
        protected IWordSplitter splitter;

        public WordSplitterTestBase(IWordSplitter splitter)
        {
            this.splitter = splitter;
        }

        public string SplitAndJoin(ByteBlock text)
        {
            RangeSet words = this.splitter.Split(text);
            return words.ToString(text.Array);
        }
    }
}
